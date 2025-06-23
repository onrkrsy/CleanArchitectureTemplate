using System.Linq.Expressions;
using Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Common.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    #region Query Methods
    public virtual IQueryable<TEntity> GetAll()
    {
        return _dbSet.AsNoTracking();
    }

    public virtual IQueryable<TEntity> GetAllWithTracking()
    {
        return _dbSet.AsQueryable();
    }

    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression)
    {
        return _dbSet.Where(expression).AsNoTracking();
    }

    public virtual IQueryable<TEntity> WhereWithTracking(Expression<Func<TEntity, bool>> expression)
    {
        return _dbSet.Where(expression);
    }
    #endregion

    #region First Methods
    public virtual TEntity First(Expression<Func<TEntity, bool>> expression, bool isTrackingActive = true)
    {
        return isTrackingActive ? _dbSet.First(expression) : _dbSet.AsNoTracking().First(expression);
    }

    public virtual TEntity FirstOrDefault(Expression<Func<TEntity, bool>> expression, bool isTrackingActive = true)
    {
        return isTrackingActive ? _dbSet.FirstOrDefault(expression) : _dbSet.AsNoTracking().FirstOrDefault(expression);
    }

    public virtual async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default, bool isTrackingActive = true)
    {
        return isTrackingActive 
            ? await _dbSet.FirstOrDefaultAsync(expression, cancellationToken) 
            : await _dbSet.AsNoTracking().FirstOrDefaultAsync(expression, cancellationToken);
    }

    public virtual async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default, bool isTrackingActive = true)
    {
        return isTrackingActive 
            ? await _dbSet.FirstAsync(expression, cancellationToken) 
            : await _dbSet.AsNoTracking().FirstAsync(expression, cancellationToken);
    }
    #endregion

    #region Get By Expression Methods
    public virtual async Task<TEntity> GetByExpressionAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(expression, cancellationToken);
    }

    public virtual async Task<TEntity> GetByExpressionWithTrackingAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(expression, cancellationToken);
    }

    public virtual TEntity GetByExpression(Expression<Func<TEntity, bool>> expression)
    {
        return _dbSet.AsNoTracking().FirstOrDefault(expression);
    }

    public virtual TEntity GetByExpressionWithTracking(Expression<Func<TEntity, bool>> expression)
    {
        return _dbSet.FirstOrDefault(expression);
    }
    #endregion

    #region Get First Methods
    public virtual async Task<TEntity> GetFirstAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().FirstAsync(cancellationToken);
    }

    public virtual TEntity GetFirst()
    {
        return _dbSet.AsNoTracking().First();
    }
    #endregion

    #region Any Methods
    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(expression, cancellationToken);
    }

    public virtual bool Any(Expression<Func<TEntity, bool>> expression)
    {
        return _dbSet.Any(expression);
    }
    #endregion

    #region Add Methods
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Add(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    public virtual async Task AddRangeAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void AddRange(ICollection<TEntity> entities)
    {
        _dbSet.AddRange(entities);
    }
    #endregion

    #region Update Methods
    public virtual void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void UpdateRange(ICollection<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
    }
    #endregion

    #region Delete Methods
    public virtual async Task DeleteByIdAsync(string id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task DeleteByExpressionAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet.Where(expression).ToListAsync(cancellationToken);
        _dbSet.RemoveRange(entities);
    }

    public virtual void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void DeleteRange(ICollection<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }
    #endregion

    #region Count Methods
    public virtual IQueryable<KeyValuePair<bool, int>> CountBy(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return _dbSet.GroupBy(x => _dbSet.Any(expression))
                    .Select(g => new KeyValuePair<bool, int>(g.Key, g.Count()));
    }
    #endregion

    #region Legacy Methods for Backward Compatibility
    public virtual async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).AsNoTracking().ToListAsync();
    }

    public virtual async Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().SingleOrDefaultAsync(predicate);
    }

    public virtual void DeleteRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }
    #endregion
}