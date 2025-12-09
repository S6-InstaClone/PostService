using Microsoft.AspNetCore.Mvc;
using PostService.Dtos;

namespace PostService.Tests;

/// <summary>
/// Tests for PUT /api/posts/{id} (UpdatePost)
/// </summary>
public class UpdatePostTests : PostsControllerTestBase
{
    [Fact]
    public async Task UpdatePost_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();
        var post = await CreateTestPostAsync("owner-user", "owner", "Original caption");
        var dto = new UpdatePostDto { Id = post.Id, Caption = "Updated" };

        // Act
        var result = await Controller.UpdatePost(post.Id, dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePost_ReturnsNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        SetAuthHeaders("some-user", "someuser");
        var dto = new UpdatePostDto { Id = 999, Caption = "Updated" };

        // Act
        var result = await Controller.UpdatePost(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePost_ReturnsForbidden_WhenUserDoesNotOwnPost()
    {
        // Arrange
        var post = await CreateTestPostAsync("owner-user", "owner", "Original caption");
        SetAuthHeaders("different-user", "differentuser"); // Not the owner

        var dto = new UpdatePostDto { Id = post.Id, Caption = "Trying to update" };

        // Act
        var result = await Controller.UpdatePost(post.Id, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdatePost_UpdatesCaption_WhenUserOwnsPost()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Original caption");
        SetAuthHeaders(userId, "owner");

        var dto = new UpdatePostDto { Id = post.Id, Caption = "Updated caption" };

        // Act
        var result = await Controller.UpdatePost(post.Id, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify database was updated
        var updatedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Equal("Updated caption", updatedPost!.Caption);
    }

    [Fact]
    public async Task UpdatePost_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Original");
        SetAuthHeaders(userId, "owner");

        var dto = new UpdatePostDto { Id = post.Id, Caption = "Updated" };

        // Act
        var result = await Controller.UpdatePost(post.Id, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdatePost_PreservesOtherFields_WhenOnlyCaptionChanged()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Original");
        post.ImageUrl = "http://example.com/image.jpg";
        await DbContext.SaveChangesAsync();

        SetAuthHeaders(userId, "owner");
        var dto = new UpdatePostDto { Id = post.Id, Caption = "New caption" };

        // Act
        await Controller.UpdatePost(post.Id, dto);

        // Assert
        var updatedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Equal("New caption", updatedPost!.Caption);
        Assert.Equal("http://example.com/image.jpg", updatedPost.ImageUrl);
        Assert.Equal(userId, updatedPost.UserId);
        Assert.Equal("owner", updatedPost.Username);
    }

    [Fact]
    public async Task UpdatePost_DoesNotAllowChangingUserId()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Original");
        SetAuthHeaders(userId, "owner");

        var dto = new UpdatePostDto { Id = post.Id, Caption = "Updated" };

        // Act
        await Controller.UpdatePost(post.Id, dto);

        // Assert - UserId should remain unchanged
        var updatedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Equal(userId, updatedPost!.UserId);
    }

    [Fact]
    public async Task UpdatePost_WithMismatchedIds_UsesRouteId()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Original");
        SetAuthHeaders(userId, "owner");

        // DTO has different ID than route
        var dto = new UpdatePostDto { Id = 999, Caption = "Updated" };

        // Act
        var result = await Controller.UpdatePost(post.Id, dto); // Using route ID

        // Assert - should update the post from route, not DTO
        Assert.IsType<NoContentResult>(result);
        var updatedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Equal("Updated", updatedPost!.Caption);
    }
}
