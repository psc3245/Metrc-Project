using API.Data;
using API.Projects;
using API.Repositories;
using API.Tickets;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.TicketTests;

public class TicketRepositoryTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddTicket_PersistsTicket()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateContext(dbName);
        var project = new Project { Title = "P" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var repo = new TicketRepository(db);
        var ticket = new Ticket { Title = "New Ticket", ProjectId = project.Id, AuthorId = Guid.NewGuid() };

        await repo.AddTicket(ticket);

        var saved = await db.Tickets.FirstOrDefaultAsync(t => t.Title == "New Ticket");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetTicketById_IncludesTagsAndComments()
    {
        var dbName = Guid.NewGuid().ToString();
        var ticketId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        await using (var seedDb = CreateContext(dbName))
        {
            seedDb.Projects.Add(new Project { Id = projectId, Title = "P" });
            var ticket = new Ticket { Id = ticketId, Title = "T", ProjectId = projectId, AuthorId = authorId };
            ticket.Tags.Add(new Tag { Name = "bug", Color = "#FF0000" });
            seedDb.Tickets.Add(ticket);
            await seedDb.SaveChangesAsync();

            seedDb.Comments.Add(new Comments.Comment { Text = "first comment", TicketId = ticketId, CommenterId = authorId });
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateContext(dbName);
        var repo = new TicketRepository(db);

        var result = await repo.GetTicketById(ticketId);

        Assert.NotNull(result);
        Assert.Single(result!.Tags);
        Assert.Single(result.Comments);
    }

    [Fact]
    public async Task GetTicketById_ReturnsNull_WhenNotFound()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var repo = new TicketRepository(db);

        var result = await repo.GetTicketById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTicketsByProjectId_ReturnsOnlyMatchingTickets()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var projectA = new Project { Title = "A" };
        var projectB = new Project { Title = "B" };
        db.Projects.AddRange(projectA, projectB);
        await db.SaveChangesAsync();

        db.Tickets.AddRange(
            new Ticket { Title = "T1", ProjectId = projectA.Id, AuthorId = Guid.NewGuid() },
            new Ticket { Title = "T2", ProjectId = projectA.Id, AuthorId = Guid.NewGuid() },
            new Ticket { Title = "T3", ProjectId = projectB.Id, AuthorId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var repo = new TicketRepository(db);
        var result = await repo.GetTicketsByProjectId(projectA.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(projectA.Id, t.ProjectId));
    }

    [Fact]
    public async Task RemoveTicket_DeletesExistingTicket_ReturnsTrue()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var project = new Project { Title = "P" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var ticket = new Ticket { Title = "ToDelete", ProjectId = project.Id, AuthorId = Guid.NewGuid() };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var repo = new TicketRepository(db);
        var result = await repo.RemoveTicket(ticket.Id);

        Assert.True(result);
        Assert.Empty(db.Tickets);
    }

    [Fact]
    public async Task RemoveTicket_ReturnsFalse_WhenNotFound()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var repo = new TicketRepository(db);

        var result = await repo.RemoveTicket(Guid.NewGuid());

        Assert.False(result);
    }
}