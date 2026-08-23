// API/Projects/Project.cs
using API.Common;
using API.Tickets;
using API.Users;

namespace API.Projects;

public class Project : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }

    public List<User> Participants { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];

    public ProjectStatus GetStatus()
    {
        if (Tickets.Count == 0) return ProjectStatus.NOT_STARTED;
        if (Tickets.All(t => t.Status == TicketStatus.COMPLETED)) return ProjectStatus.COMPLETED;
        return ProjectStatus.IN_PROGRESS;
    }
}