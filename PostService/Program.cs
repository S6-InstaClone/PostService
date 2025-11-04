using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using PostService.Business;
using PostService.Data;
using PostService.Persistence;

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
                        .WithOrigins("http://localhost:55757")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()); // if sending cookies or auth headers
            });
            builder.Services.AddDbContext<PostRepository>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddSingleton(x =>
            {
                var config = builder.Configuration.GetSection("BlobStorage");
                return new BlobServiceClient(config["ConnectionString"]);
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<BlobService>();
            builder.Services.AddScoped<PostsService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowReactApp");
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
