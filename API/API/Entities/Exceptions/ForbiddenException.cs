namespace API.Entities.Exceptions;

public class ForbiddenException(string message) : Exception(message);