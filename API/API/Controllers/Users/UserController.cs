using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.Users;
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

    /// <summary>Get a user by their id.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Get a user by their username.</summary>
    [HttpGet("by-username")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Get all users.</summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsers()
    {
        return Ok(await _service.GetUsers());
    }

    /// <summary>Delete a user by their id.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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