using API.Data;
using API.Tickets;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public interface ITicketRepository
{
    Task AddTicket(Ticket ticket);
    Task<Ticket?> GetTicketById(Guid ticketId);
    Task<List<Ticket>> GetAllTickets();
    Task<List<Ticket>> GetTicketsByProjectId(Guid projectId);
    Task<bool> RemoveTicket(Guid ticketId);
    Task SaveChangesAsync();
}


public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _db;

    public TicketRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddTicket(Ticket ticket)
    {
        await _db.Tickets.AddAsync(ticket);
        await _db.SaveChangesAsync();
    }

    public async Task<Ticket?> GetTicketById(Guid ticketId)
    {
        return await _db.Tickets
            .Include(t => t.Tags)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
    }

    public async Task<List<Ticket>> GetAllTickets()
    {
        return await _db.Tickets
            .Include(t => t.Tags)
            .Include(t => t.Comments)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetTicketsByProjectId(Guid projectId)
    {
        return await _db.Tickets
            .Include(t => t.Tags)
            .Include(t => t.Comments)
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<bool> RemoveTicket(Guid ticketId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket == null) return false;
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}