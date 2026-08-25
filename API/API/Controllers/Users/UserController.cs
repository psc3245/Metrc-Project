using API.Entities.Exceptions;
using API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _service;

    public UserController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserById([FromQuery] Guid userId)
    {
        try
        {
            var user = await _service.GetUserByUserId(userId);
            return Ok(user);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("by-username")]
    public async Task<IActionResult> GetUserByUsername([FromQuery] string username)
    {
        try
        {
            var user = await _service.GetUserByUsername(username);
            return Ok(user);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        return Ok(await _service.GetUsers());
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteUser([FromQuery] Guid userId)
    {
        try
        {
            await _service.RemoveUser(userId);
            return NoContent();
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}