using System.Linq.Expressions;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Common.Repositories;

namespace Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    // Specialized methods for User with Role include
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet.Include(u => u.Role).Where(u => u.IsActive).AsNoTracking().ToListAsync();
    }

    // Additional specialized methods using new repository features
    public async Task<User?> GetUserWithRoleByIdAsync(int id)
    {
        return await GetByExpressionWithTrackingAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserByEmailWithTrackingAsync(string email)
    {
        return await GetByExpressionWithTrackingAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
    {
        return await _dbSet.Include(u => u.Role)
                          .Where(u => u.Role.Name == roleName)
                          .AsNoTracking()
                          .ToListAsync();
    }

    public async Task<bool> IsEmailExistsAsync(string email)
    {
        return await AnyAsync(u => u.Email == email);
    }

    public async Task<int> GetActiveUserCountAsync()
    {
        return await _dbSet.CountAsync(u => u.IsActive);
    }

    public IQueryable<User> GetUsersWithRole()
    {
        return _dbSet.Include(u => u.Role).AsNoTracking();
    }

    public IQueryable<User> GetUsersWithRoleTracking()
    {
        return _dbSet.Include(u => u.Role);
    }

    // Override base methods to include Role navigation property
    public override async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public override async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet.Include(u => u.Role).AsNoTracking().ToListAsync();
    }

    public override async Task<User> GetByExpressionAsync(Expression<Func<User, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(expression, cancellationToken);
    }

    public override async Task<User> GetByExpressionWithTrackingAsync(Expression<Func<User, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(expression, cancellationToken);
    }

    public override IQueryable<User> GetAll()
    {
        return _dbSet.Include(u => u.Role).AsNoTracking();
    }

    public override IQueryable<User> GetAllWithTracking()
    {
        return _dbSet.Include(u => u.Role);
    }

    public override IQueryable<User> Where(Expression<Func<User, bool>> expression)
    {
        return _dbSet.Include(u => u.Role).Where(expression).AsNoTracking();
    }

    public override IQueryable<User> WhereWithTracking(Expression<Func<User, bool>> expression)
    {
        return _dbSet.Include(u => u.Role).Where(expression);
    }
}