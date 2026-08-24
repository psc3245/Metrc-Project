using API.Entities.Exceptions;
using API.Entities.Users;
using API.Repositories;
using API.Users;
using Microsoft.AspNetCore.Identity.Data;

namespace API.Service;

public interface IUserService
{
    Task<UserDto?> RegisterUser(LoginSignupRequest req);
    Task<UserDto?> LoginUser(LoginSignupRequest req);
    Task<UserDto?> GetUserByUserId(Guid userId);
    Task<UserDto?> GetUserByUsername(string username);
    Task<List<UserDto>> GetUsers();
    Task<bool> RemoveUser(Guid userId);
}
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> RegisterUser(LoginSignupRequest req) 
    {
        var exists = await _userRepository.GetUserByUsername(req.username) != null;
        if (exists)
        {
            throw new UserExistsException("Username already exists");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(req.password);
        var user = new User { Username = req.username, PasswordHash = hash };
        await _userRepository.AddUser(user);
        return new UserDto(user);
    }

    public async Task<UserDto> LoginUser(LoginSignupRequest req)
    {
        var user = await _userRepository.GetUserByUsername(req.username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.password, user.PasswordHash)) throw new BadLoginException("Failed Login Attempt");
        return new UserDto(user);
    }

    public async Task<UserDto> GetUserByUserId(Guid userId)
    {
        var user = await _userRepository.GetUserByUserId(userId);
        if (user == null) throw new UserNotFoundException(userId);
        return new UserDto(user);
    }

    public async Task<UserDto?> GetUserByUsername(string username)
    {
        var user = await _userRepository.GetUserByUsername(username);
        if (user == null) throw new UserNotFoundException(username);
        return new UserDto(user);
    }

    public async Task<List<UserDto>> GetUsers()
    {
        var users = await _userRepository.GetAllUsers();
        return users.Select(user => new UserDto(user)).ToList();
    }

    public async Task<bool> RemoveUser(Guid userId)
    {
        var res = await _userRepository.RemoveUser(userId);
        
        if (!res) throw new UserNotFoundException(userId);
        
        return true;
    }
}