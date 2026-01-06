using Microsoft.EntityFrameworkCore;
using PostService.Business;
using PostService.Data;

namespace PostService.Tests;

/// <summary>
/// Tests for PostsService business logic
/// </summary>
public class PostsServiceTests : IDisposable
{
    private readonly PostRepository _dbContext;
    private readonly PostsService _postsService;

    public PostsServiceTests()
    {
        var options = new DbContextOptionsBuilder<PostRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PostRepository(options);
        _postsService = new PostsService(_dbContext);
    }

    [Fact]
    public void PostsService_CanBeInstantiated()
    {
        // Assert
        Assert.NotNull(_postsService);
    }

    [Fact]
    public void PostsService_HasDbContext()
    {
        // The service should be properly initialized with a DbContext
        // This tests the constructor
        var options = new DbContextOptionsBuilder<PostRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new PostRepository(options);
        var service = new PostsService(context);
        
        Assert.NotNull(service);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
