using API.Controllers;
using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Projects;
using API.Service;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.ProjectTests;

public class ProjectControllerTests
{
    private readonly Mock<IProjectService> _serviceMock;
    private readonly ProjectController _controller;

    public ProjectControllerTests()
    {
        _serviceMock = new Mock<IProjectService>();
        _controller = new ProjectController(_serviceMock.Object);
    }

    private static ProjectDto MakeDto(Guid id, string title) =>
        new ProjectDto(new Project { Id = id, Title = title });

    [Fact]
    public async Task CreateProject_ReturnsCreatedAtActionWithDto()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "New Project");
        _serviceMock.Setup(s => s.CreateProject(It.IsAny<CreateProjectRequest>())).ReturnsAsync(dto);

        var req = new CreateProjectRequest("New Project", null, null);
        var result = await _controller.CreateProject(req);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProjectController.GetProjectById), createdResult.ActionName);
        var returnedDto = Assert.IsType<ProjectDto>(createdResult.Value);
        Assert.Equal("New Project", returnedDto.Title);
    }

    [Fact]
    public async Task GetProjectById_WithExistingProject_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "Existing");
        _serviceMock.Setup(s => s.GetProjectById(id)).ReturnsAsync(dto);

        var result = await _controller.GetProjectById(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<ProjectDto>(okResult.Value);
        Assert.Equal("Existing", returnedDto.Title);
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

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<ProjectDto>>(okResult.Value);
        Assert.Equal(2, returned.Count);
    }

    [Fact]
    public async Task UpdateProject_WithExistingProject_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "Updated");
        _serviceMock.Setup(s => s.UpdateProject(id, It.IsAny<UpdateProjectRequest>())).ReturnsAsync(dto);

        var req = new UpdateProjectRequest("Updated", null, null);
        var result = await _controller.UpdateProject(id, req);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<ProjectDto>(okResult.Value);
        Assert.Equal("Updated", returnedDto.Title);
    }

    [Fact]
    public async Task UpdateProject_WithNonexistentProject_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.UpdateProject(id, It.IsAny<UpdateProjectRequest>()))
            .ThrowsAsync(new ProjectNotFoundException(id));

        var req = new UpdateProjectRequest("Updated", null, null);
        var result = await _controller.UpdateProject(id, req);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteProject_WithExistingProject_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveProject(id)).ReturnsAsync(true);

        var result = await _controller.DeleteProject(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteProject_WithNonexistentProject_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveProject(id)).ThrowsAsync(new ProjectNotFoundException(id));

        var result = await _controller.DeleteProject(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddParticipant_Success_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = MakeDto(projectId, "P");
        _serviceMock.Setup(s => s.AddParticipant(projectId, userId)).ReturnsAsync(dto);

        var result = await _controller.AddParticipant(projectId, userId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddParticipant_ProjectNotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _serviceMock.Setup(s => s.AddParticipant(projectId, userId))
            .ThrowsAsync(new ProjectNotFoundException(projectId));

        var result = await _controller.AddParticipant(projectId, userId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddParticipant_UserNotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _serviceMock.Setup(s => s.AddParticipant(projectId, userId))
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
        _serviceMock.Setup(s => s.RemoveParticipant(projectId, userId)).ReturnsAsync(dto);

        var result = await _controller.RemoveParticipant(projectId, userId);

        Assert.IsType<OkObjectResult>(result);
    }
}