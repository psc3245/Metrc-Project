namespace API.Entities.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid userId)
        : base($"User id: {userId} not found")
    {
    }
    
    public UserNotFoundException(string username)
        : base($"User: {username} not found")
    {
    }
}