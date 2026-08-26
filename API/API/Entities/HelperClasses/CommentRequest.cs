using System.ComponentModel.DataAnnotations;

namespace API.Entities.HelperClasses;

public record CreateCommentRequest(
    [Required(AllowEmptyStrings = false)] string Text,
    Guid TicketId);