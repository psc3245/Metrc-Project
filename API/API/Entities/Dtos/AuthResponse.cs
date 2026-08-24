using API.Entities.Users;

namespace API.Entities.Dtos;

public record AuthResponse(UserDto User, string Token);