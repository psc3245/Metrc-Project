using API.Data;
using API.Projects;
using API.Repositories;
using API.Tickets;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.ProjectTests;

public class ProjectRepositoryTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddProject_PersistsProject()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateContext(dbName);
        var repo = new ProjectRepository(db);
        var project = new Project { Title = "New Project" };

        await repo.AddProject(project);

        var saved = await db.Projects.FirstOrDefaultAsync(p => p.Title == "New Project");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetProjectById_IncludesParticipantsAndTickets()
    {
        // This test exists specifically to guard against a real gotcha: if the
        // repository's query forgets .Include(), Participants/Tickets silently
        // come back empty instead of throwing, and ProjectDto/GetStatus() would
        // quietly produce wrong results rather than fail loudly.
        var dbName = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using (var seedDb = CreateContext(dbName))
        {
            var user = new User { Id = userId, Username = "participant1", PasswordHash = "hash" };
            var project = new Project { Id = projectId, Title = "Tracked Project" };
            project.Participants.Add(user);
            project.Tickets.Add(new Ticket
            {
                Title = "T1",
                ProjectId = projectId,
                AuthorId = userId
            });
            seedDb.Users.Add(user);
            seedDb.Projects.Add(project);
            await seedDb.SaveChangesAsync();
        }

        // Fresh context/instance to prove this was actually persisted, not just tracked in memory
        await using var db = CreateContext(dbName);
        var repo = new ProjectRepository(db);

        var result = await repo.GetProjectById(projectId);

        Assert.NotNull(result);
        Assert.Single(result!.Participants);
        Assert.Single(result.Tickets);
    }

    [Fact]
    public async Task GetProjectById_ReturnsNull_WhenNotFound()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var repo = new ProjectRepository(db);

        var result = await repo.GetProjectById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllProjects_ReturnsAllPersistedProjects()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        db.Projects.AddRange(
            new Project { Title = "P1" },
            new Project { Title = "P2" });
        await db.SaveChangesAsync();
        var repo = new ProjectRepository(db);

        var result = await repo.GetAllProjects();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task RemoveProject_DeletesExistingProject_ReturnsTrue()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var project = new Project { Title = "ToDelete" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var repo = new ProjectRepository(db);

        var result = await repo.RemoveProject(project.Id);

        Assert.True(result);
        Assert.Empty(db.Projects);
    }

    [Fact]
    public async Task RemoveProject_ReturnsFalse_WhenNotFound()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var repo = new ProjectRepository(db);

        var result = await repo.RemoveProject(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsMutationsToTrackedEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid();

        await using (var seedDb = CreateContext(dbName))
        {
            seedDb.Projects.Add(new Project { Id = projectId, Title = "Original Title" });
            await seedDb.SaveChangesAsync();
        }

        await using var db = CreateContext(dbName);
        var repo = new ProjectRepository(db);
        var project = await repo.GetProjectById(projectId);
        project!.Title = "Updated Title";

        await repo.SaveChangesAsync();

        await using var verifyDb = CreateContext(dbName);
        var reloaded = await verifyDb.Projects.FindAsync(projectId);
        Assert.Equal("Updated Title", reloaded!.Title);
    }
}