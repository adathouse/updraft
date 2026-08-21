using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface IUserRepository
{
    IQueryable<User> Query();
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class UserRepository(UpdraftDbContext dbContext) : IUserRepository
{
    public IQueryable<User> Query() => dbContext.Users.AsNoTracking();

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(x => x.UserId == userId, cancellationToken);
}