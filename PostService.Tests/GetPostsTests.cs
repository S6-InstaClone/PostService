using Microsoft.AspNetCore.Mvc;
using PostService.Models;

namespace PostService.Tests;

/// <summary>
/// Tests for GET endpoints (GetPosts, GetPost, GetMyPosts)
/// </summary>
public class GetPostsTests : PostsControllerTestBase
{
    #region GET /api/posts

    [Fact]
    public async Task GetPosts_ReturnsEmptyList_WhenNoPosts()
    {
        // Act
        var result = await Controller.GetPosts();

        // Assert
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(result.Value);
        Assert.Empty(posts);
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_OrderedByCreatedAtDescending()
    {
        // Arrange
        await SeedPostsAsync(3);

        // Act
        var result = await Controller.GetPosts();

        // Assert
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(result.Value);
        var postList = posts.ToList();
        
        Assert.Equal(3, postList.Count);
        // Verify descending order (most recent first)
        Assert.True(postList[0].CreatedAt >= postList[1].CreatedAt);
        Assert.True(postList[1].CreatedAt >= postList[2].CreatedAt);
    }

    [Fact]
    public async Task GetPosts_DoesNotRequireAuthentication()
    {
        // Arrange - no auth headers set
        ClearAuthHeaders();
        await SeedPostsAsync(2);

        // Act
        var result = await Controller.GetPosts();

        // Assert - should succeed without auth
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(result.Value);
        Assert.Equal(2, posts.Count());
    }

    #endregion

    #region GET /api/posts/{id}

    [Fact]
    public async Task GetPost_ReturnsPost_WhenExists()
    {
        // Arrange
        var seededPosts = await SeedPostsAsync(1);
        var expectedPost = seededPosts.First();

        // Act
        var result = await Controller.GetPost(expectedPost.Id);

        // Assert
        var post = Assert.IsType<Post>(result.Value);
        Assert.Equal(expectedPost.Id, post.Id);
        Assert.Equal(expectedPost.Caption, post.Caption);
        Assert.Equal(expectedPost.UserId, post.UserId);
    }

    [Fact]
    public async Task GetPost_ReturnsNotFound_WhenPostDoesNotExist()
    {
        // Act
        var result = await Controller.GetPost(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPost_DoesNotRequireAuthentication()
    {
        // Arrange
        ClearAuthHeaders();
        var seededPosts = await SeedPostsAsync(1);

        // Act
        var result = await Controller.GetPost(seededPosts.First().Id);

        // Assert
        Assert.IsType<Post>(result.Value);
    }

    #endregion

    #region GET /api/posts/my-posts

    [Fact]
    public async Task GetMyPosts_ReturnsUnauthorized_WhenNoAuthHeader()
    {
        // Arrange
        ClearAuthHeaders();

        // Act
        var result = await Controller.GetMyPosts();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMyPosts_ReturnsOnlyUsersPosts()
    {
        // Arrange
        var userId = "my-user-id";
        SetAuthHeaders(userId, "myuser");

        // Create posts for different users
        await CreateTestPostAsync(userId, "myuser", "My post 1");
        await CreateTestPostAsync(userId, "myuser", "My post 2");
        await CreateTestPostAsync("other-user", "otheruser", "Other's post");

        // Act
        var result = await Controller.GetMyPosts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(okResult.Value);
        var postList = posts.ToList();

        Assert.Equal(2, postList.Count);
        Assert.All(postList, p => Assert.Equal(userId, p.UserId));
    }

    [Fact]
    public async Task GetMyPosts_ReturnsEmptyList_WhenUserHasNoPosts()
    {
        // Arrange
        SetAuthHeaders("user-with-no-posts", "newuser");
        await SeedPostsAsync(3); // Posts belong to other users

        // Act
        var result = await Controller.GetMyPosts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(okResult.Value);
        Assert.Empty(posts);
    }

    [Fact]
    public async Task GetMyPosts_ReturnsPostsOrderedByCreatedAtDescending()
    {
        // Arrange
        var userId = "test-user";
        SetAuthHeaders(userId, "testuser");

        // Create posts with different timestamps
        var post1 = await CreateTestPostAsync(userId, "testuser", "Oldest");
        await Task.Delay(10); // Small delay to ensure different timestamps
        var post2 = await CreateTestPostAsync(userId, "testuser", "Middle");
        await Task.Delay(10);
        var post3 = await CreateTestPostAsync(userId, "testuser", "Newest");

        // Act
        var result = await Controller.GetMyPosts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var posts = Assert.IsAssignableFrom<IEnumerable<Post>>(okResult.Value).ToList();

        Assert.Equal(3, posts.Count);
        Assert.Equal("Newest", posts[0].Caption);
        Assert.Equal("Oldest", posts[2].Caption);
    }

    #endregion
}
