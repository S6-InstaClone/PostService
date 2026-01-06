using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PostService.Consumers;
using PostService.Data;
using PostService.Models;
using AccountService.Messages;

namespace PostService.Tests;

/// <summary>
/// Additional edge case tests for AccountDeletedConsumer
/// </summary>
public class AccountDeletedConsumerEdgeCaseTests : IDisposable
{
    private readonly PostRepository _dbContext;
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<AccountDeletedConsumer>> _mockLogger;
    private readonly Mock<Azure.Storage.Blobs.BlobContainerClient> _mockContainerClient;
    private readonly Mock<Azure.Storage.Blobs.BlobClient> _mockBlobClient;
    private readonly AccountDeletedConsumer _consumer;

    public AccountDeletedConsumerEdgeCaseTests()
    {
        var options = new DbContextOptionsBuilder<PostRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PostRepository(options);
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AccountDeletedConsumer>>();

        _mockConfig.Setup(c => c["BlobStorage:ContainerName"]).Returns("testcontainer");

        // Setup blob mocks
        _mockContainerClient = new Mock<Azure.Storage.Blobs.BlobContainerClient>();
        _mockBlobClient = new Mock<Azure.Storage.Blobs.BlobClient>();

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_mockBlobClient.Object);

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_mockContainerClient.Object);

        _consumer = new AccountDeletedConsumer(
            _dbContext,
            _mockBlobServiceClient.Object,
            _mockConfig.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Consume_HandlesEmptyImageUrl()
    {
        // Arrange
        var userId = "user-empty-image";
        var post = new Post
        {
            UserId = userId,
            Caption = "Post with empty image URL",
            ImageUrl = "",  // Empty string
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent
        {
            UserId = userId,
            Reason = "GDPR_USER_REQUEST"
        };

        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        var remainingPosts = await _dbContext.Posts.Where(p => p.UserId == userId).ToListAsync();
        Assert.Empty(remainingPosts);
    }

    [Fact]
    public async Task Consume_HandlesMultiplePostsWithMixedImageUrls()
    {
        // Arrange
        var userId = "user-mixed-images";
        _dbContext.Posts.AddRange(
            new Post { UserId = userId, Caption = "With image", ImageUrl = "http://blob.storage/img1.jpg", CreatedAt = DateTime.UtcNow },
            new Post { UserId = userId, Caption = "Without image", ImageUrl = null, CreatedAt = DateTime.UtcNow },
            new Post { UserId = userId, Caption = "Empty URL", ImageUrl = "", CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent { UserId = userId, Reason = "GDPR_USER_REQUEST" };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        var remainingPosts = await _dbContext.Posts.Where(p => p.UserId == userId).ToListAsync();
        Assert.Empty(remainingPosts);
    }

    [Fact]
    public async Task Consume_HandlesDifferentReasons()
    {
        // Arrange
        var userId = "admin-deleted-user";
        _dbContext.Posts.Add(new Post { UserId = userId, Caption = "Test", CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent
        {
            UserId = userId,
            Reason = "ADMIN_ACTION"  // Different reason than GDPR
        };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert - Should still delete
        var remainingPosts = await _dbContext.Posts.Where(p => p.UserId == userId).ToListAsync();
        Assert.Empty(remainingPosts);
    }

    [Fact]
    public async Task Consume_HandlesUserWithManyPosts()
    {
        // Arrange
        var userId = "prolific-user";
        for (int i = 0; i < 100; i++)
        {
            _dbContext.Posts.Add(new Post
            {
                UserId = userId,
                Caption = $"Post {i}",
                ImageUrl = $"http://blob.storage/{userId}/image{i}.jpg",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent { UserId = userId, Reason = "GDPR_USER_REQUEST" };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        var remainingPosts = await _dbContext.Posts.Where(p => p.UserId == userId).ToListAsync();
        Assert.Empty(remainingPosts);
    }

    [Fact]
    public async Task Consume_HandlesInvalidImageUrl()
    {
        // Arrange
        var userId = "user-invalid-url";
        _dbContext.Posts.Add(new Post
        {
            UserId = userId,
            Caption = "Invalid URL",
            ImageUrl = "not-a-valid-url",  // Invalid URL format
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent { UserId = userId, Reason = "GDPR_USER_REQUEST" };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act - Should not throw even with invalid URL
        await _consumer.Consume(mockContext.Object);

        // Assert - Post should still be deleted from database
        var remainingPosts = await _dbContext.Posts.Where(p => p.UserId == userId).ToListAsync();
        Assert.Empty(remainingPosts);
    }

    [Fact]
    public async Task Consume_HandlesMessageWithAllProperties()
    {
        // Arrange
        var userId = "full-user";
        _dbContext.Posts.Add(new Post { UserId = userId, Caption = "Test", CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent
        {
            UserId = userId,
            Username = "fulluser",
            Email = "full@example.com",
            DeletedAt = DateTime.UtcNow,
            Reason = "GDPR_USER_REQUEST"
        };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        var remainingPosts = await _dbContext.Posts.Where(p => p.UserId == userId).ToListAsync();
        Assert.Empty(remainingPosts);
    }

    [Fact]
    public async Task Consume_HandlesUUIDFormatUserId()
    {
        // Arrange
        var userId = "550e8400-e29b-41d4-a716-446655440000"; // Standard UUID format
        _dbContext.Posts.Add(new Post { UserId = userId, Caption = "UUID user post", CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent { UserId = userId, Reason = "GDPR_USER_REQUEST" };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        var remainingPosts = await _dbContext.Posts.Where(p => p.UserId == userId).ToListAsync();
        Assert.Empty(remainingPosts);
    }

    [Fact]
    public async Task Consume_LeavesOtherUsersPostsIntact()
    {
        // Arrange
        var userToDelete = "delete-me";
        var userToKeep = "keep-me";

        _dbContext.Posts.AddRange(
            new Post { UserId = userToDelete, Caption = "Delete 1", CreatedAt = DateTime.UtcNow },
            new Post { UserId = userToDelete, Caption = "Delete 2", CreatedAt = DateTime.UtcNow },
            new Post { UserId = userToKeep, Caption = "Keep 1", CreatedAt = DateTime.UtcNow },
            new Post { UserId = userToKeep, Caption = "Keep 2", CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent { UserId = userToDelete, Reason = "GDPR_USER_REQUEST" };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        var allPosts = await _dbContext.Posts.ToListAsync();
        Assert.Equal(2, allPosts.Count);
        Assert.All(allPosts, p => Assert.Equal(userToKeep, p.UserId));
    }

    [Fact]
    public async Task Consume_ConfigUsesDefaultContainerName_WhenNotConfigured()
    {
        // Arrange - create consumer with null container name config
        var mockConfigNull = new Mock<IConfiguration>();
        mockConfigNull.Setup(c => c["BlobStorage:ContainerName"]).Returns((string?)null);

        var consumer = new AccountDeletedConsumer(
            _dbContext,
            _mockBlobServiceClient.Object,
            mockConfigNull.Object,
            _mockLogger.Object);

        var userId = "user-default-container";
        _dbContext.Posts.Add(new Post
        {
            UserId = userId,
            Caption = "Test",
            ImageUrl = "http://blob.storage/img.jpg",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var message = new AccountDeletedEvent { UserId = userId, Reason = "GDPR_USER_REQUEST" };
        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await consumer.Consume(mockContext.Object);

        // Assert - should use default "postimages" container
        _mockBlobServiceClient.Verify(
            x => x.GetBlobContainerClient("postimages"),
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
