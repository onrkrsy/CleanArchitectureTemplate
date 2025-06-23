using Application.Interfaces;
using Infrastructure.Data;
using Common.UnitOfWork;

namespace Infrastructure.Repositories;

public class AppUnitOfWork : UnitOfWork, IAppUnitOfWork
{
    private readonly ApplicationDbContext _appContext;
    private IUserRepository? _users;

    public AppUnitOfWork(ApplicationDbContext context) : base(context)
    {
        _appContext = context;
    }

    public IUserRepository Users
    {
        get
        {
            return _users ??= new UserRepository(_appContext);
        }
    }
}