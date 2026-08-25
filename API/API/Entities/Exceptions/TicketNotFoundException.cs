namespace API.Entities.Exceptions;

public class TicketNotFoundException(Guid ticketId) : Exception($"Ticket with id '{ticketId}' not found.");