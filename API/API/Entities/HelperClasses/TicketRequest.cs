using System.ComponentModel.DataAnnotations;
using API.Tickets;

namespace API.Entities.HelperClasses;

public record CreateTicketRequest(
    [Required(AllowEmptyStrings = false)] string Title,
    string? Description,
    DateTime? Deadline,
    Priority Priority,
    Guid ProjectId);

public record UpdateTicketRequest(
    [MinLength(1)] string? Title,
    string? Description,
    DateTime? Deadline,
    TicketStatus? Status,
    Priority? Priority);

public record AddTagRequest(
    [Required(AllowEmptyStrings = false)] string Name,
    string? Color);