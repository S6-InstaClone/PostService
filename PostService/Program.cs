using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using PostService.Business;
using PostService.Consumers;
using PostService.Data;
using PostService.Persistence;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
namespace PostService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp",
                    builder => builder
                        .WithOrigins(
                            Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',')
                            ?? new[] { "http://localhost:55757", "http://localhost:3000" }
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            // Build connection string from environment variables
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "post-db";
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "posts";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER")
                ?? throw new InvalidOperationException("DB_USER environment variable is required");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
                ?? throw new InvalidOperationException("DB_PASSWORD environment variable is required");

            var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

            builder.Services.AddDbContext<PostRepository>(options =>
                options.UseNpgsql(connectionString));

            var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                ?? "localhost:6379,abortConnect=false";

            // 1. Add Redis as distributed cache (L2)
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "InstaClone:PostService:";
            });

            // 2. Add HybridCache (L1 in-memory + L2 Redis)
            builder.Services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    LocalCacheExpiration = TimeSpan.FromMinutes(2),
                    Expiration = TimeSpan.FromMinutes(15)
                };
                options.MaximumPayloadBytes = 5 * 1024 * 1024; // 5MB max
                options.MaximumKeyCount = 10000;
            });

            // 3. Register Redis connection for health checks (optional)
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse(redisConnectionString);
                config.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(config);
            });

            // Azure Blob Storage from environment variable
            var blobConnectionString = Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING")
                ?? throw new InvalidOperationException("BLOB_CONNECTION_STRING environment variable is required");

            builder.Services.AddSingleton(x => new BlobServiceClient(blobConnectionString));

            // Store container name in configuration for access in controllers
            var containerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME") ?? "postimages";
            builder.Configuration["BlobStorage:ContainerName"] = containerName;

            builder.Services.AddEndpointsApiExplorer();

            // Only enable Swagger in Development
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddSwaggerGen();
            }

            builder.Services.AddScoped<BlobService>();
            builder.Services.AddScoped<PostsService>();

            // OpenTelemetry with comprehensive metrics
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(
                        serviceName: "PostService",
                        serviceVersion: "1.0.0",
                        serviceInstanceId: Environment.MachineName))
                .WithMetrics(metrics =>
                {
                    metrics
                        // ASP.NET Core metrics (requests, connections)
                        .AddAspNetCoreInstrumentation()
                        // HttpClient metrics
                        .AddHttpClientInstrumentation()
                        // .NET Runtime metrics (GC, Memory, ThreadPool)
                        .AddRuntimeInstrumentation()
                        // Prometheus exporter
                        .AddPrometheusExporter();
                });

            // RabbitMQ configuration from environment variables
            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
            var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER")
                ?? throw new InvalidOperationException("RABBITMQ_USER environment variable is required");
            var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                ?? throw new InvalidOperationException("RABBITMQ_PASSWORD environment variable is required");

            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<AccountDeletedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitHost, "/", h =>
                    {
                        h.Username(rabbitUser);
                        h.Password(rabbitPassword);
                    });

                    cfg.ReceiveEndpoint("account-deleted-queue", e =>
                    {
                        e.ConfigureConsumer<AccountDeletedConsumer>(context);
                    });
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PostRepository>();

                try
                {
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    // Log and rethrow so Kubernetes/Docker can restart the pod
                    Console.Error.WriteLine($"Database migration failed: {ex}");
                    throw;
                }
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Prometheus metrics endpoint at /metrics
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            // Only use HTTPS redirection in production with proper certificates
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowReactApp");
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
