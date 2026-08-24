namespace API.Entities.Exceptions;

public class ProjectNotFoundException(Guid projectId) : Exception($"Project with id '{projectId}' not found.");