using API.Common;
using API.Projects;
using API.Tickets;
using API.Comments;

namespace API.Users;

public class User : BaseEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }

    public List<Project> Projects { get; set; } = [];
    public List<Ticket> AssignedTickets { get; set; } = [];
    public List<Ticket> AuthoredTickets { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
}