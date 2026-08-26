using API.Comments;
using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Repositories;

namespace API.Service;

public interface ICommentService
{
    Task<CommentDto> CreateComment(CreateCommentRequest req, Guid commenterId);
    Task<List<CommentDto>> GetCommentsByTicketId(Guid ticketId);
    Task<bool> RemoveComment(Guid commentId, Guid callerId);
}

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IProjectRepository _projectRepository;

    public CommentService(
        ICommentRepository commentRepository,
        ITicketRepository ticketRepository,
        IProjectRepository projectRepository)
    {
        _commentRepository = commentRepository;
        _ticketRepository = ticketRepository;
        _projectRepository = projectRepository;
    }

    public async Task<CommentDto> CreateComment(CreateCommentRequest req, Guid commenterId)
    {
        var ticket = await _ticketRepository.GetTicketById(req.TicketId);
        if (ticket == null) throw new TicketNotFoundException(req.TicketId);

        var project = await _projectRepository.GetProjectById(ticket.ProjectId);
        if (project == null) throw new ProjectNotFoundException(ticket.ProjectId);
        ProjectAuthorization.EnsureParticipant(project, commenterId);

        var comment = new Comment
        {
            Text = req.Text,
            TicketId = req.TicketId,
            CommenterId = commenterId
        };

        await _commentRepository.AddComment(comment);
        return new CommentDto(comment);
    }

    public async Task<List<CommentDto>> GetCommentsByTicketId(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetTicketById(ticketId);
        if (ticket == null) throw new TicketNotFoundException(ticketId);

        var comments = await _commentRepository.GetCommentsByTicketId(ticketId);
        return comments.Select(c => new CommentDto(c)).ToList();
    }

    public async Task<bool> RemoveComment(Guid commentId, Guid callerId)
    {
        var comment = await _commentRepository.GetCommentById(commentId);
        if (comment == null) throw new CommentNotFoundException(commentId);
        
        if (comment.CommenterId != callerId)
            throw new ForbiddenException("Only the comment's author can delete it.");

        await _commentRepository.RemoveComment(commentId);
        return true;
    }
}