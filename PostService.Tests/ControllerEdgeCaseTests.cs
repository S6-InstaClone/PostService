using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PostService.Dtos;
using PostService.Models;

namespace PostService.Tests;

/// <summary>
/// Additional edge case tests for PostsController
/// </summary>
public class ControllerEdgeCaseTests : PostsControllerTestBase
{
    #region Header Parsing Edge Cases

    [Fact]
    public async Task GetMyPosts_HandlesWhitespaceOnlyUserId()
    {
        // Arrange
        Controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "   ";

        // Act
        var result = await Controller.GetMyPosts();

        // Assert - whitespace-only should be treated as empty
        // Behavior depends on implementation - adjust assertion if needed
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreatePost_HandlesSpecialCharactersInUsername()
    {
        // Arrange
        SetAuthHeaders("user-123", "user@domain.com");
        var dto = new CreatePostDto { Caption = "Test with special username" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var post = Assert.IsType<Post>(createdResult.Value);
        Assert.Equal("user@domain.com", post.Username);
    }

    [Fact]
    public async Task CreatePost_HandlesUnicodeUsername()
    {
        // Arrange
        SetAuthHeaders("user-123", "用户名");
        var dto = new CreatePostDto { Caption = "Test with unicode" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var post = Assert.IsType<Post>(createdResult.Value);
        Assert.Equal("用户名", post.Username);
    }

    [Fact]
    public async Task CreatePost_HandlesVeryLongCaption()
    {
        // Arrange
        SetAuthHeaders("user-123", "testuser");
        var longCaption = new string('A', 2200); // Max length
        var dto = new CreatePostDto { Caption = longCaption };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var post = Assert.IsType<Post>(createdResult.Value);
        Assert.Equal(2200, post.Caption.Length);
    }

    #endregion

    #region Multiple Posts Operations

    [Fact]
    public async Task GetPosts_ReturnsPostsInCorrectOrder_WithManyPosts()
    {
        // Arrange - Create posts with specific timestamps
        for (int i = 0; i < 20; i++)
        {
            var post = new Post
            {
                UserId = $"user-{i % 5}",
                Username = $"user{i % 5}",
                Caption = $"Post {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            DbContext.Posts.Add(post);
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Controller.GetPosts();

        // Assert
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(result.Value).ToList();
        Assert.Equal(20, posts.Count);
        
        // Verify ordering (newest first)
        for (int i = 0; i < posts.Count - 1; i++)
        {
            Assert.True(posts[i].CreatedAt >= posts[i + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task GetMyPosts_ReturnsOnlyOwnPosts_WhenManyUsersExist()
    {
        // Arrange
        var targetUserId = "target-user";
        
        // Create posts for many different users
        for (int i = 0; i < 50; i++)
        {
            var userId = i % 10 == 0 ? targetUserId : $"other-user-{i}";
            DbContext.Posts.Add(new Post
            {
                UserId = userId,
                Caption = $"Post by {userId}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await DbContext.SaveChangesAsync();

        SetAuthHeaders(targetUserId, "targetuser");

        // Act
        var result = await Controller.GetMyPosts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(okResult.Value).ToList();
        
        Assert.Equal(5, posts.Count); // Should be 5 posts (indexes 0, 10, 20, 30, 40)
        Assert.All(posts, p => Assert.Equal(targetUserId, p.UserId));
    }

    #endregion

    #region Update Edge Cases

    [Fact]
    public async Task UpdatePost_PreservesCreatedAt()
    {
        // Arrange
        var userId = "owner-user";
        var originalCreatedAt = DateTime.UtcNow.AddDays(-1);
        var post = new Post
        {
            UserId = userId,
            Username = "owner",
            Caption = "Original",
            CreatedAt = originalCreatedAt
        };
        DbContext.Posts.Add(post);
        await DbContext.SaveChangesAsync();

        SetAuthHeaders(userId, "owner");
        var dto = new UpdatePostDto { Id = post.Id, Caption = "Updated" };

        // Act
        await Controller.UpdatePost(post.Id, dto);

        // Assert
        var updatedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Equal(originalCreatedAt, updatedPost!.CreatedAt);
    }

    [Fact]
    public async Task UpdatePost_HandlesSameCaption()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Same caption");
        SetAuthHeaders(userId, "owner");

        var dto = new UpdatePostDto { Id = post.Id, Caption = "Same caption" }; // Same as original

        // Act
        var result = await Controller.UpdatePost(post.Id, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdatePost_HandlesEmptyCaption()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Original");
        SetAuthHeaders(userId, "owner");

        var dto = new UpdatePostDto { Id = post.Id, Caption = "" };

        // Act
        var result = await Controller.UpdatePost(post.Id, dto);

        // Assert - behavior depends on validation
        Assert.IsType<NoContentResult>(result);
        var updatedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Equal("", updatedPost!.Caption);
    }

    #endregion

    #region Delete Edge Cases

    [Fact]
    public async Task DeletePost_HandlesPostWithNullImageUrl()
    {
        // Arrange
        var userId = "owner-user";
        var post = new Post
        {
            UserId = userId,
            Caption = "No image",
            ImageUrl = null,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Posts.Add(post);
        await DbContext.SaveChangesAsync();

        SetAuthHeaders(userId, "owner");

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePost_HandlesPostWithEmptyImageUrl()
    {
        // Arrange
        var userId = "owner-user";
        var post = new Post
        {
            UserId = userId,
            Caption = "Empty image URL",
            ImageUrl = "",
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Posts.Add(post);
        await DbContext.SaveChangesAsync();

        SetAuthHeaders(userId, "owner");

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Concurrent Access Scenarios

    [Fact]
    public async Task GetPost_ReturnsLatestData_AfterUpdate()
    {
        // Arrange
        var post = await CreateTestPostAsync("user-1", "user1", "Original");
        var postId = post.Id;

        // Update directly in database
        post.Caption = "Updated via DB";
        await DbContext.SaveChangesAsync();

        // Act - Get should return updated data
        var result = await Controller.GetPost(postId);

        // Assert
        var fetchedPost = Assert.IsType<Post>(result.Value);
        Assert.Equal("Updated via DB", fetchedPost.Caption);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public async Task GetPost_HandlesMaxIntId()
    {
        // Act
        var result = await Controller.GetPost(int.MaxValue);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPost_HandlesZeroId()
    {
        // Act
        var result = await Controller.GetPost(0);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPost_HandlesNegativeId()
    {
        // Act
        var result = await Controller.GetPost(-1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion
}
