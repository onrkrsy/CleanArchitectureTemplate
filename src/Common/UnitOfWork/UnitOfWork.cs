using Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Common.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    protected readonly DbContext _context;

    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}