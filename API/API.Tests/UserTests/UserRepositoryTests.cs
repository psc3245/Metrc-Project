using API.Data;
using API.Repositories;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.Repositories;

public class UserRepositoryTests
{
    // Fresh, isolated in-memory database per test so state never leaks between tests
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddUser_PersistsUser()
    {
        await using var db = CreateContext();
        var repo = new UserRepository(db);
        var user = new User { Username = "alice", PasswordHash = "hash" };

        await repo.AddUser(user);

        var saved = await db.Users.FirstOrDefaultAsync(u => u.Username == "alice");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetUserByUsername_ReturnsMatchingUser()
    {
        await using var db = CreateContext();
        db.Users.Add(new User { Username = "bob", PasswordHash = "hash" });
        await db.SaveChangesAsync();
        var repo = new UserRepository(db);

        var result = await repo.GetUserByUsername("bob");

        Assert.NotNull(result);
        Assert.Equal("bob", result!.Username);
    }

    [Fact]
    public async Task GetUserByUsername_ReturnsNull_WhenNotFound()
    {
        await using var db = CreateContext();
        var repo = new UserRepository(db);

        var result = await repo.GetUserByUsername("ghost");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllPersistedUsers()
    {
        await using var db = CreateContext();
        db.Users.AddRange(
            new User { Username = "u1", PasswordHash = "hash" },
            new User { Username = "u2", PasswordHash = "hash" });
        await db.SaveChangesAsync();
        var repo = new UserRepository(db);

        var result = await repo.GetAllUsers();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserByUserId_ReturnsMatchingUser()
    {
        await using var db = CreateContext();
        var user = new User { Username = "carol", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var repo = new UserRepository(db);

        var result = await repo.GetUserByUserId(user.Id);

        Assert.NotNull(result);
        Assert.Equal("carol", result!.Username);
    }

    [Fact]
    public async Task GetUserByUserId_ReturnsNull_WhenNotFound()
    {
        await using var db = CreateContext();
        var repo = new UserRepository(db);

        var result = await repo.GetUserByUserId(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveUser_DeletesExistingUser_ReturnsTrue()
    {
        await using var db = CreateContext();
        var user = new User { Username = "dave", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var repo = new UserRepository(db);

        var result = await repo.RemoveUser(user.Id);

        Assert.True(result);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task RemoveUser_ReturnsFalse_WhenNotFound()
    {
        await using var db = CreateContext();
        var repo = new UserRepository(db);

        var result = await repo.RemoveUser(Guid.NewGuid());

        Assert.False(result);
    }
}