using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Projects;
using API.Repositories;
using API.Service;
using API.Users;
using Moq;

namespace API.Tests.ProjectTests;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _projectRepoMock = new Mock<IProjectRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _service = new ProjectService(_projectRepoMock.Object, _userRepoMock.Object);
    }

    // ---- CreateProject ----

    [Fact]
    public async Task CreateProject_CreatesAndReturnsDto()
    {
        _projectRepoMock.Setup(r => r.AddProject(It.IsAny<Project>()))
            .Returns(Task.CompletedTask);

        var req = new CreateProjectRequest("New Project", "desc", null);

        var result = await _service.CreateProject(req);

        Assert.Equal("New Project", result.Title);
        _projectRepoMock.Verify(r => r.AddProject(It.Is<Project>(p => p.Title == "New Project")), Times.Once);
    }

    // ---- GetProjectById ----

    [Fact]
    public async Task GetProjectById_WithExistingProject_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var project = new Project { Id = id, Title = "Existing" };
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync(project);

        var result = await _service.GetProjectById(id);

        Assert.Equal("Existing", result.Title);
    }

    [Fact]
    public async Task GetProjectById_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var id = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.GetProjectById(id));
    }

    // ---- GetAllProjects ----

    [Fact]
    public async Task GetAllProjects_ReturnsAllAsDtos()
    {
        var projects = new List<Project>
        {
            new() { Title = "P1" },
            new() { Title = "P2" }
        };
        _projectRepoMock.Setup(r => r.GetAllProjects()).ReturnsAsync(projects);

        var result = await _service.GetAllProjects();

        Assert.Equal(2, result.Count);
    }

    // ---- UpdateProject ----

    [Fact]
    public async Task UpdateProject_WithProvidedFields_UpdatesOnlyThoseFields()
    {
        var id = Guid.NewGuid();
        var original = new Project { Id = id, Title = "Old Title", Description = "Old Desc" };
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync(original);
        _projectRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var req = new UpdateProjectRequest("New Title", null, null);

        var result = await _service.UpdateProject(id, req);

        Assert.Equal("New Title", result.Title);
        Assert.Equal("Old Desc", result.Description); // untouched since req.Description was null
    }

    [Fact]
    public async Task UpdateProject_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var id = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync((Project?)null);

        var req = new UpdateProjectRequest("New Title", null, null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.UpdateProject(id, req));
    }

    // ---- RemoveProject ----

    [Fact]
    public async Task RemoveProject_WithExistingProject_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.RemoveProject(id)).ReturnsAsync(true);

        var result = await _service.RemoveProject(id);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveProject_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var id = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.RemoveProject(id)).ReturnsAsync(false);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.RemoveProject(id));
    }

    // ---- AddParticipant ----

    [Fact]
    public async Task AddParticipant_WithValidProjectAndUser_AddsParticipant()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" };
        var user = new User { Id = userId, Username = "alice", PasswordHash = "hash" };

        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(userId)).ReturnsAsync(user);
        _projectRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.AddParticipant(projectId, userId);

        Assert.Contains(userId, result.ParticipantIds);
        _projectRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddParticipant_AlreadyParticipant_DoesNotDuplicateOrSave()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "alice", PasswordHash = "hash" };
        var project = new Project { Id = projectId, Title = "P" };
        project.Participants.Add(user);

        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(userId)).ReturnsAsync(user);

        var result = await _service.AddParticipant(projectId, userId);

        Assert.Single(result.ParticipantIds);
        _projectRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddParticipant_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.AddParticipant(projectId, userId));
    }

    [Fact]
    public async Task AddParticipant_WithNonexistentUser_ThrowsUserNotFoundException()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" };
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(userId)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _service.AddParticipant(projectId, userId));
    }

    // ---- RemoveParticipant ----

    [Fact]
    public async Task RemoveParticipant_RemovesExistingParticipant()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "alice", PasswordHash = "hash" };
        var project = new Project { Id = projectId, Title = "P" };
        project.Participants.Add(user);

        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(userId)).ReturnsAsync(user);
        _projectRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.RemoveParticipant(projectId, userId);

        Assert.DoesNotContain(userId, result.ParticipantIds);
    }

    [Fact]
    public async Task RemoveParticipant_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.RemoveParticipant(projectId, userId));
    }

    [Fact]
    public async Task RemoveParticipant_WithNonexistentUser_ThrowsUserNotFoundException()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" };
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(userId)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _service.RemoveParticipant(projectId, userId));
    }
}