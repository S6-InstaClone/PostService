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
/// Tests for AccountDeletedConsumer (GDPR compliance)
/// </summary>
public class AccountDeletedConsumerTests : IDisposable
{
    private readonly PostRepository _dbContext;
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<AccountDeletedConsumer>> _mockLogger;
    private readonly AccountDeletedConsumer _consumer;

    public AccountDeletedConsumerTests()
    {
        var options = new DbContextOptionsBuilder<PostRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PostRepository(options);
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AccountDeletedConsumer>>();

        _mockConfig.Setup(c => c["BlobStorage:ContainerName"]).Returns("testcontainer");

        // Setup blob container mock
        var mockContainerClient = new Mock<Azure.Storage.Blobs.BlobContainerClient>();
        var mockBlobClient = new Mock<Azure.Storage.Blobs.BlobClient>();
        
        mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);
        
        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mockContainerClient.Object);

        _consumer = new AccountDeletedConsumer(
            _dbContext,
            _mockBlobServiceClient.Object,
            _mockConfig.Object,
            _mockLogger.Object);
    }

    private async Task<List<Post>> CreateTestPostsForUser(string userId, int count)
    {
        var posts = new List<Post>();
        for (int i = 0; i < count; i++)
        {
            var post = new Post
            {
                UserId = userId,
                Username = $"user_{userId}",
                Caption = $"Post {i} by user {userId}",
                ImageUrl = $"http://blob.storage/{userId}/image{i}.jpg",
                CreatedAt = DateTime.UtcNow
            };
            posts.Add(post);
        }

        _dbContext.Posts.AddRange(posts);
        await _dbContext.SaveChangesAsync();
        return posts;
    }

    [Fact]
    public async Task Consume_DeletesAllPostsForUser()
    {
        // Arrange
        var userId = "user-to-delete";
        await CreateTestPostsForUser(userId, 3);
        await CreateTestPostsForUser("other-user", 2); // Should not be deleted

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
        var remainingPosts = await _dbContext.Posts.ToListAsync();
        Assert.Equal(2, remainingPosts.Count);
        Assert.All(remainingPosts, p => Assert.Equal("other-user", p.UserId));
    }

    [Fact]
    public async Task Consume_DoesNothingWhenNoPostsExist()
    {
        // Arrange
        var userId = "user-with-no-posts";
        await CreateTestPostsForUser("other-user", 2);

        var message = new AccountDeletedEvent
        {
            UserId = userId,
            Reason = "GDPR_USER_REQUEST"
        };

        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert - other user's posts should still exist
        var remainingPosts = await _dbContext.Posts.ToListAsync();
        Assert.Equal(2, remainingPosts.Count);
    }

    [Fact]
    public async Task Consume_DeletesPostsWithImages()
    {
        // Arrange
        var userId = "user-with-images";
        var posts = await CreateTestPostsForUser(userId, 2);

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
    public async Task Consume_HandlesPostsWithoutImages()
    {
        // Arrange
        var userId = "user-no-images";
        var post = new Post
        {
            UserId = userId,
            Caption = "Post without image",
            ImageUrl = null,
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
    public async Task Consume_LogsInformationAboutDeletion()
    {
        // Arrange
        var userId = "logged-user";
        await CreateTestPostsForUser(userId, 2);

        var message = new AccountDeletedEvent
        {
            UserId = userId,
            Reason = "GDPR_USER_REQUEST"
        };

        var mockContext = new Mock<ConsumeContext<AccountDeletedEvent>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert - verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AccountDeletedEvent")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
