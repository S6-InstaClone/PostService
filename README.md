# PostService

A microservice for managing posts in the InstaClone application. Handles CRUD operations for user posts, image storage, and GDPR-compliant data deletion.

## Overview

PostService is part of the InstaClone microservices architecture, responsible for:
- Creating, reading, updating, and deleting posts
- Managing post images via Azure Blob Storage
- Processing account deletion events for GDPR compliance
- Exposing metrics for monitoring via Prometheus

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 8.0 |
| Database | PostgreSQL 9.x |
| ORM | Entity Framework Core 9.x |
| Message Broker | RabbitMQ (via MassTransit) |
| Blob Storage | Azure Blob Storage |
| Monitoring | OpenTelemetry + Prometheus |
| Containerization | Docker |

## API Endpoints

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| GET | `/api/posts` | No | Get all posts |
| GET | `/api/posts/{id}` | No | Get a specific post |
| GET | `/api/posts/my-posts` | Yes | Get current user's posts |
| POST | `/api/posts` | Yes | Create a new post |
| PUT | `/api/posts/{id}` | Yes | Update a post (owner only) |
| DELETE | `/api/posts/{id}` | Yes | Delete a post (owner only) |

## Project Structure

```
PostService/
├── Business/
│   └── PostsService.cs           # Business logic layer
├── Consumers/
│   └── AccountDeletedConsumer.cs # RabbitMQ event handler
├── Controllers/
│   └── PostController.cs         # REST API endpoints
├── Dtos/
│   ├── CreatePostDto.cs
│   └── UpdatePostDto.cs
├── Messages/
│   └── AccountDeletedEvent.cs    # Event contract
├── Models/
│   ├── Post.cs                   # Post entity
│   └── Profile.cs
├── Persistence/
│   ├── PostRepository.cs         # DbContext
│   └── BlobService.cs
├── Migrations/
├── Program.cs                    # Application entry point
└── appsettings.json
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DB_HOST` | PostgreSQL host | `post-db` |
| `DB_PORT` | PostgreSQL port | `5432` |
| `DB_NAME` | Database name | `posts` |
| `DB_USER` | Database username | Required |
| `DB_PASSWORD` | Database password | Required |
| `RABBITMQ_HOST` | RabbitMQ host | `rabbitmq` |
| `RABBITMQ_USER` | RabbitMQ username | Required |
| `RABBITMQ_PASSWORD` | RabbitMQ password | Required |
| `BLOB_CONNECTION_STRING` | Azure Blob Storage connection | Required |
| `BLOB_CONTAINER_NAME` | Blob container name | `postimages` |
| `CORS_ORIGINS` | Allowed CORS origins | `http://localhost:55757,http://localhost:3000` |

### Example `.env` File

```bash
DB_HOST=post-db
DB_PORT=5432
DB_NAME=posts
DB_USER=admin
DB_PASSWORD=your_secure_password

RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=your_secure_password

BLOB_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;...
BLOB_CONTAINER_NAME=postimages

ASPNETCORE_ENVIRONMENT=Docker
```

## Running Locally

### Prerequisites
- .NET 8.0 SDK
- Docker (for PostgreSQL, RabbitMQ, Azurite)

### Development Setup

```bash
# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update

# Run the service
dotnet run
```

### Docker

```bash
# Build image
docker build -t postservice .

# Run container
docker run -p 5006:5006 --env-file .env postservice
```

## Testing

```bash
# Run all tests
cd PostService.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~GetPostsTests"
```

### Test Coverage

The test suite covers:
- Controller endpoints (GET, POST, PUT, DELETE)
- Authorization and ownership checks
- GDPR account deletion consumer
- Model validation
- Repository operations

## GDPR Compliance

PostService listens for `AccountDeletedEvent` messages on RabbitMQ. When received:
1. All posts by the deleted user are removed from the database
2. Associated images are deleted from blob storage
3. Operations are logged for audit purposes

## Monitoring

Prometheus metrics are exposed at `/metrics`:
- ASP.NET Core request metrics
- HTTP client metrics
- .NET runtime metrics (GC, memory, thread pool)

## CI/CD

GitHub Actions workflows:
- **ci-security.yml**: Build, test, and security scanning
- **deploy.yml**: Build and push Docker image to GHCR
- **deploy-security.yml**: Security-focused deployment with Trivy scanning

## Security Features

- Gitleaks for secret detection
- SonarCloud for SAST analysis
- Trivy for container vulnerability scanning
- OWASP ZAP for DAST scanning
- Dependency vulnerability checking

## License

Proprietary - Fontys ICT Advanced Software Project
