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

    [HttpPost]
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

    [HttpGet]
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

    [HttpGet("all")]
    public async Task<IActionResult> GetAllTickets()
    {
        return Ok(await _service.GetAllTickets());
    }

    [HttpGet("by-project")]
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

    [HttpPut]
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

    [HttpDelete]
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

    [HttpPut("assign")]
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

    [HttpDelete("assign")]
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

    [HttpPost("tags")]
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

    [HttpDelete("tags")]
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