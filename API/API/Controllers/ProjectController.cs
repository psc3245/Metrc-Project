using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Service;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectController(IProjectService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req)
    {
        var project = await _service.CreateProject(req);
        return CreatedAtAction(nameof(GetProjectById), new { projectId = project.ProjectId }, project);
    }

    [HttpGet]
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

    [HttpGet("all")]
    public async Task<IActionResult> GetAllProjects()
    {
        return Ok(await _service.GetAllProjects());
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProject([FromQuery] Guid projectId, [FromBody] UpdateProjectRequest req)
    {
        try
        {
            var project = await _service.UpdateProject(projectId, req);
            return Ok(project);
        }
        catch (ProjectNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteProject([FromQuery] Guid projectId)
    {
        try
        {
            await _service.RemoveProject(projectId);
            return NoContent();
        }
        catch (ProjectNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("participants")]
    public async Task<IActionResult> AddParticipant([FromQuery] Guid projectId, [FromQuery] Guid userId)
    {
        try
        {
            var project = await _service.AddParticipant(projectId, userId);
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
    }

    [HttpDelete("participants")]
    public async Task<IActionResult> RemoveParticipant([FromQuery] Guid projectId, [FromQuery] Guid userId)
    {
        try
        {
            var project = await _service.RemoveParticipant(projectId, userId);
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
    }
}