using API.Comments;
using API.Data;
using API.Projects;
using API.Repositories;
using API.Tickets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests.CommentTests;

public class CommentRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddComment_PersistsComment()
    {
        await using var db = CreateContext();
        var project = new Project { Title = "P" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var ticket = new Ticket { Title = "T", ProjectId = project.Id, AuthorId = Guid.NewGuid() };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var repo = new CommentRepository(db);
        var comment = new Comment { Text = "First comment", TicketId = ticket.Id, CommenterId = Guid.NewGuid() };

        await repo.AddComment(comment);

        var saved = await db.Comments.FirstOrDefaultAsync(c => c.Text == "First comment");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetCommentsByTicketId_ReturnsOnlyMatchingCommentsInOrder()
    {
        await using var db = CreateContext();
        var project = new Project { Title = "P" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var ticketA = new Ticket { Title = "TA", ProjectId = project.Id, AuthorId = Guid.NewGuid() };
        var ticketB = new Ticket { Title = "TB", ProjectId = project.Id, AuthorId = Guid.NewGuid() };
        db.Tickets.AddRange(ticketA, ticketB);
        await db.SaveChangesAsync();

        var commenterId = Guid.NewGuid();
        db.Comments.AddRange(
            new Comment { Text = "First", TicketId = ticketA.Id, CommenterId = commenterId, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new Comment { Text = "Second", TicketId = ticketA.Id, CommenterId = commenterId, CreatedAt = DateTime.UtcNow },
            new Comment { Text = "Unrelated", TicketId = ticketB.Id, CommenterId = commenterId });
        await db.SaveChangesAsync();

        var repo = new CommentRepository(db);
        var result = await repo.GetCommentsByTicketId(ticketA.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].Text);
        Assert.Equal("Second", result[1].Text);
    }

    [Fact]
    public async Task RemoveComment_DeletesExistingComment_ReturnsTrue()
    {
        await using var db = CreateContext();
        var project = new Project { Title = "P" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var ticket = new Ticket { Title = "T", ProjectId = project.Id, AuthorId = Guid.NewGuid() };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        var comment = new Comment { Text = "ToDelete", TicketId = ticket.Id, CommenterId = Guid.NewGuid() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var repo = new CommentRepository(db);
        var result = await repo.RemoveComment(comment.Id);

        Assert.True(result);
        Assert.Empty(db.Comments);
    }

    [Fact]
    public async Task RemoveComment_ReturnsFalse_WhenNotFound()
    {
        await using var db = CreateContext();
        var repo = new CommentRepository(db);

        var result = await repo.RemoveComment(Guid.NewGuid());

        Assert.False(result);
    }
}