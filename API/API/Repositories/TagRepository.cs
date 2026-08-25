using API.Data;
using API.Tickets;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public interface ITagRepository
{
    // Case-insensitive lookup by name; creates a new Tag with the given color
    // if none exists yet.
    Task<Tag> GetOrCreateTag(string name, string color);
}

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _db;

    public TagRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Tag> GetOrCreateTag(string name, string color)
    {
        var existing = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
        if (existing != null) return existing;

        var tag = new Tag { Name = name, Color = color };
        _db.Tags.Add(tag);

        try
        {
            await _db.SaveChangesAsync();
            return tag;
        }
        catch (DbUpdateException)
        {
            // Another concurrent request created a tag with this name first and
            // won the race against the unique index. Detach our failed insert
            // and return the tag that actually made it in, rather than erroring out.
            _db.Entry(tag).State = EntityState.Detached;
            var winner = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
            if (winner != null) return winner;
            throw; // genuinely unexpected - surface the original failure
        }
    }
}