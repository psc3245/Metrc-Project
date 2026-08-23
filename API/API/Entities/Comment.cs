using API.Common;
using API.Tickets;
using API.Users;

namespace API.Comments;

public class Comment : BaseEntity
{
    public required string Text { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid CommenterId { get; set; }
    public User Commenter { get; set; } = null!;
}