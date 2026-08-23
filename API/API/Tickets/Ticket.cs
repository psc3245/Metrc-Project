using API.Comments;
using API.Projects;
using API.Users;

namespace API.Tickets;

public class Ticket
{
    public Guid ticketId  {get;set;}
    public Project project {get;set;}
    public string title  {get;set;}
    public DateTime deadline {get;set;}
    public string description {get;set;}
    public TicketStatus status {get;set;}
    public Priority priority {get;set;}
    public List<Tag> tags {get;set;}
    public List<Comment> comments {get;set;}
    public User? assignee {get;set;}
    public User author {get;set;}
    public DateTime createDate {get;set;}
    public DateTime updateDate {get;set;}

    public Ticket()
    {
        this.ticketId = Guid.NewGuid();
    }
}