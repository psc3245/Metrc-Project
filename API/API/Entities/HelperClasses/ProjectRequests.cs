namespace API.Entities.HelperClasses;

public record CreateProjectRequest(string Title, string? Description, DateTime? Deadline);

// Fields left null are left unchanged. There is currently no way to explicitly
// clear a Deadline back to null via update - scoped out deliberately given timeline;
// would need an Optional<T>-style wrapper or a separate clear-deadline endpoint to support it.
public record UpdateProjectRequest(string? Title, string? Description, DateTime? Deadline);