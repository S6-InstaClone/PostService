using PostService.Models;
using System.ComponentModel.DataAnnotations;

namespace PostService.Tests;

/// <summary>
/// Tests for the Post model validation and properties
/// </summary>
public class PostModelTests
{
    [Fact]
    public void Post_DefaultValues_AreCorrect()
    {
        // Act
        var post = new Post();

        // Assert
        Assert.Equal(0, post.Id);
        Assert.Equal(string.Empty, post.UserId);
        Assert.Equal(string.Empty, post.Caption);
        Assert.Null(post.Username);
        Assert.Null(post.ImageUrl);
    }

    [Fact]
    public void Post_CanSetAllProperties()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var post = new Post
        {
            Id = 1,
            UserId = "user-123",
            Username = "testuser",
            Caption = "Test caption",
            ImageUrl = "http://example.com/image.jpg",
            CreatedAt = createdAt
        };

        // Assert
        Assert.Equal(1, post.Id);
        Assert.Equal("user-123", post.UserId);
        Assert.Equal("testuser", post.Username);
        Assert.Equal("Test caption", post.Caption);
        Assert.Equal("http://example.com/image.jpg", post.ImageUrl);
        Assert.Equal(createdAt, post.CreatedAt);
    }

    [Fact]
    public void Post_UserId_HasMaxLengthAttribute()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.UserId));
        var attribute = property?.GetCustomAttributes(typeof(MaxLengthAttribute), false)
            .FirstOrDefault() as MaxLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(36, attribute.Length); // UUID length
    }

    [Fact]
    public void Post_Caption_HasMaxLengthAttribute()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.Caption));
        var attribute = property?.GetCustomAttributes(typeof(MaxLengthAttribute), false)
            .FirstOrDefault() as MaxLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(2200, attribute.Length); // Instagram-like caption limit
    }

    [Fact]
    public void Post_Username_HasMaxLengthAttribute()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.Username));
        var attribute = property?.GetCustomAttributes(typeof(MaxLengthAttribute), false)
            .FirstOrDefault() as MaxLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(150, attribute.Length);
    }

    [Fact]
    public void Post_UserId_IsRequired()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.UserId));
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false)
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Post_Caption_IsRequired()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.Caption));
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false)
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Post_ImageUrl_IsOptional()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.ImageUrl));
        var requiredAttribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false)
            .FirstOrDefault();

        // Assert - no Required attribute means it's optional
        Assert.Null(requiredAttribute);
    }

    [Fact]
    public void Post_Username_IsOptional()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.Username));
        var requiredAttribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false)
            .FirstOrDefault();

        // Assert
        Assert.Null(requiredAttribute);
    }

    [Fact]
    public void Post_Id_HasKeyAttribute()
    {
        // Arrange
        var property = typeof(Post).GetProperty(nameof(Post.Id));
        var attribute = property?.GetCustomAttributes(typeof(KeyAttribute), false)
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }
}
