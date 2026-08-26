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
public class TicketController : AuthenticatedControllerBase
{
    private readonly ITicketService _service;

    public TicketController(ITicketService service)
    {
        _service = service;
    }

    /// <summary>Create a ticket in a project. Author is derived from the caller's JWT. Caller must be a project participant.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        try
        {
            var authorId = GetAuthenticatedUserId();
            var ticket = await _service.CreateTicket(req, authorId);
            return CreatedAtAction(nameof(GetTicketById), new { ticketId = ticket.TicketId }, ticket);
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

    /// <summary>Get a ticket by its id.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketById([FromQuery] Guid ticketId)
    {
        try
        {
            var ticket = await _service.GetTicketById(ticketId);
            return Ok(ticket);
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Get all tickets.</summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(List<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllTickets()
    {
        return Ok(await _service.GetAllTickets());
    }

    /// <summary>Get all tickets belonging to a project.</summary>
    [HttpGet("by-project")]
    [ProducesResponseType(typeof(List<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketsByProjectId([FromQuery] Guid projectId)
    {
        try
        {
            var tickets = await _service.GetTicketsByProjectId(projectId);
            return Ok(tickets);
        }
        catch (ProjectNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Update a ticket's title, description, deadline, status, and/or priority. Caller must be a project participant.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTicket([FromQuery] Guid ticketId, [FromBody] UpdateTicketRequest req)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var ticket = await _service.UpdateTicket(ticketId, req, callerId);
            return Ok(ticket);
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

    /// <summary>Delete a ticket (cascades to its comments). Caller must be a project participant.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTicket([FromQuery] Guid ticketId)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            await _service.RemoveTicket(ticketId, callerId);
            return NoContent();
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

    /// <summary>Assign a ticket to a user. Both caller and assignee must be project participants.</summary>
    [HttpPut("assign")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignTicket([FromQuery] Guid ticketId, [FromQuery] Guid assigneeId)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var ticket = await _service.AssignTicket(ticketId, assigneeId, callerId);
            return Ok(ticket);
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ProjectNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>Unassign a ticket. Caller must be a project participant.</summary>
    [HttpDelete("assign")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignTicket([FromQuery] Guid ticketId)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var ticket = await _service.UnassignTicket(ticketId, callerId);
            return Ok(ticket);
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

    /// <summary>Add a tag to a ticket, creating the tag if it doesn't already exist. Caller must be a project participant.</summary>
    [HttpPost("tags")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTag([FromQuery] Guid ticketId, [FromBody] AddTagRequest req)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var ticket = await _service.AddTag(ticketId, req, callerId);
            return Ok(ticket);
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

    /// <summary>Remove a tag from a ticket by name. Caller must be a project participant.</summary>
    [HttpDelete("tags")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag([FromQuery] Guid ticketId, [FromQuery] string tagName)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var ticket = await _service.RemoveTag(ticketId, tagName, callerId);
            return Ok(ticket);
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
}