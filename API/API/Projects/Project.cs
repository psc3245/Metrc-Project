using API.Tickets;
using API.Users;

namespace API.Projects;

public class Project
{
    public Guid projectId  {get;set;}
    public string title  {get;set;}
    public DateTime deadline  {get;set;}
    public string description  {get;set;}
    public List<User> participants {get;set;}
    public List<Ticket> tickets {get;set;}
    public DateTime createdAt {get;set;}
    public DateTime updatedAt {get;set;}

    public Project()
    {
        this.projectId = Guid.NewGuid();
    }

    public ProjectStatus getStatus()
    {
        var numStarted = tickets.Count(t => t.status == TicketStatus.IN_PROGRESS);
        var numCompleted = tickets.Count(t => t.status == TicketStatus.COMPLETED);
        var numReview = tickets.Count(t => t.status == TicketStatus.IN_REVIEW);

        if (numCompleted == tickets.Count)
        {
            return ProjectStatus.COMPLETED;
        }

        if (numStarted == 0 && numCompleted == 0 && numReview == 0)
        {
            return ProjectStatus.NOT_STARTED;
        }
        return ProjectStatus.IN_PROGRESS;
    }
}