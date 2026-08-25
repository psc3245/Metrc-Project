using System.ComponentModel.DataAnnotations;

namespace API.Entities.HelperClasses;

public record CreateProjectRequest(
    [Required(AllowEmptyStrings = false)] string Title,
    string? Description,
    DateTime? Deadline);

// Fields left null are left unchanged - same "no explicit clear" limitation as
// before, deliberate scope cut given timeline.
// MinLength(1) on a nullable string only validates when a value IS provided
// (null passes through untouched) - so this blocks "" without blocking "don't update".
public record UpdateProjectRequest(
    [MinLength(1)] string? Title,
    string? Description,
    DateTime? Deadline);