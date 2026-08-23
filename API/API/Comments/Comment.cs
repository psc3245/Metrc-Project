using API.Tickets;
using API.Users;

namespace API.Comments;

public class Comment
{
    public Guid commentId {get;set;}
    public User commenter {get;set;}
    public Ticket ticket {get;set;}
    public string text {get;set;}
    public DateTime createdAt {get;set;}

    public Comment()
    {
        this.commentId = Guid.NewGuid();
    }
}