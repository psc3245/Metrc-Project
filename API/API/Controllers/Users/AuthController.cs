using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.Users;
using API.Service;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IUserService _service;
    private readonly ITokenService _tokenService;

    public AuthController(IUserService service, ITokenService tokenService)
    {
        _service = service;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginSignupRequest loginRequest)
    {
        try
        {
            var user = await _service.LoginUser(loginRequest);
            var token = _tokenService.GenerateToken(user!.userId, user.Username);
            return Ok(new AuthResponse(user, token));
        }
        catch (BadLoginException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] LoginSignupRequest signupRequest)
    {
        try
        {
            var user = await _service.RegisterUser(signupRequest);
            var token = _tokenService.GenerateToken(user!.userId, user.Username);
            return Ok(new AuthResponse(user, token));
        }
        catch (UserExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}