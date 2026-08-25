using System.Security.Claims;
using API.Controllers;
using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Projects;
using API.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.ProjectTests;

public class ProjectControllerTests
{
    private readonly Mock<IProjectService> _serviceMock;
    private readonly ProjectController _controller;
    private readonly Guid _authenticatedUserId = Guid.NewGuid();

    public ProjectControllerTests()
    {
        _serviceMock = new Mock<IProjectService>();
        _controller = new ProjectController(_serviceMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _authenticatedUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static ProjectDto MakeDto(Guid id, string title) =>
        new ProjectDto(new Project { Id = id, Title = title });

    [Fact]
    public async Task CreateProject_UsesAuthenticatedUserIdAsCreator_ReturnsCreatedAtAction()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "New Project");
        _serviceMock.Setup(s => s.CreateProject(It.IsAny<CreateProjectRequest>(), _authenticatedUserId))
            .ReturnsAsync(dto);

        var req = new CreateProjectRequest("New Project", null, null);
        var result = await _controller.CreateProject(req);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProjectController.GetProjectById), createdResult.ActionName);
        _serviceMock.Verify(s => s.CreateProject(req, _authenticatedUserId), Times.Once);
    }

    [Fact]
    public async Task GetProjectById_WithExistingProject_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "Existing");
        _serviceMock.Setup(s => s.GetProjectById(id)).ReturnsAsync(dto);

        var result = await _controller.GetProjectById(id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetProjectById_WithNonexistentProject_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetProjectById(id)).ThrowsAsync(new ProjectNotFoundException(id));

        var result = await _controller.GetProjectById(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAllProjects_ReturnsOkWithList()
    {
        var dtos = new List<ProjectDto> { MakeDto(Guid.NewGuid(), "P1"), MakeDto(Guid.NewGuid(), "P2") };
        _serviceMock.Setup(s => s.GetAllProjects()).ReturnsAsync(dtos);

        var result = await _controller.GetAllProjects();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProject_WithExistingProject_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "Updated");
        _serviceMock.Setup(s => s.UpdateProject(id, It.IsAny<UpdateProjectRequest>(), _authenticatedUserId))
            .ReturnsAsync(dto);

        var req = new UpdateProjectRequest("Updated", null, null);
        var result = await _controller.UpdateProject(id, req);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProject_WhenCallerNotParticipant_ReturnsForbidden()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.UpdateProject(id, It.IsAny<UpdateProjectRequest>(), _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("You must be a participant of this project to perform this action."));

        var req = new UpdateProjectRequest("Updated", null, null);
        var result = await _controller.UpdateProject(id, req);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WithNonexistentProject_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.UpdateProject(id, It.IsAny<UpdateProjectRequest>(), _authenticatedUserId))
            .ThrowsAsync(new ProjectNotFoundException(id));

        var req = new UpdateProjectRequest("Updated", null, null);
        var result = await _controller.UpdateProject(id, req);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteProject_WithExistingProject_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveProject(id, _authenticatedUserId)).ReturnsAsync(true);

        var result = await _controller.DeleteProject(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteProject_WhenCallerNotParticipant_ReturnsForbidden()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveProject(id, _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("You must be a participant of this project to perform this action."));

        var result = await _controller.DeleteProject(id);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithNonexistentProject_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveProject(id, _authenticatedUserId))
            .ThrowsAsync(new ProjectNotFoundException(id));

        var result = await _controller.DeleteProject(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddParticipant_Success_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = MakeDto(projectId, "P");
        _serviceMock.Setup(s => s.AddParticipant(projectId, userId, _authenticatedUserId)).ReturnsAsync(dto);

        var result = await _controller.AddParticipant(projectId, userId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddParticipant_WhenCallerNotParticipant_ReturnsForbidden()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _serviceMock.Setup(s => s.AddParticipant(projectId, userId, _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("You must be a participant of this project to perform this action."));

        var result = await _controller.AddParticipant(projectId, userId);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task AddParticipant_ProjectNotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _serviceMock.Setup(s => s.AddParticipant(projectId, userId, _authenticatedUserId))
            .ThrowsAsync(new ProjectNotFoundException(projectId));

        var result = await _controller.AddParticipant(projectId, userId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddParticipant_UserNotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _serviceMock.Setup(s => s.AddParticipant(projectId, userId, _authenticatedUserId))
            .ThrowsAsync(new UserNotFoundException(userId));

        var result = await _controller.AddParticipant(projectId, userId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemoveParticipant_Success_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = MakeDto(projectId, "P");
        _serviceMock.Setup(s => s.RemoveParticipant(projectId, userId, _authenticatedUserId)).ReturnsAsync(dto);

        var result = await _controller.RemoveParticipant(projectId, userId);

        Assert.IsType<OkObjectResult>(result);
    }
}