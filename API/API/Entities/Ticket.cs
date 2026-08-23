// API/Tickets/Ticket.cs
using API.Common;
using API.Comments;
using API.Projects;
using API.Users;

namespace API.Tickets;

public class Ticket : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.TO_DO;
    public Priority Priority { get; set; } = Priority.MEDIUM;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public List<Tag> Tags { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
}