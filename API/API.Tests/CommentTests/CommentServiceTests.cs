using API.Comments;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Projects;
using API.Repositories;
using API.Service;
using API.Tickets;
using API.Users;
using Moq;
using Xunit;

namespace API.Tests.CommentTests;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepoMock;
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly CommentService _service;

    public CommentServiceTests()
    {
        _commentRepoMock = new Mock<ICommentRepository>();
        _ticketRepoMock = new Mock<ITicketRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _service = new CommentService(_commentRepoMock.Object, _ticketRepoMock.Object, _projectRepoMock.Object);
    }

    private static (Ticket ticket, Project project) MakeTicketWithParticipant(Guid commenterId)
    {
        var commenter = new User { Id = commenterId, Username = "commenter", PasswordHash = "hash" };
        var project = new Project { Title = "P" };
        project.Participants.Add(commenter);
        var ticket = new Ticket { Title = "T", ProjectId = project.Id, AuthorId = Guid.NewGuid() };
        return (ticket, project);
    }

    // ---- CreateComment ----

    [Fact]
    public async Task CreateComment_AsParticipant_CreatesCommentWithGivenCommenter()
    {
        var commenterId = Guid.NewGuid();
        var (ticket, project) = MakeTicketWithParticipant(commenterId);
        _ticketRepoMock.Setup(r => r.GetTicketById(ticket.Id)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(ticket.ProjectId)).ReturnsAsync(project);
        _commentRepoMock.Setup(r => r.AddComment(It.IsAny<Comment>())).Returns(Task.CompletedTask);

        var req = new CreateCommentRequest("Nice work!", ticket.Id);

        var result = await _service.CreateComment(req, commenterId);

        Assert.Equal("Nice work!", result.Text);
        Assert.Equal(commenterId, result.CommenterId);
    }

    [Fact]
    public async Task CreateComment_AsNonParticipant_ThrowsForbiddenException()
    {
        var ticket = new Ticket { Title = "T", ProjectId = Guid.NewGuid(), AuthorId = Guid.NewGuid() };
        var project = new Project { Id = ticket.ProjectId, Title = "P" }; // no participants
        _ticketRepoMock.Setup(r => r.GetTicketById(ticket.Id)).ReturnsAsync(ticket);
        _projectRepoMock.Setup(r => r.GetProjectById(ticket.ProjectId)).ReturnsAsync(project);

        var req = new CreateCommentRequest("Text", ticket.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.CreateComment(req, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateComment_WithNonexistentTicket_ThrowsTicketNotFoundException()
    {
        var ticketId = Guid.NewGuid();
        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync((Ticket?)null);

        var req = new CreateCommentRequest("Text", ticketId);

        await Assert.ThrowsAsync<TicketNotFoundException>(() => _service.CreateComment(req, Guid.NewGuid()));
    }

    // ---- GetCommentsByTicketId ----

    [Fact]
    public async Task GetCommentsByTicketId_ReturnsAllAsDtos()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = Guid.NewGuid(), AuthorId = Guid.NewGuid() };
        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync(ticket);
        var comments = new List<Comment>
        {
            new() { Text = "C1", TicketId = ticketId, CommenterId = Guid.NewGuid() },
            new() { Text = "C2", TicketId = ticketId, CommenterId = Guid.NewGuid() }
        };
        _commentRepoMock.Setup(r => r.GetCommentsByTicketId(ticketId)).ReturnsAsync(comments);

        var result = await _service.GetCommentsByTicketId(ticketId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetCommentsByTicketId_WithNonexistentTicket_ThrowsTicketNotFoundException()
    {
        var ticketId = Guid.NewGuid();
        _ticketRepoMock.Setup(r => r.GetTicketById(ticketId)).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<TicketNotFoundException>(() => _service.GetCommentsByTicketId(ticketId));
    }

    // ---- RemoveComment ----

    [Fact]
    public async Task RemoveComment_AsAuthor_ReturnsTrue()
    {
        var commentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, Text = "T", TicketId = Guid.NewGuid(), CommenterId = authorId };
        _commentRepoMock.Setup(r => r.GetCommentById(commentId)).ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.RemoveComment(commentId)).ReturnsAsync(true);

        var result = await _service.RemoveComment(commentId, authorId);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveComment_AsNonAuthor_ThrowsForbiddenException()
    {
        var commentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var someoneElseId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, Text = "T", TicketId = Guid.NewGuid(), CommenterId = authorId };
        _commentRepoMock.Setup(r => r.GetCommentById(commentId)).ReturnsAsync(comment);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.RemoveComment(commentId, someoneElseId));
    }

    [Fact]
    public async Task RemoveComment_WithNonexistentComment_ThrowsCommentNotFoundException()
    {
        var commentId = Guid.NewGuid();
        _commentRepoMock.Setup(r => r.GetCommentById(commentId)).ReturnsAsync((Comment?)null);

        await Assert.ThrowsAsync<CommentNotFoundException>(() => _service.RemoveComment(commentId, Guid.NewGuid()));
    }
}