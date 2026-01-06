using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Models;

namespace PostService.Tests;

/// <summary>
/// Tests for PostRepository (DbContext)
/// </summary>
public class PostRepositoryTests : IDisposable
{
    private readonly PostRepository _dbContext;

    public PostRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PostRepository>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PostRepository(options);
    }

    #region DbContext Tests

    [Fact]
    public void PostRepository_CanBeInstantiated()
    {
        // Assert
        Assert.NotNull(_dbContext);
    }

    [Fact]
    public void PostRepository_HasPostsDbSet()
    {
        // Assert
        Assert.NotNull(_dbContext.Posts);
    }

    [Fact]
    public async Task PostRepository_CanAddPost()
    {
        // Arrange
        var post = new Post
        {
            UserId = "user-123",
            Caption = "Test post",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.True(post.Id > 0);
    }

    [Fact]
    public async Task PostRepository_CanQueryPosts()
    {
        // Arrange
        _dbContext.Posts.AddRange(
            new Post { UserId = "user-1", Caption = "Post 1", CreatedAt = DateTime.UtcNow },
            new Post { UserId = "user-2", Caption = "Post 2", CreatedAt = DateTime.UtcNow },
            new Post { UserId = "user-1", Caption = "Post 3", CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var user1Posts = await _dbContext.Posts
            .Where(p => p.UserId == "user-1")
            .ToListAsync();

        // Assert
        Assert.Equal(2, user1Posts.Count);
    }

    [Fact]
    public async Task PostRepository_CanUpdatePost()
    {
        // Arrange
        var post = new Post
        {
            UserId = "user-123",
            Caption = "Original",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        // Act
        post.Caption = "Updated";
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedPost = await _dbContext.Posts.FindAsync(post.Id);
        Assert.Equal("Updated", updatedPost!.Caption);
    }

    [Fact]
    public async Task PostRepository_CanDeletePost()
    {
        // Arrange
        var post = new Post
        {
            UserId = "user-123",
            Caption = "To delete",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();
        var postId = post.Id;

        // Act
        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync();

        // Assert
        var deletedPost = await _dbContext.Posts.FindAsync(postId);
        Assert.Null(deletedPost);
    }

    [Fact]
    public async Task PostRepository_CanFindPostById()
    {
        // Arrange
        var post = new Post
        {
            UserId = "user-123",
            Caption = "Find me",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        // Act
        var foundPost = await _dbContext.Posts.FindAsync(post.Id);

        // Assert
        Assert.NotNull(foundPost);
        Assert.Equal("Find me", foundPost.Caption);
    }

    [Fact]
    public async Task PostRepository_FindReturnsNull_WhenPostDoesNotExist()
    {
        // Act
        var post = await _dbContext.Posts.FindAsync(99999);

        // Assert
        Assert.Null(post);
    }

    [Fact]
    public async Task PostRepository_CanOrderByCreatedAt()
    {
        // Arrange
        _dbContext.Posts.AddRange(
            new Post { UserId = "user-1", Caption = "Oldest", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Post { UserId = "user-1", Caption = "Newest", CreatedAt = DateTime.UtcNow },
            new Post { UserId = "user-1", Caption = "Middle", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var posts = await _dbContext.Posts
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Assert
        Assert.Equal("Newest", posts[0].Caption);
        Assert.Equal("Middle", posts[1].Caption);
        Assert.Equal("Oldest", posts[2].Caption);
    }

    [Fact]
    public async Task PostRepository_CanCountPosts()
    {
        // Arrange
        _dbContext.Posts.AddRange(
            new Post { UserId = "user-1", Caption = "Post 1", CreatedAt = DateTime.UtcNow },
            new Post { UserId = "user-2", Caption = "Post 2", CreatedAt = DateTime.UtcNow },
            new Post { UserId = "user-3", Caption = "Post 3", CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var count = await _dbContext.Posts.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task PostRepository_CanDeleteRange()
    {
        // Arrange
        var posts = new List<Post>
        {
            new Post { UserId = "user-1", Caption = "Post 1", CreatedAt = DateTime.UtcNow },
            new Post { UserId = "user-1", Caption = "Post 2", CreatedAt = DateTime.UtcNow },
            new Post { UserId = "user-2", Caption = "Post 3", CreatedAt = DateTime.UtcNow }
        };
        _dbContext.Posts.AddRange(posts);
        await _dbContext.SaveChangesAsync();

        // Act - Delete user-1's posts
        var user1Posts = await _dbContext.Posts.Where(p => p.UserId == "user-1").ToListAsync();
        _dbContext.Posts.RemoveRange(user1Posts);
        await _dbContext.SaveChangesAsync();

        // Assert
        var remainingPosts = await _dbContext.Posts.ToListAsync();
        Assert.Single(remainingPosts);
        Assert.Equal("user-2", remainingPosts[0].UserId);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
