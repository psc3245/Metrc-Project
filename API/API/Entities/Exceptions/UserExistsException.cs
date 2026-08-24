namespace API.Entities.Exceptions;

public class UserExistsException(string message) : Exception(message);