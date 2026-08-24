using API.Projects;

namespace API.Entities.Dtos;

public record ProjectDto
{
    public Guid ProjectId { get; init; }
    public string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? Deadline { get; init; }
    public ProjectStatus Status { get; init; }
    public List<Guid> ParticipantIds { get; init; }
    public int TicketCount { get; init; }

    // NOTE: Status is computed from project.Tickets, and ParticipantIds/TicketCount
    // read project.Participants/project.Tickets directly. If the Project passed in
    // wasn't loaded with .Include(p => p.Participants).Include(p => p.Tickets),
    // these will silently read as empty rather than throwing - always Include both
    // in the repository query before constructing this DTO.
    public ProjectDto(Project project)
    {
        ProjectId = project.Id;
        Title = project.Title;
        Description = project.Description;
        Deadline = project.Deadline;
        Status = project.GetStatus();
        ParticipantIds = project.Participants.Select(p => p.Id).ToList();
        TicketCount = project.Tickets.Count;
    }
}