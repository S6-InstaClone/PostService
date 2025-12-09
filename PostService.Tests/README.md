# PostService Unit Tests

## Project Structure

```
PostService.Tests/
├── PostService.Tests.csproj       # Test project file
├── PostsControllerTestBase.cs     # Base class with shared setup
├── GetPostsTests.cs               # Tests for GET endpoints
├── CreatePostTests.cs             # Tests for POST endpoint
├── UpdatePostTests.cs             # Tests for PUT endpoint
├── DeletePostTests.cs             # Tests for DELETE endpoint
├── AuthorizationTests.cs          # Authorization-specific tests
└── PostModelTests.cs              # Model validation tests
```

## Running Tests

```bash
# Run all tests
cd PostService.Tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~GetPostsTests"

# Run specific test
dotnet test --filter "GetPosts_ReturnsAllPosts_OrderedByCreatedAtDescending"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Categories

### GET Endpoints (`GetPostsTests.cs`)
- `GET /api/posts` - List all posts (public)
- `GET /api/posts/{id}` - Get single post (public)
- `GET /api/posts/my-posts` - Get current user's posts (auth required)

### POST Endpoint (`CreatePostTests.cs`)
- `POST /api/posts` - Create new post (auth required)
- Tests caption, username storage, timestamps

### PUT Endpoint (`UpdatePostTests.cs`)
- `PUT /api/posts/{id}` - Update post (auth + ownership required)
- Tests authorization and field preservation

### DELETE Endpoint (`DeletePostTests.cs`)
- `DELETE /api/posts/{id}` - Delete post (auth + ownership required)
- Tests authorization and database removal

### Authorization (`AuthorizationTests.cs`)
- Public vs protected endpoints
- Ownership verification
- Edge cases (UUID formats, case sensitivity)

### Model (`PostModelTests.cs`)
- Data annotations validation
- Property constraints
- Required vs optional fields

## Test Approach

- **In-Memory Database**: Uses EF Core InMemory provider for fast, isolated tests
- **Mocked Dependencies**: BlobServiceClient, IConfiguration, ILogger are mocked
- **No External Dependencies**: Tests run without Docker, databases, or blob storage

## Adding New Tests

1. Inherit from `PostsControllerTestBase` for controller tests
2. Use `SetAuthHeaders()` to simulate authenticated requests
3. Use `SeedPostsAsync()` or `CreateTestPostAsync()` to set up test data
4. Each test class gets a fresh in-memory database

## CI/CD Integration

Add to your GitHub Actions workflow:

```yaml
- name: Run Tests
  run: dotnet test PostService.Tests --configuration Release --verbosity normal
```
