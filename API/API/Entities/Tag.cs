using API.Common;

namespace API.Tickets;

public class Tag : BaseEntity
{
    public required string Name { get; set; }
    public required string Color { get; set; } // hex, validated in service layer

    public List<Ticket> Tickets { get; set; } = [];
}