using Microsoft.AspNetCore.Mvc;
using PostService.Dtos;

namespace PostService.Tests;

/// <summary>
/// Tests specifically for authorization scenarios across all endpoints
/// </summary>
public class AuthorizationTests : PostsControllerTestBase
{
    #region Public Endpoints (No Auth Required)

    [Fact]
    public async Task PublicEndpoints_GetPosts_SucceedsWithoutAuth()
    {
        // Arrange
        ClearAuthHeaders();
        await SeedPostsAsync(2);

        // Act
        var result = await Controller.GetPosts();

        // Assert - should return posts, not unauthorized
        Assert.NotNull(result.Value);
        Assert.Null(result.Result); // No error result
    }

    [Fact]
    public async Task PublicEndpoints_GetPostById_SucceedsWithoutAuth()
    {
        // Arrange
        ClearAuthHeaders();
        var posts = await SeedPostsAsync(1);

        // Act
        var result = await Controller.GetPost(posts.First().Id);

        // Assert
        Assert.NotNull(result.Value);
    }

    #endregion

    #region Protected Endpoints (Auth Required)

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ProtectedEndpoints_GetMyPosts_FailsWithEmptyOrNullUserId(string? userId)
    {
        // Arrange
        if (userId != null)
        {
            Controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        }

        // Act
        var result = await Controller.GetMyPosts();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ProtectedEndpoints_CreatePost_FailsWithEmptyOrNullUserId(string? userId)
    {
        // Arrange
        if (userId != null)
        {
            Controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        }
        var dto = new CreatePostDto { Caption = "Test" };

        // Act
        var result = await Controller.CreatePost(dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ProtectedEndpoints_UpdatePost_FailsWithEmptyOrNullUserId(string? userId)
    {
        // Arrange
        var post = await CreateTestPostAsync("owner", "owner", "Test");
        if (userId != null)
        {
            Controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        }
        var dto = new UpdatePostDto { Id = post.Id, Caption = "Updated" };

        // Act
        var result = await Controller.UpdatePost(post.Id, dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ProtectedEndpoints_DeletePost_FailsWithEmptyOrNullUserId(string? userId)
    {
        // Arrange
        var post = await CreateTestPostAsync("owner", "owner", "Test");
        if (userId != null)
        {
            Controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
        }

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region Ownership Checks

    [Fact]
    public async Task Ownership_UserCanOnlyUpdateOwnPosts()
    {
        // Arrange
        var ownerPost = await CreateTestPostAsync("owner-123", "owner", "Owner's post");
        var otherPost = await CreateTestPostAsync("other-456", "other", "Other's post");

        SetAuthHeaders("owner-123", "owner");

        // Act & Assert - can update own post
        var ownResult = await Controller.UpdatePost(ownerPost.Id, 
            new UpdatePostDto { Caption = "Updated" });
        Assert.IsType<NoContentResult>(ownResult);

        // Act & Assert - cannot update other's post
        var otherResult = await Controller.UpdatePost(otherPost.Id, 
            new UpdatePostDto { Caption = "Hacked" });
        Assert.IsType<ForbidResult>(otherResult);
    }

    [Fact]
    public async Task Ownership_UserCanOnlyDeleteOwnPosts()
    {
        // Arrange
        var ownerPost = await CreateTestPostAsync("owner-123", "owner", "Owner's post");
        var otherPost = await CreateTestPostAsync("other-456", "other", "Other's post");

        SetAuthHeaders("owner-123", "owner");

        // Act & Assert - can delete own post
        var ownResult = await Controller.DeletePost(ownerPost.Id);
        Assert.IsType<NoContentResult>(ownResult);

        // Act & Assert - cannot delete other's post
        var otherResult = await Controller.DeletePost(otherPost.Id);
        Assert.IsType<ForbidResult>(otherResult);
    }

    [Fact]
    public async Task Ownership_UserCanViewAllPosts_ButOnlyModifyOwn()
    {
        // Arrange
        var userId = "viewer-user";
        await CreateTestPostAsync("user-1", "user1", "Post 1");
        await CreateTestPostAsync("user-2", "user2", "Post 2");
        await CreateTestPostAsync(userId, "viewer", "My post");

        SetAuthHeaders(userId, "viewer");

        // Act - can view all posts
        var allPosts = await Controller.GetPosts();
        Assert.Equal(3, allPosts.Value!.Count());

        // Act - my-posts only returns own
        var myPosts = await Controller.GetMyPosts();
        var myPostsList = Assert.IsType<OkObjectResult>(myPosts.Result);
        var posts = Assert.IsAssignableFrom<IEnumerable<PostService.Models.Post>>(myPostsList.Value);
        Assert.Single(posts);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Authorization_ValidUserIdButDifferentFormats_WorksCorrectly()
    {
        // Arrange - Test with UUID format (like Keycloak uses)
        var uuid = "550e8400-e29b-41d4-a716-446655440000";
        var post = await CreateTestPostAsync(uuid, "uuiduser", "UUID user post");

        SetAuthHeaders(uuid, "uuiduser");

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Authorization_CaseSensitiveUserIds()
    {
        // Arrange
        var post = await CreateTestPostAsync("User-123", "user", "Test post");

        // Try with different case
        SetAuthHeaders("user-123", "user"); // lowercase

        // Act
        var result = await Controller.DeletePost(post.Id);

        // Assert - should be forbidden (case matters)
        Assert.IsType<ForbidResult>(result);
    }

    #endregion
}
