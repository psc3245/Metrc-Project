using API.Entities.Exceptions;
using API.Entities.Dtos;
using API.Entities.HelperClasses;
using API.Projects;
using API.Repositories;
using API.Tickets;

namespace API.Service;

public interface ITicketService
{
    Task<TicketDto> CreateTicket(CreateTicketRequest req, Guid authorId);
    Task<TicketDto> GetTicketById(Guid ticketId);
    Task<List<TicketDto>> GetAllTickets();
    Task<List<TicketDto>> GetTicketsByProjectId(Guid projectId);
    Task<TicketDto> UpdateTicket(Guid ticketId, UpdateTicketRequest req, Guid callerId);
    Task<bool> RemoveTicket(Guid ticketId, Guid callerId);
    Task<TicketDto> AssignTicket(Guid ticketId, Guid assigneeId, Guid callerId);
    Task<TicketDto> UnassignTicket(Guid ticketId, Guid callerId);
    Task<TicketDto> AddTag(Guid ticketId, AddTagRequest req, Guid callerId);
    Task<TicketDto> RemoveTag(Guid ticketId, string tagName, Guid callerId);
}


public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITagRepository _tagRepository;

    private const string DefaultTagColor = "#CCCCCC";

    public TicketService(
        ITicketRepository ticketRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ITagRepository tagRepository)
    {
        _ticketRepository = ticketRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _tagRepository = tagRepository;
    }

    public async Task<TicketDto> CreateTicket(CreateTicketRequest req, Guid authorId)
    {
        var project = await _projectRepository.GetProjectById(req.ProjectId);
        if (project == null) throw new ProjectNotFoundException(req.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, authorId);

        var ticket = new Ticket
        {
            Title = req.Title,
            Description = req.Description,
            Deadline = req.Deadline,
            Priority = req.Priority,
            ProjectId = req.ProjectId,
            AuthorId = authorId
        };

        await _ticketRepository.AddTicket(ticket);
        return new TicketDto(ticket);
    }

    public async Task<TicketDto> GetTicketById(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);
        return new TicketDto(ticket);
    }

    public async Task<List<TicketDto>> GetAllTickets()
    {
        var tickets = await _ticketRepository.GetAllTickets();
        return tickets.Select(t => new TicketDto(t)).ToList();
    }

    public async Task<List<TicketDto>> GetTicketsByProjectId(Guid projectId)
    {
        var project = await _projectRepository.GetProjectById(projectId);
        if (project == null) throw new ProjectNotFoundException(projectId);

        var tickets = await _ticketRepository.GetTicketsByProjectId(projectId);
        return tickets.Select(t => new TicketDto(t)).ToList();
    }
    
    private async Task<Project> GetOwningProjectOrThrow(Guid projectId)
    {
        var project = await _projectRepository.GetProjectById(projectId);
        if (project == null) throw new ProjectNotFoundException(projectId);
        return project;
    }

    public async Task<TicketDto> UpdateTicket(Guid ticketId, UpdateTicketRequest req, Guid callerId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);

        var project = await GetOwningProjectOrThrow(ticket.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        if (req.Title != null) ticket.Title = req.Title;
        if (req.Description != null) ticket.Description = req.Description;
        if (req.Deadline.HasValue) ticket.Deadline = req.Deadline;
        if (req.Status.HasValue) ticket.Status = req.Status.Value;
        if (req.Priority.HasValue) ticket.Priority = req.Priority.Value;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepository.SaveChangesAsync();
        return new TicketDto(ticket);
    }

    public async Task<bool> RemoveTicket(Guid ticketId, Guid callerId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);

        var project = await GetOwningProjectOrThrow(ticket.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        await _ticketRepository.RemoveTicket(ticketId);
        return true;
    }

    public async Task<TicketDto> AssignTicket(Guid ticketId, Guid assigneeId, Guid callerId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);

        var project = await GetOwningProjectOrThrow(ticket.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        var assignee = await _userRepository.GetUserByUserId(assigneeId);
        if (assignee == null) throw new UserNotFoundException(assigneeId);
        
        if (project.Participants.All(p => p.Id != assigneeId))
            throw new ForbiddenException("Assignee must be a participant of the ticket's project.");

        ticket.AssigneeId = assigneeId;
        await _ticketRepository.SaveChangesAsync();
        return new TicketDto(ticket);
    }

    public async Task<TicketDto> UnassignTicket(Guid ticketId, Guid callerId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);

        var project = await GetOwningProjectOrThrow(ticket.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        ticket.AssigneeId = null;
        await _ticketRepository.SaveChangesAsync();
        return new TicketDto(ticket);
    }

    public async Task<TicketDto> AddTag(Guid ticketId, AddTagRequest req, Guid callerId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);

        var project = await GetOwningProjectOrThrow(ticket.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        var tag = await _tagRepository.GetOrCreateTag(req.Name, req.Color ?? DefaultTagColor);

        if (ticket.Tags.All(t => t.Id != tag.Id))
        {
            ticket.Tags.Add(tag);
            await _ticketRepository.SaveChangesAsync();
        }

        return new TicketDto(ticket);
    }

    public async Task<TicketDto> RemoveTag(Guid ticketId, string tagName, Guid callerId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);

        var project = await GetOwningProjectOrThrow(ticket.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        ticket.Tags.RemoveAll(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
        await _ticketRepository.SaveChangesAsync();

        return new TicketDto(ticket);
    }
}