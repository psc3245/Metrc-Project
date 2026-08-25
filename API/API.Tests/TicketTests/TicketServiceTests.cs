using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Projects;
using API.Repositories;
using API.Service;
using API.Tickets;
using API.Users;
using Moq;
using Xunit;

namespace API.Tests.TicketTests;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ITagRepository> _tagRepoMock;
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _ticketRepoMock = new Mock<ITicketRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _tagRepoMock = new Mock<ITagRepository>();
        _service = new TicketService(
            _ticketRepoMock.Object, _projectRepoMock.Object, _userRepoMock.Object, _tagRepoMock.Object);
    }

    private static (Project project, User caller) MakeProjectWithParticipant(Guid projectId, Guid callerId)
    {
        var caller = new User { Id = callerId, Username = "caller", PasswordHash = "hash" };
        var project = new Project { Id = projectId, Title = "P" };
        project.Participants.Add(caller);
        return (project, caller);
    }

    // ---- CreateTicket ----

    [Fact]
    public async Task CreateTicket_AsParticipant_CreatesTicketWithGivenAuthor()
    {
        var projectId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, authorId);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _ticketRepoMock.Setup(r => r.AddTicket(It.IsAny<Ticket>())).Returns(Task.CompletedTask);

        var req = new CreateTicketRequest("New Ticket", null, null, Priority.MEDIUM, projectId);

        var result = await _service.CreateTicket(req, authorId);

        Assert.Equal("New Ticket", result.Title);
        Assert.Equal(authorId, result.AuthorId);
    }

    [Fact]
    public async Task CreateTicket_AsNonParticipant_ThrowsForbiddenException()
    {
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" }; // no participants
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);

        var req = new CreateTicketRequest("New Ticket", null, null, Priority.MEDIUM, projectId);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.CreateTicket(req, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateTicket_WithNonexistentProject_ThrowsProjectNotFoundException()
    {
        var projectId = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync((Project?)null);

        var req = new CreateTicketRequest("New Ticket", null, null, Priority.MEDIUM, projectId);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.CreateTicket(req, Guid.NewGuid()));
    }

    // ---- GetTicketById ----

    [Fact]
    public async Task GetTicketById_WithExistingTicket_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var ticket = new Ticket { Id = id, Title = "T", ProjectId = Guid.NewGuid(), AuthorId = Guid.NewGuid() };
        _ticketRepoMock.Setup(r => r.GetTicketById(id)).ReturnsAsync(ticket);

        var result = await _service.GetTicketById(id);

        Assert.Equal("T", result.Title);
    }

    [Fact]
    public async Task GetTicketById_WithNonexistentTicket_ThrowsTicketNotFoundException()
    {
        var id = Guid.NewGuid();
        _ticketRepoMock.Setup(r => r.GetTicketById(id)).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<TicketNotFoundException>(() => _service.GetTicketById(id));
    }

    // ---- UpdateTicket ----

    [Fact]
    public async Task UpdateTicket_AsParticipant_UpdatesOnlyProvidedFields()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        var ticket = new Ticket
        {
            Id = ticketId, Title = "Old", ProjectId = projectId, AuthorId = Guid.NewGuid(),
            Status = TicketStatus.TO_DO, Priority = Priority.LOW
        };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _ticketRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var req = new UpdateTicketRequest(null, null, null, TicketStatus.IN_PROGRESS, null);
        var result = await _service.UpdateTicket(ticketId, req, callerId);

        Assert.Equal("Old", result.Title);
        Assert.Equal(TicketStatus.IN_PROGRESS, result.Status);
    }

    [Fact]
    public async Task UpdateTicket_AsNonParticipant_ThrowsForbiddenException()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" }; // no participants
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);

        var req = new UpdateTicketRequest("New", null, null, null, null);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateTicket(ticketId, req, Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateTicket_WithNonexistentTicket_ThrowsTicketNotFoundException()
    {
        var id = Guid.NewGuid();
        _ticketRepoMock.Setup(r => r.GetTicketById(id)).ReturnsAsync((Ticket?)null);

        var req = new UpdateTicketRequest("New", null, null, null, null);

        await Assert.ThrowsAsync<TicketNotFoundException>(() => _service.UpdateTicket(id, req, Guid.NewGuid()));
    }

    // ---- RemoveTicket ----

    [Fact]
    public async Task RemoveTicket_AsParticipant_ReturnsTrue()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _ticketRepoMock.Setup(r => r.RemoveTicket(ticketId)).ReturnsAsync(true);

        var result = await _service.RemoveTicket(ticketId, callerId);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveTicket_WithNonexistentTicket_ThrowsTicketNotFoundException()
    {
        var id = Guid.NewGuid();
        _ticketRepoMock.Setup(r => r.GetTicketById(id)).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<TicketNotFoundException>(() => _service.RemoveTicket(id, Guid.NewGuid()));
    }

    // ---- AssignTicket / UnassignTicket ----

    [Fact]
    public async Task AssignTicket_ToProjectParticipant_SetsAssigneeId()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        var assignee = new User { Id = assigneeId, Username = "assignee", PasswordHash = "hash" };
        project.Participants.Add(assignee); // assignee must also be a participant

        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(assigneeId)).ReturnsAsync(assignee);
        _ticketRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.AssignTicket(ticketId, assigneeId, callerId);

        Assert.Equal(assigneeId, result.AssigneeId);
    }

    [Fact]
    public async Task AssignTicket_ToNonParticipantUser_ThrowsForbiddenException()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        // assignee exists globally but is NOT added to project.Participants
        var assignee = new User { Id = assigneeId, Username = "outsider", PasswordHash = "hash" };
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(assigneeId)).ReturnsAsync(assignee);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.AssignTicket(ticketId, assigneeId, callerId));
    }

    [Fact]
    public async Task AssignTicket_AsNonParticipantCaller_ThrowsForbiddenException()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" }; // caller not a participant
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.AssignTicket(ticketId, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignTicket_WithNonexistentUser_ThrowsUserNotFoundException()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _userRepoMock.Setup(r => r.GetUserByUserId(assigneeId)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _service.AssignTicket(ticketId, assigneeId, callerId));
    }

    [Fact]
    public async Task UnassignTicket_AsParticipant_ClearsAssigneeId()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        var ticket = new Ticket
        {
            Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid(),
            AssigneeId = Guid.NewGuid()
        };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _ticketRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.UnassignTicket(ticketId, callerId);

        Assert.Null(result.AssigneeId);
    }

    // ---- AddTag / RemoveTag ----

    [Fact]
    public async Task AddTag_AsParticipant_AddsNewTagToTicket()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };
        var tag = new Tag { Name = "bug", Color = "#FF0000" };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _tagRepoMock.Setup(r => r.GetOrCreateTag("bug", "#FF0000")).ReturnsAsync(tag);
        _ticketRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var req = new AddTagRequest("bug", "#FF0000");
        var result = await _service.AddTag(ticketId, req, callerId);

        Assert.Contains("bug", result.Tags);
    }

    [Fact]
    public async Task AddTag_AsNonParticipant_ThrowsForbiddenException()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Title = "P" };
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);

        var req = new AddTagRequest("bug", null);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.AddTag(ticketId, req, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveTag_AsParticipant_RemovesMatchingTagCaseInsensitive()
    {
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var (project, _) = MakeProjectWithParticipant(projectId, callerId);
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = Guid.NewGuid() };
        ticket.Tags.Add(new Tag { Name = "bug", Color = "#FF0000" });

        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(projectId)).ReturnsAsync(project);
        _ticketRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.RemoveTag(ticketId, "BUG", callerId);

        Assert.Empty(result.Tags);
    }
}