using API.Comments;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public interface ICommentRepository
{
    Task AddComment(Comment comment);
    Task<Comment?> GetCommentById(Guid commentId);
    Task<List<Comment>> GetCommentsByTicketId(Guid ticketId);
    Task<bool> RemoveComment(Guid commentId);
}


public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _db;

    public CommentRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddComment(Comment comment)
    {
        await _db.Comments.AddAsync(comment);
        await _db.SaveChangesAsync();
    }

    public async Task<Comment?> GetCommentById(Guid commentId)
    {
        return await _db.Comments.FindAsync(commentId);
    }

    public async Task<List<Comment>> GetCommentsByTicketId(Guid ticketId)
    {
        return await _db.Comments
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> RemoveComment(Guid commentId)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment == null) return false;
        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
        return true;
    }
}