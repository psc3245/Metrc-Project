using API.Projects;
using API.Tickets;
using API.Users;

namespace API.Entities.Users;

public class UserDto
{
    public Guid userId { get; set; }
    public string Username { get; set; }
    public List<Project> Projects { get; set; } = new List<Project>();
    public List<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

    public UserDto(User user)
    {
        this.userId = user.Id;
        Username = user.Username;
        Projects = user.Projects;
        AssignedTickets = user.AssignedTickets;
    }
}