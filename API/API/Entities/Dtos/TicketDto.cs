using API.Tickets;

namespace API.Entities.Dtos;

public record TicketDto
{
    public Guid TicketId { get; init; }
    public string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? Deadline { get; init; }
    public TicketStatus Status { get; init; }
    public Priority Priority { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? AssigneeId { get; init; }
    public Guid AuthorId { get; init; }
    public List<string> Tags { get; init; }
    public int CommentCount { get; init; }

    // Tags/CommentCount read ticket.Tags/ticket.Comments directly - same Include()
    // gotcha as ProjectDto. The repository always includes both, but keep that in
    // mind if this constructor is ever called against a ticket fetched elsewhere.
    public TicketDto(Ticket ticket)
    {
        TicketId = ticket.Id;
        Title = ticket.Title;
        Description = ticket.Description;
        Deadline = ticket.Deadline;
        Status = ticket.Status;
        Priority = ticket.Priority;
        ProjectId = ticket.ProjectId;
        AssigneeId = ticket.AssigneeId;
        AuthorId = ticket.AuthorId;
        Tags = ticket.Tags.Select(t => t.Name).ToList();
        CommentCount = ticket.Comments.Count;
    }
}