using API.Controllers.Users;
using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.Users;
using API.Service;
using API.Services;
using API.Users;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.UserTests;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _serviceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _serviceMock = new Mock<IUserService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _controller = new AuthController(_serviceMock.Object, _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokenAndUser()
    {
        var dto = new UserDto(new User { Username = "alice", PasswordHash = "hash" });
        var request = new LoginSignupRequest("alice", "password");
        _serviceMock.Setup(s => s.LoginUser(request)).ReturnsAsync(dto);
        _tokenServiceMock.Setup(t => t.GenerateToken(dto.userId, dto.Username)).Returns("fake-jwt");

        var result = await _controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("fake-jwt", response.Token);
        Assert.Equal("alice", response.User.Username);
    }

    [Fact]
    public async Task Login_WithBadCredentials_ReturnsUnauthorized()
    {
        var request = new LoginSignupRequest("alice", "wrong");
        _serviceMock.Setup(s => s.LoginUser(request))
            .ThrowsAsync(new BadLoginException("Failed Login Attempt"));

        var result = await _controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task SignUp_WithNewUsername_ReturnsOkWithTokenAndUser()
    {
        var dto = new UserDto(new User { Username = "newuser", PasswordHash = "hash" });
        var request = new LoginSignupRequest("newuser", "password");
        _serviceMock.Setup(s => s.RegisterUser(request)).ReturnsAsync(dto);
        _tokenServiceMock.Setup(t => t.GenerateToken(dto.userId, dto.Username)).Returns("fake-jwt");

        var result = await _controller.SignUp(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("fake-jwt", response.Token);
    }

    [Fact]
    public async Task SignUp_WithExistingUsername_ReturnsConflict()
    {
        var request = new LoginSignupRequest("taken", "password");
        _serviceMock.Setup(s => s.RegisterUser(request))
            .ThrowsAsync(new UserExistsException("Username already exists"));

        var result = await _controller.SignUp(request);

        Assert.IsType<ConflictObjectResult>(result);
    }
}