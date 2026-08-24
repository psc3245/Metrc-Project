using API.Controllers.Users;
using API.Entities.Exceptions;
using API.Entities.Users;
using API.Service;
using API.Users;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.UserTests;

public class UserControllerTests
{
    private readonly Mock<IUserService> _serviceMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _serviceMock = new Mock<IUserService>();
        _controller = new UserController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ReturnsOkWithUser()
    {
        var id = Guid.NewGuid();
        var dto = new UserDto(new User { Id = id, Username = "alice", PasswordHash = "hash" });
        _serviceMock.Setup(s => s.GetUserByUserId(id)).ReturnsAsync(dto);

        var result = await _controller.GetUserById(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("alice", returnedDto.Username);
    }

    [Fact]
    public async Task GetUserById_WithNonexistentUser_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetUserByUserId(id))
            .ThrowsAsync(new UserNotFoundException(id));

        var result = await _controller.GetUserById(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetUserByUsername_WithExistingUser_ReturnsOk()
    {
        var dto = new UserDto(new User { Username = "bob", PasswordHash = "hash" });
        _serviceMock.Setup(s => s.GetUserByUsername("bob")).ReturnsAsync(dto);

        var result = await _controller.GetUserByUsername("bob");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("bob", returnedDto.Username);
    }

    [Fact]
    public async Task GetUserByUsername_WithNonexistentUser_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetUserByUsername("ghost"))
            .ThrowsAsync(new UserNotFoundException("ghost"));

        var result = await _controller.GetUserByUsername("ghost");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithList()
    {
        var users = new List<UserDto>
        {
            new(new User { Username = "u1", PasswordHash = "hash" }),
            new(new User { Username = "u2", PasswordHash = "hash" })
        };
        _serviceMock.Setup(s => s.GetUsers()).ReturnsAsync(users);

        var result = await _controller.GetAllUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Equal(2, returned.Count);
    }

    [Fact]
    public async Task DeleteUser_WithExistingUser_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveUser(id)).ReturnsAsync(true);

        var result = await _controller.DeleteUser(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WithNonexistentUser_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveUser(id))
            .ThrowsAsync(new UserNotFoundException(id));

        var result = await _controller.DeleteUser(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}