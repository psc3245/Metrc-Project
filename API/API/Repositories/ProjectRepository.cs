using API.Data;
using API.Projects;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;
 
public interface IProjectRepository
{
    Task AddProject(Project project);
    Task<Project?> GetProjectById(Guid projectId);
    Task<List<Project>> GetAllProjects();
    Task<bool> RemoveProject(Guid projectId);
    Task SaveChangesAsync();
}

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _db;

    public ProjectRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddProject(Project project)
    {
        await _db.Projects.AddAsync(project);
        await _db.SaveChangesAsync();
    }

    public async Task<Project?> GetProjectById(Guid projectId)
    {
        // Participants and Tickets are explicitly included: ProjectDto and
        // Project.GetStatus() both read these collections, and EF Core silently
        // returns an empty list rather than throwing if they're not loaded.
        return await _db.Projects
            .Include(p => p.Participants)
            .Include(p => p.Tickets)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<List<Project>> GetAllProjects()
    {
        return await _db.Projects
            .Include(p => p.Participants)
            .Include(p => p.Tickets)
            .ToListAsync();
    }

    public async Task<bool> RemoveProject(Guid projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null) return false;
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}