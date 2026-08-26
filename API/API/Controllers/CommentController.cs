using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentController : AuthenticatedControllerBase
{
    private readonly ICommentService _service;

    public CommentController(ICommentService service)
    {
        _service = service;
    }

    /// <summary>Add a comment to a ticket. Commenter is derived from the caller's JWT. Caller must be a project participant.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest req)
    {
        try
        {
            var commenterId = GetAuthenticatedUserId();
            var comment = await _service.CreateComment(req, commenterId);
            return CreatedAtAction(nameof(GetCommentsByTicketId), new { ticketId = comment.TicketId }, comment);
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ProjectNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>Get all comments for a ticket.</summary>
    [HttpGet("by-ticket")]
    [ProducesResponseType(typeof(List<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCommentsByTicketId([FromQuery] Guid ticketId)
    {
        try
        {
            var comments = await _service.GetCommentsByTicketId(ticketId);
            return Ok(comments);
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Delete a comment. Only the comment's original author may delete it.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment([FromQuery] Guid commentId)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            await _service.RemoveComment(commentId, callerId);
            return NoContent();
        }
        catch (CommentNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }
}