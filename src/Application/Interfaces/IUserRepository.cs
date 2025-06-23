using Domain.Entities;
using Common.Interfaces;

namespace Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    // Original methods
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    
    // New specialized methods
    Task<User?> GetUserWithRoleByIdAsync(int id);
    Task<User?> GetUserByEmailWithTrackingAsync(string email);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);
    Task<bool> IsEmailExistsAsync(string email);
    Task<int> GetActiveUserCountAsync();
    IQueryable<User> GetUsersWithRole();
    IQueryable<User> GetUsersWithRoleTracking();
}