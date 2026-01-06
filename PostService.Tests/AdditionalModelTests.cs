using PostService.Models;
using AccountService.Models;

namespace PostService.Tests;

/// <summary>
/// Tests for additional model classes in PostService
/// </summary>
public class AdditionalModelTests
{
    #region Profile Model Tests

    [Fact]
    public void Profile_DefaultConstructor_InitializesCorrectly()
    {
        // Act
        var profile = new Profile();

        // Assert
        Assert.Equal(0, profile.Id);
        Assert.Null(profile.Username);
        Assert.Null(profile.Name);
        Assert.Null(profile.Description);
        Assert.Null(profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_TwoParamConstructor_SetsUsernameAndName()
    {
        // Act
        var profile = new Profile("testuser", "Test User");

        // Assert
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
        Assert.Null(profile.Description);
    }

    [Fact]
    public void Profile_ThreeParamConstructor_SetsAllFields()
    {
        // Act
        var profile = new Profile("testuser", "Test User", "A description");

        // Assert
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.Name);
        Assert.Equal("A description", profile.Description);
        Assert.Equal("default_pr_pic", profile.ProfilePictureLink);
    }

    [Fact]
    public void Profile_CanSetId()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.Id = 42;

        // Assert
        Assert.Equal(42, profile.Id);
    }

    [Fact]
    public void Profile_CanSetUsername()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.Username = "newuser";

        // Assert
        Assert.Equal("newuser", profile.Username);
    }

    [Fact]
    public void Profile_CanSetName()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.Name = "New Name";

        // Assert
        Assert.Equal("New Name", profile.Name);
    }

    [Fact]
    public void Profile_CanSetDescription()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.Description = "New description";

        // Assert
        Assert.Equal("New description", profile.Description);
    }

    [Fact]
    public void Profile_CanSetProfilePictureLink()
    {
        // Arrange
        var profile = new Profile();

        // Act
        profile.ProfilePictureLink = "http://example.com/pic.jpg";

        // Assert
        Assert.Equal("http://example.com/pic.jpg", profile.ProfilePictureLink);
    }

    #endregion

    #region AccountData Model Tests

    [Fact]
    public void AccountData_DefaultValues()
    {
        // Act
        var account = new AccountData();

        // Assert
        Assert.Equal(0, account.Id);
        Assert.Null(account.Username);
        Assert.Null(account.Password);
        Assert.Null(account.Email);
    }

    [Fact]
    public void AccountData_CanSetId()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Id = 123;

        // Assert
        Assert.Equal(123, account.Id);
    }

    [Fact]
    public void AccountData_CanSetUsername()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Username = "testuser";

        // Assert
        Assert.Equal("testuser", account.Username);
    }

    [Fact]
    public void AccountData_CanSetPassword()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Password = "secret123";

        // Assert
        Assert.Equal("secret123", account.Password);
    }

    [Fact]
    public void AccountData_CanSetEmail()
    {
        // Arrange
        var account = new AccountData();

        // Act
        account.Email = "test@example.com";

        // Assert
        Assert.Equal("test@example.com", account.Email);
    }

    [Fact]
    public void AccountData_CanSetAllProperties()
    {
        // Act
        var account = new AccountData
        {
            Id = 1,
            Username = "user1",
            Password = "pass123",
            Email = "user1@example.com"
        };

        // Assert
        Assert.Equal(1, account.Id);
        Assert.Equal("user1", account.Username);
        Assert.Equal("pass123", account.Password);
        Assert.Equal("user1@example.com", account.Email);
    }

    #endregion
}
