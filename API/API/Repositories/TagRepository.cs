using API.Data;
using API.Tickets;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public interface ITagRepository
{
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
            _db.Entry(tag).State = EntityState.Detached;
            var winner = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
            if (winner != null) return winner;
            throw;
        }
    }
}