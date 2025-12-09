using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PostService.Controllers;
using PostService.Data;
using PostService.Models;

namespace PostService.Tests;

/// <summary>
/// Base class for controller tests with common setup
/// </summary>
public abstract class PostsControllerTestBase : IDisposable
{
    protected readonly PostRepository DbContext;
    protected readonly Mock<BlobServiceClient> MockBlobServiceClient;
    protected readonly Mock<IConfiguration> MockConfig;
    protected readonly Mock<ILogger<PostsController>> MockLogger;
    protected readonly PostsController Controller;

    protected PostsControllerTestBase()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PostRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new PostRepository(options);

        // Setup mocks
        MockBlobServiceClient = new Mock<BlobServiceClient>();
        MockConfig = new Mock<IConfiguration>();
        MockLogger = new Mock<ILogger<PostsController>>();

        // Setup configuration
        MockConfig.Setup(c => c["BlobStorage:ContainerName"]).Returns("testcontainer");

        // Setup blob container mock
        var mockContainerClient = new Mock<Azure.Storage.Blobs.BlobContainerClient>();
        MockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mockContainerClient.Object);

        // Create controller
        Controller = new PostsController(
            DbContext,
            MockBlobServiceClient.Object,
            MockConfig.Object,
            MockLogger.Object);

        // Setup default HTTP context
        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    /// <summary>
    /// Sets authentication headers on the controller's request
    /// </summary>
    protected void SetAuthHeaders(string userId, string? username = null)
    {
        Controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        if (username != null)
        {
            Controller.ControllerContext.HttpContext.Request.Headers["X-User-Name"] = username;
        }
    }

    /// <summary>
    /// Clears authentication headers
    /// </summary>
    protected void ClearAuthHeaders()
    {
        Controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Id");
        Controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Name");
    }

    /// <summary>
    /// Seeds the database with test posts
    /// </summary>
    protected async Task<List<Post>> SeedPostsAsync(int count = 3, string? userId = null)
    {
        var posts = new List<Post>();
        for (int i = 1; i <= count; i++)
        {
            var post = new Post
            {
                UserId = userId ?? $"user-{i}",
                Username = $"testuser{i}",
                Caption = $"Test caption {i}",
                ImageUrl = $"http://blob.storage/image{i}.jpg",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            posts.Add(post);
        }

        DbContext.Posts.AddRange(posts);
        await DbContext.SaveChangesAsync();
        return posts;
    }

    /// <summary>
    /// Creates a single test post
    /// </summary>
    protected async Task<Post> CreateTestPostAsync(string userId, string username, string caption)
    {
        var post = new Post
        {
            UserId = userId,
            Username = username,
            Caption = caption,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Posts.Add(post);
        await DbContext.SaveChangesAsync();
        return post;
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }
}
