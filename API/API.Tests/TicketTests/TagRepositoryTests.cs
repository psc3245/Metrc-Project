using API.Data;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.TicketTests;

public class TagRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetOrCreateTag_CreatesNewTag_WhenNoneExists()
    {
        await using var db = CreateContext();
        var repo = new TagRepository(db);

        var tag = await repo.GetOrCreateTag("bug", "#FF0000");

        Assert.Equal("bug", tag.Name);
        Assert.Single(db.Tags);
    }

    [Fact]
    public async Task GetOrCreateTag_ReturnsExistingTag_CaseInsensitive()
    {
        await using var db = CreateContext();
        var repo = new TagRepository(db);
        var original = await repo.GetOrCreateTag("bug", "#FF0000");

        var result = await repo.GetOrCreateTag("BUG", "#00FF00"); // different casing, different color

        Assert.Equal(original.Id, result.Id);
        Assert.Equal("#FF0000", result.Color); // original color preserved, not overwritten
        Assert.Single(db.Tags); // no duplicate created
    }
}