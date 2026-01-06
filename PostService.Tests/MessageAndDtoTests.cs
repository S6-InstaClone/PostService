using AccountService.Messages;
using PostService.Dtos;

namespace PostService.Tests;

/// <summary>
/// Tests for Messages and DTOs
/// </summary>
public class MessageAndDtoTests
{
    #region AccountDeletedEvent Tests

    [Fact]
    public void AccountDeletedEvent_DefaultValues()
    {
        // Act
        var evt = new AccountDeletedEvent();

        // Assert
        Assert.Equal(string.Empty, evt.UserId);
        Assert.Null(evt.Username);
        Assert.Null(evt.Email);
        Assert.Equal(default(DateTime), evt.DeletedAt);
        Assert.Equal(string.Empty, evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetUserId()
    {
        // Act
        var evt = new AccountDeletedEvent { UserId = "user-123" };

        // Assert
        Assert.Equal("user-123", evt.UserId);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetUsername()
    {
        // Act
        var evt = new AccountDeletedEvent { Username = "testuser" };

        // Assert
        Assert.Equal("testuser", evt.Username);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetEmail()
    {
        // Act
        var evt = new AccountDeletedEvent { Email = "test@example.com" };

        // Assert
        Assert.Equal("test@example.com", evt.Email);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetDeletedAt()
    {
        // Arrange
        var deletedAt = DateTime.UtcNow;

        // Act
        var evt = new AccountDeletedEvent { DeletedAt = deletedAt };

        // Assert
        Assert.Equal(deletedAt, evt.DeletedAt);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetReason()
    {
        // Act
        var evt = new AccountDeletedEvent { Reason = "GDPR_USER_REQUEST" };

        // Assert
        Assert.Equal("GDPR_USER_REQUEST", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_CanSetAllProperties()
    {
        // Arrange
        var deletedAt = DateTime.UtcNow;

        // Act
        var evt = new AccountDeletedEvent
        {
            UserId = "user-456",
            Username = "johndoe",
            Email = "john@example.com",
            DeletedAt = deletedAt,
            Reason = "ADMIN_ACTION"
        };

        // Assert
        Assert.Equal("user-456", evt.UserId);
        Assert.Equal("johndoe", evt.Username);
        Assert.Equal("john@example.com", evt.Email);
        Assert.Equal(deletedAt, evt.DeletedAt);
        Assert.Equal("ADMIN_ACTION", evt.Reason);
    }

    [Fact]
    public void AccountDeletedEvent_IsRecord_HasValueEquality()
    {
        // Arrange
        var deletedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        
        var evt1 = new AccountDeletedEvent
        {
            UserId = "user-1",
            Username = "test",
            Email = "test@test.com",
            DeletedAt = deletedAt,
            Reason = "TEST"
        };

        var evt2 = new AccountDeletedEvent
        {
            UserId = "user-1",
            Username = "test",
            Email = "test@test.com",
            DeletedAt = deletedAt,
            Reason = "TEST"
        };

        // Assert - Records have value equality
        Assert.Equal(evt1, evt2);
    }

    [Fact]
    public void AccountDeletedEvent_DifferentValues_NotEqual()
    {
        // Arrange
        var evt1 = new AccountDeletedEvent { UserId = "user-1" };
        var evt2 = new AccountDeletedEvent { UserId = "user-2" };

        // Assert
        Assert.NotEqual(evt1, evt2);
    }

    #endregion

    #region CreatePostDto Tests

    [Fact]
    public void CreatePostDto_DefaultValues()
    {
        // Act
        var dto = new CreatePostDto();

        // Assert
        Assert.Equal(string.Empty, dto.Caption);
        Assert.Null(dto.File);
    }

    [Fact]
    public void CreatePostDto_CanSetCaption()
    {
        // Act
        var dto = new CreatePostDto { Caption = "Test caption" };

        // Assert
        Assert.Equal("Test caption", dto.Caption);
    }

    [Fact]
    public void CreatePostDto_FileIsOptional()
    {
        // Act
        var dto = new CreatePostDto { Caption = "No file" };

        // Assert
        Assert.Null(dto.File);
    }

    #endregion

    #region UpdatePostDto Tests

    [Fact]
    public void UpdatePostDto_DefaultValues()
    {
        // Act
        var dto = new UpdatePostDto();

        // Assert
        Assert.Equal(0, dto.Id);
        Assert.Null(dto.Caption);
        Assert.Null(dto.File);
    }

    [Fact]
    public void UpdatePostDto_CanSetId()
    {
        // Act
        var dto = new UpdatePostDto { Id = 42 };

        // Assert
        Assert.Equal(42, dto.Id);
    }

    [Fact]
    public void UpdatePostDto_CanSetCaption()
    {
        // Act
        var dto = new UpdatePostDto { Caption = "Updated caption" };

        // Assert
        Assert.Equal("Updated caption", dto.Caption);
    }

    [Fact]
    public void UpdatePostDto_CanSetAllProperties()
    {
        // Act
        var dto = new UpdatePostDto
        {
            Id = 123,
            Caption = "Full update"
        };

        // Assert
        Assert.Equal(123, dto.Id);
        Assert.Equal("Full update", dto.Caption);
    }

    #endregion
}
