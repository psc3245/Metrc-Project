namespace API.Entities.Exceptions;

public class CommentNotFoundException(Guid commentId) : Exception($"Comment with id '{commentId}' not found.");