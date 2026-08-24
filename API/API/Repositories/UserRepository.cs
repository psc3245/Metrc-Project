using API.Data;
using API.Entities.Exceptions;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public interface IUserRepository
{
    Task AddUser(User user);
    Task<User?> GetUserByUsername(string username);
    Task<List<User>> GetAllUsers();
    Task<User?> GetUserByUserId(Guid userId);
    Task<bool> RemoveUser(Guid userId);
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;
    
    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }
    
    public async Task AddUser(User user)
    {
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetUserByUsername(string username)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<List<User>> GetAllUsers()
    {
        return await _db.Users.ToListAsync();
    }

    public async Task<User?> GetUserByUserId(Guid userId)
    {
        return await _db.Users.FindAsync(userId);
    }

    public async Task<bool> RemoveUser(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }
    
}