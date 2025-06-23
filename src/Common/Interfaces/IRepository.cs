using System.Linq.Expressions;

namespace Common.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    // Query methods
    IQueryable<TEntity> GetAll();
    IQueryable<TEntity> GetAllWithTracking();
    IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression);
    IQueryable<TEntity> WhereWithTracking(Expression<Func<TEntity, bool>> expression);
    
    // First methods
    TEntity First(Expression<Func<TEntity, bool>> expression, bool isTrackingActive = true);
    TEntity FirstOrDefault(Expression<Func<TEntity, bool>> expression, bool isTrackingActive = true);
    Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default, bool isTrackingActive = true);
    Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default, bool isTrackingActive = true);
    
    // Get by expression methods
    Task<TEntity> GetByExpressionAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    Task<TEntity> GetByExpressionWithTrackingAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    TEntity GetByExpression(Expression<Func<TEntity, bool>> expression);
    TEntity GetByExpressionWithTracking(Expression<Func<TEntity, bool>> expression);
    
    // Get first methods
    Task<TEntity> GetFirstAsync(CancellationToken cancellationToken = default);
    TEntity GetFirst();
    
    // Any methods
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    bool Any(Expression<Func<TEntity, bool>> expression);
    
    // Add methods
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    Task AddRangeAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default);
    void AddRange(ICollection<TEntity> entities);
    
    // Update methods
    void Update(TEntity entity);
    void UpdateRange(ICollection<TEntity> entities);
    
    // Delete methods
    Task DeleteByIdAsync(string id);
    Task DeleteByExpressionAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    void Delete(TEntity entity);
    void DeleteRange(ICollection<TEntity> entities);
    
    // Count methods
    IQueryable<KeyValuePair<bool, int>> CountBy(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    
    // Legacy methods for backward compatibility
    Task<TEntity?> GetByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    void DeleteRange(IEnumerable<TEntity> entities);
}