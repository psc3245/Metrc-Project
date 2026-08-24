using API.Entities.Exceptions;
using API.Entities.Users;
using API.Repositories;
using API.Service;
using API.Users;
using Moq;

namespace API.Tests.UserTests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _service = new UserService(_repoMock.Object);
    }

    // ---- RegisterUser ----

    [Fact]
    public async Task RegisterUser_WithNewUsername_CreatesUserAndReturnsDto()
    {
        // Arrange
        _repoMock.Setup(r => r.GetUserByUsername("newuser"))
            .ReturnsAsync((User?)null);
        _repoMock.Setup(r => r.AddUser(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        var request = new LoginSignupRequest("newuser", "Sup3rSecret!");

        // Act
        var result = await _service.RegisterUser(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser", result!.Username);
        _repoMock.Verify(r => r.AddUser(It.Is<User>(u => u.Username == "newuser")), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_WithExistingUsername_ThrowsUserExistsException()
    {
        // Arrange
        var existing = new User { Username = "taken", PasswordHash = "hash" };
        _repoMock.Setup(r => r.GetUserByUsername("taken"))
            .ReturnsAsync(existing);

        var request = new LoginSignupRequest("taken", "whatever");

        // Act & Assert
        await Assert.ThrowsAsync<UserExistsException>(() => _service.RegisterUser(request));
        _repoMock.Verify(r => r.AddUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUser_StoresHashedPassword_NotPlaintext()
    {
        // Arrange
        _repoMock.Setup(r => r.GetUserByUsername("newuser"))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        _repoMock.Setup(r => r.AddUser(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        var request = new LoginSignupRequest("newuser", "PlaintextPassword!");

        // Act
        await _service.RegisterUser(request);

        // Assert
        Assert.NotNull(capturedUser);
        Assert.NotEqual("PlaintextPassword!", capturedUser!.PasswordHash);
    }

    // ---- LoginUser ----

    [Fact]
    public async Task LoginUser_WithCorrectCredentials_ReturnsUserDto()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        var existing = new User { Username = "existinguser", PasswordHash = hash };
        _repoMock.Setup(r => r.GetUserByUsername("existinguser"))
            .ReturnsAsync(existing);

        var request = new LoginSignupRequest("existinguser", "correct-password");

        // Act
        var result = await _service.LoginUser(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("existinguser", result!.Username);
    }

    [Fact]
    public async Task LoginUser_WithWrongPassword_ThrowsBadLoginException()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        var existing = new User { Username = "existinguser", PasswordHash = hash };
        _repoMock.Setup(r => r.GetUserByUsername("existinguser"))
            .ReturnsAsync(existing);

        var request = new LoginSignupRequest("existinguser", "wrong-password");

        // Act & Assert
        await Assert.ThrowsAsync<BadLoginException>(() => _service.LoginUser(request));
    }

    [Fact]
    public async Task LoginUser_WithNonexistentUsername_ThrowsBadLoginException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetUserByUsername("ghost"))
            .ReturnsAsync((User?)null);

        var request = new LoginSignupRequest("ghost", "whatever");

        // Act & Assert
        await Assert.ThrowsAsync<BadLoginException>(() => _service.LoginUser(request));
    }

    // ---- GetUserByUserId ----

    [Fact]
    public async Task GetUserByUserId_WithExistingId_ReturnsUserDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new User { Id = id, Username = "someone", PasswordHash = "hash" };
        _repoMock.Setup(r => r.GetUserByUserId(id))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.GetUserByUserId(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("someone", result!.Username);
    }

    [Fact]
    public async Task GetUserByUserId_WithNonexistentId_ThrowsUserNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetUserByUserId(id))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => _service.GetUserByUserId(id));
    }

    // ---- GetUsers ----

    [Fact]
    public async Task GetUsers_ReturnsAllUsersAsDtos()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Username = "userA", PasswordHash = "hash" },
            new() { Username = "userB", PasswordHash = "hash" }
        };
        _repoMock.Setup(r => r.GetAllUsers()).ReturnsAsync(users);

        // Act
        var result = await _service.GetUsers();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Username == "userA");
        Assert.Contains(result, u => u.Username == "userB");
    }

    // ---- RemoveUser ----

    [Fact]
    public async Task RemoveUser_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.RemoveUser(id)).ReturnsAsync(true);

        // Act
        var result = await _service.RemoveUser(id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RemoveUser_WithNonexistentId_ThrowsUserNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.RemoveUser(id)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => _service.RemoveUser(id));
    }
}