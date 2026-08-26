using API.Comments;

namespace API.Entities.Dtos;

public record CommentDto
{
    public Guid CommentId { get; init; }
    public string Text { get; init; }
    public Guid TicketId { get; init; }
    public Guid CommenterId { get; init; }
    public DateTime CreatedAt { get; init; }

    public CommentDto(Comment comment)
    {
        CommentId = comment.Id;
        Text = comment.Text;
        TicketId = comment.TicketId;
        CommenterId = comment.CommenterId;
        CreatedAt = comment.CreatedAt;
    }
}