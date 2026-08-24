namespace API.Entities.Exceptions;

public class BadLoginException : Exception
{
    public BadLoginException(string message) : base(message) { }
}