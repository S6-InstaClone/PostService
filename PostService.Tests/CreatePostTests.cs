using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostService.Dtos;
using PostService.Models;

namespace PostService.Tests;

/// <summary>
/// Tests for POST /api/posts (CreatePost)
/// </summary>
public class CreatePostTests : PostsControllerTestBase
{
    [Fact]
    public async Task CreatePost_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();
        var dto = new CreatePostDto { Caption = "Test caption" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePost_CreatesPost_WithValidData()
    {
        // Arrange
        var userId = "test-user-123";
        var username = "testuser";
        SetAuthHeaders(userId, username);

        var dto = new CreatePostDto { Caption = "My new post!" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var post = Assert.IsType<Post>(createdResult.Value);

        Assert.Equal("My new post!", post.Caption);
        Assert.Equal(userId, post.UserId);
        Assert.Equal(username, post.Username);
        Assert.Null(post.ImageUrl); // No file uploaded
    }

    [Fact]
    public async Task CreatePost_SetsCreatedAtToUtcNow()
    {
        // Arrange
        SetAuthHeaders("user-1", "user1");
        var dto = new CreatePostDto { Caption = "Test" };
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var afterCreate = DateTime.UtcNow;
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var post = Assert.IsType<Post>(createdResult.Value);

        Assert.InRange(post.CreatedAt, beforeCreate, afterCreate);
    }

    [Fact]
    public async Task CreatePost_SavesPostToDatabase()
    {
        // Arrange
        var userId = "persistent-user";
        SetAuthHeaders(userId, "persistuser");
        var dto = new CreatePostDto { Caption = "Should be saved" };

        // Act
        await Controller.CreatePost(dto);

        // Assert
        var savedPost = await DbContext.Posts.FirstOrDefaultAsync(p => p.UserId == userId);
        Assert.NotNull(savedPost);
        Assert.Equal("Should be saved", savedPost.Caption);
    }

    [Fact]
    public async Task CreatePost_ReturnsCreatedAtAction_WithCorrectRoute()
    {
        // Arrange
        SetAuthHeaders("user-1", "user1");
        var dto = new CreatePostDto { Caption = "Test" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetPost", createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.True(createdResult.RouteValues.ContainsKey("id"));
    }

    [Fact]
    public async Task CreatePost_StoresUsername_WhenProvided()
    {
        // Arrange
        SetAuthHeaders("user-123", "johndoe");
        var dto = new CreatePostDto { Caption = "Post with username" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var post = Assert.IsType<Post>(createdResult.Value);
        Assert.Equal("johndoe", post.Username);
    }

    [Fact]
    public async Task CreatePost_AllowsNullUsername()
    {
        // Arrange - only set user ID, not username
        Controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "user-no-name";
        var dto = new CreatePostDto { Caption = "Post without username" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var post = Assert.IsType<Post>(createdResult.Value);
        Assert.Null(post.Username);
    }

    [Fact]
    public async Task CreatePost_AssignsUniqueIds()
    {
        // Arrange
        SetAuthHeaders("user-1", "user1");

        // Act
        var result1 = await Controller.CreatePost(new CreatePostDto { Caption = "Post 1" });
        var result2 = await Controller.CreatePost(new CreatePostDto { Caption = "Post 2" });

        // Assert
        var post1 = Assert.IsType<Post>(((CreatedAtActionResult)result1.Result!).Value);
        var post2 = Assert.IsType<Post>(((CreatedAtActionResult)result2.Result!).Value);

        Assert.NotEqual(post1.Id, post2.Id);
    }
}