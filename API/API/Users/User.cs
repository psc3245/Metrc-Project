using API.Comments;
using API.Projects;
using API.Tickets;

namespace API.Users;

public class User
{
    public Guid userId {get;set;}
    public string username  {get;set;}
    public string passwordHash  {get;set;}
    public List<Project> projects {get;set;}
    public List<Ticket> tickets {get;set;}
    public List<Comment> comments {get;set;}
    public DateTime createdAt {get;set;}

    public User()
    {
        userId = Guid.NewGuid();
    }
}