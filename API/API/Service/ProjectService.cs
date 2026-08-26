using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Projects;
using API.Repositories;

namespace API.Service;

public interface IProjectService
{
    Task<ProjectDto> CreateProject(CreateProjectRequest req, Guid creatorId);
    Task<ProjectDto> GetProjectById(Guid projectId);
    Task<List<ProjectDto>> GetAllProjects();
    Task<ProjectDto> UpdateProject(Guid projectId, UpdateProjectRequest req, Guid callerId);
    Task<bool> RemoveProject(Guid projectId, Guid callerId);
    Task<ProjectDto> AddParticipant(Guid projectId, Guid userId, Guid callerId);
    Task<ProjectDto> RemoveParticipant(Guid projectId, Guid userId, Guid callerId);
}

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public ProjectService(IProjectRepository projectRepository, IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<ProjectDto> CreateProject(CreateProjectRequest req, Guid creatorId)
    {
        var project = new Project
        {
            Title = req.Title,
            Description = req.Description,
            Deadline = req.Deadline
        };
        
        var creator = await _userRepository.GetUserByUserId(creatorId);
        if (creator != null) project.Participants.Add(creator);

        await _projectRepository.AddProject(project);
        return new ProjectDto(project);
    }

    public async Task<ProjectDto> GetProjectById(Guid projectId)
    {
        var project = await _projectRepository.GetProjectById(projectId);
        if (project == null) throw new ProjectNotFoundException(projectId);
        return new ProjectDto(project);
    }

    public async Task<List<ProjectDto>> GetAllProjects()
    {
        var projects = await _projectRepository.GetAllProjects();
        return projects.Select(p => new ProjectDto(p)).ToList();
    }

    public async Task<ProjectDto> UpdateProject(Guid projectId, UpdateProjectRequest req, Guid callerId)
    {
        var project = await _projectRepository.GetProjectById(projectId);
        if (project == null) throw new ProjectNotFoundException(projectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        if (req.Title != null) project.Title = req.Title;
        if (req.Description != null) project.Description = req.Description;
        if (req.Deadline.HasValue) project.Deadline = req.Deadline;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.SaveChangesAsync();
        return new ProjectDto(project);
    }

    public async Task<bool> RemoveProject(Guid projectId, Guid callerId)
    {
        var project = await _projectRepository.GetProjectById(projectId);
        if (project == null) throw new ProjectNotFoundException(projectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        await _projectRepository.RemoveProject(projectId);
        return true;
    }

    public async Task<ProjectDto> AddParticipant(Guid projectId, Guid userId, Guid callerId)
    {
        var project = await _projectRepository.GetProjectById(projectId);
        if (project == null) throw new ProjectNotFoundException(projectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        var user = await _userRepository.GetUserByUserId(userId);
        if (user == null) throw new UserNotFoundException(userId);

        if (project.Participants.All(p => p.Id != userId))
        {
            project.Participants.Add(user);
            await _projectRepository.SaveChangesAsync();
        }

        return new ProjectDto(project);
    }

    public async Task<ProjectDto> RemoveParticipant(Guid projectId, Guid userId, Guid callerId)
    {
        var project = await _projectRepository.GetProjectById(projectId);
        if (project == null) throw new ProjectNotFoundException(projectId);
        ProjectAuthorization.EnsureParticipant(project, callerId);

        var user = await _userRepository.GetUserByUserId(userId);
        if (user == null) throw new UserNotFoundException(userId);

        project.Participants.RemoveAll(p => p.Id == userId);
        await _projectRepository.SaveChangesAsync();

        return new ProjectDto(project);
    }
}