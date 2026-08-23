namespace API.Tickets;

public class Tag
{
    public Guid tagId {get;set;}
    public string name {get;set;}
    public string color { get; set; } // hex code?

    public Tag()
    {
        this.tagId = Guid.NewGuid();
    }
}