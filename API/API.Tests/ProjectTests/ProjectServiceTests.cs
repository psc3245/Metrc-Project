using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Projects;
using API.Repositories;
using API.Service;
using API.Users;
using Moq;
using Xunit;

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
    public async Task CreateProject_AddsCreatorAsParticipant()
    {
        var creatorId = Guid.NewGuid();
        var creator = new User { Id = creatorId, Username = "creator", PasswordHash = "hash" };
        _userRepoMock.Setup(r => r.GetUserByUserId(creatorId)).ReturnsAsync(creator);
        _projectRepoMock.Setup(r => r.AddProject(It.IsAny<Project>())).Returns(Task.CompletedTask);

        var req = new CreateProjectRequest("New Project", "desc", null);

        var result = await _service.CreateProject(req, creatorId);

        Assert.Equal("New Project", result.Title);
        Assert.Contains(creatorId, result.ParticipantIds);
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
        var projects = new List<Project> { new() { Title = "P1" }, new() { Title = "P2" } };
        _projectRepoMock.Setup(r => r.GetAllProjects()).ReturnsAsync(projects);

        var result = await _service.GetAllProjects();

        Assert.Equal(2, result.Count);
    }

    // ---- UpdateProject ----

    [Fact]
    public async Task UpdateProject_AsParticipant_UpdatesOnlyProvidedFields()
    {
        var id = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var caller = new User { Id = callerId, Username = "caller", PasswordHash = "hash" };
        var project = new Project { Id = id, Title = "Old Title", Description = "Old Desc" };
        project.Participants.Add(caller);

        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync(project);
        _projectRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var req = new UpdateProjectRequest("New Title", null, null);
        var result = await _service.UpdateProject(id, req, callerId);

        Assert.Equal("New Title", result.Title);
        Assert.Equal("Old Desc", result.Description);
    }

    [Fact]
    public async Task UpdateProject_AsNonParticipant_ThrowsForbiddenException()
    {
        var id = Guid.NewGuid();
        var callerId = Guid.NewGuid(); // not added as a participant
        var project = new Project { Id = id, Title = "Old Title" };
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync(project);

        var req = new UpdateProjectRequest("New Title", null, null);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateProject(id, req, callerId));
    }

    [Fact]
    public async Task UpdateProject_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var id = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync((Project?)null);

        var req = new UpdateProjectRequest("New Title", null, null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.UpdateProject(id, req, Guid.NewGuid()));
    }

    // ---- RemoveProject ----

    [Fact]
    public async Task RemoveProject_AsParticipant_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var caller = new User { Id = callerId, Username = "caller", PasswordHash = "hash" };
        var project = new Project { Id = id, Title = "P" };
        project.Participants.Add(caller);

        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync(project);
        _projectRepoMock.Setup(r => r.RemoveProject(id)).ReturnsAsync(true);

        var result = await _service.RemoveProject(id, callerId);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveProject_AsNonParticipant_ThrowsForbiddenException()
    {
        var id = Guid.NewGuid();
        var project = new Project { Id = id, Title = "P" };
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync(project);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.RemoveProject(id, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveProject_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var id = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(id)).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.RemoveProject(id, Guid.NewGuid()));
    }

    // ---- AddParticipant ----

    [Fact]
    public async Task AddParticipant_AsExistingParticipant_AddsNewParticipant()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var caller = new User { Id = callerId, Username = "caller", PasswordHash = "hash" };
        var newUser = new User { Id = newUserId, Username = "newbie", PasswordHash = "hash" };
        var project = new Project { Id = projectId, Title = "P" };
        project.Participants.Add(caller);

        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(newUserId)).ReturnsAsync(newUser);
        _projectRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.AddParticipant(projectId, newUserId, callerId);

        Assert.Contains(newUserId, result.ParticipantIds);
    }

    [Fact]
    public async Task AddParticipant_AsNonParticipant_ThrowsForbiddenException()
    {
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" };
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.AddParticipant(projectId, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task AddParticipant_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var projectId = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(
            () => _service.AddParticipant(projectId, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task AddParticipant_WithNonexistentUser_ThrowsUserNotFoundException()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var caller = new User { Id = callerId, Username = "caller", PasswordHash = "hash" };
        var project = new Project { Id = projectId, Title = "P" };
        project.Participants.Add(caller);
        var newUserId = Guid.NewGuid();

        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(newUserId)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _service.AddParticipant(projectId, newUserId, callerId));
    }

    // ---- RemoveParticipant ----

    [Fact]
    public async Task RemoveParticipant_AsExistingParticipant_RemovesTarget()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var caller = new User { Id = callerId, Username = "caller", PasswordHash = "hash" };
        var target = new User { Id = targetId, Username = "target", PasswordHash = "hash" };
        var project = new Project { Id = projectId, Title = "P" };
        project.Participants.Add(caller);
        project.Participants.Add(target);

        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(targetId)).ReturnsAsync(target);
        _projectRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.RemoveParticipant(projectId, targetId, callerId);

        Assert.DoesNotContain(targetId, result.ParticipantIds);
    }

    [Fact]
    public async Task RemoveParticipant_AsNonParticipant_ThrowsForbiddenException()
    {
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" };
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.RemoveParticipant(projectId, Guid.NewGuid(), Guid.NewGuid()));
    }
}