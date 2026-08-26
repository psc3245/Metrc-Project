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
public class ProjectController : AuthenticatedControllerBase
{
    private readonly IProjectService _service;

    public ProjectController(IProjectService service)
    {
        _service = service;
    }

    /// <summary>Create a project. The caller is automatically added as a participant.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req)
    {
        var callerId = GetAuthenticatedUserId();
        var project = await _service.CreateProject(req, callerId);
        return CreatedAtAction(nameof(GetProjectById), new { projectId = project.ProjectId }, project);
    }

    /// <summary>Get a project by its id.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById([FromQuery] Guid projectId)
    {
        try
        {
            var project = await _service.GetProjectById(projectId);
            return Ok(project);
        }
        catch (ProjectNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Get all projects.</summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(List<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllProjects()
    {
        return Ok(await _service.GetAllProjects());
    }

    /// <summary>Update a project's title, description, and/or deadline. Caller must be a participant.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject([FromQuery] Guid projectId, [FromBody] UpdateProjectRequest req)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var project = await _service.UpdateProject(projectId, req, callerId);
            return Ok(project);
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

    /// <summary>Delete a project (cascades to its tickets and their comments). Caller must be a participant.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject([FromQuery] Guid projectId)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            await _service.RemoveProject(projectId, callerId);
            return NoContent();
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

    /// <summary>Add a participant to a project. Caller must already be a participant.</summary>
    [HttpPost("participants")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddParticipant([FromQuery] Guid projectId, [FromQuery] Guid userId)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var project = await _service.AddParticipant(projectId, userId, callerId);
            return Ok(project);
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

    /// <summary>Remove a participant from a project. Caller must already be a participant.</summary>
    [HttpDelete("participants")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveParticipant([FromQuery] Guid projectId, [FromQuery] Guid userId)
    {
        try
        {
            var callerId = GetAuthenticatedUserId();
            var project = await _service.RemoveParticipant(projectId, userId, callerId);
            return Ok(project);
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
}