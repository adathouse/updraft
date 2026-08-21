using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface IDraftRepository
{
    IQueryable<Draft> Query();
    Task<Draft?> GetByIdAsync(Guid draftId, CancellationToken cancellationToken);
    Task AddAsync(Draft draft, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class DraftRepository(UpdraftDbContext dbContext) : IDraftRepository
{
    public IQueryable<Draft> Query() => dbContext.Drafts.AsNoTracking();

    public Task<Draft?> GetByIdAsync(Guid draftId, CancellationToken cancellationToken) =>
        dbContext.Drafts.FirstOrDefaultAsync(x => x.DraftId == draftId, cancellationToken);

    public Task AddAsync(Draft draft, CancellationToken cancellationToken) =>
        dbContext.Drafts.AddAsync(draft, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}