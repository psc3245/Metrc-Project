using System.ComponentModel.DataAnnotations;

namespace API.Entities.HelperClasses;

public record CreateProjectRequest(
    [Required(AllowEmptyStrings = false)] string Title,
    string? Description,
    DateTime? Deadline);

public record UpdateProjectRequest(
    [MinLength(1)] string? Title,
    string? Description,
    DateTime? Deadline);