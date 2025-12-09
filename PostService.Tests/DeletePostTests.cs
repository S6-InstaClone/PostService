using Microsoft.AspNetCore.Mvc;

namespace PostService.Tests;

/// <summary>
/// Tests for DELETE /api/posts/{id} (DeletePost)
/// </summary>
public class DeletePostTests : PostsControllerTestBase
{
    [Fact]
    public async Task DeletePost_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();
        var post = await CreateTestPostAsync("owner-user", "owner", "To be deleted");

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeletePost_ReturnsNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        SetAuthHeaders("some-user", "someuser");

        // Act
        var result = await Controller.DeletePost(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePost_ReturnsForbidden_WhenUserDoesNotOwnPost()
    {
        // Arrange
        var post = await CreateTestPostAsync("owner-user", "owner", "Protected post");
        SetAuthHeaders("attacker-user", "attacker"); // Not the owner

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeletePost_RemovesPost_WhenUserOwnsPost()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "To delete");
        SetAuthHeaders(userId, "owner");

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify post was removed from database
        var deletedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Null(deletedPost);
    }

    [Fact]
    public async Task DeletePost_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "To delete");
        SetAuthHeaders(userId, "owner");

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePost_DoesNotAffectOtherPosts()
    {
        // Arrange
        var userId = "owner-user";
        var postToDelete = await CreateTestPostAsync(userId, "owner", "Delete me");
        var postToKeep = await CreateTestPostAsync(userId, "owner", "Keep me");
        SetAuthHeaders(userId, "owner");

        // Act
        await Controller.DeletePost(postToDelete.Id);

        // Assert
        var remainingPost = await DbContext.Posts.FindAsync(postToKeep.Id);
        Assert.NotNull(remainingPost);
        Assert.Equal("Keep me", remainingPost.Caption);
    }

    [Fact]
    public async Task DeletePost_CannotDeleteSamePostTwice()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "To delete");
        SetAuthHeaders(userId, "owner");

        // Act
        await Controller.DeletePost(post.Id); // First delete
        var result = await Controller.DeletePost(post.Id); // Second delete

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePost_PostWithImage_StillDeletesFromDatabase()
    {
        // Arrange
        var userId = "owner-user";
        var post = await CreateTestPostAsync(userId, "owner", "Post with image");
        post.ImageUrl = "http://blob.storage/image.jpg";
        await DbContext.SaveChangesAsync();

        SetAuthHeaders(userId, "owner");

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert - post should be deleted even if blob deletion is mocked
        Assert.IsType<NoContentResult>(result);
        var deletedPost = await DbContext.Posts.FindAsync(post.Id);
        Assert.Null(deletedPost);
    }
}
