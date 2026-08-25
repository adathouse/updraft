using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface IRequestRepository
{
    IQueryable<Request> Query();
    IQueryable<Request> QueryByTagId(string tagId);
    Task<Request?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid requestId, CancellationToken cancellationToken);
    Task AddAsync(Request request, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class RequestRepository(UpdraftDbContext dbContext) : IRequestRepository
{
    public IQueryable<Request> Query() => dbContext.Requests.AsNoTracking();

    public IQueryable<Request> QueryByTagId(string tagId) =>
        dbContext.RequestTags
            .AsNoTracking()
            .Where(x => x.TagId == tagId)
            .Select(x => x.Request);

    public Task<Request?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        dbContext.Requests.FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);

    public Task<bool> ExistsAsync(Guid requestId, CancellationToken cancellationToken) =>
        dbContext.Requests.AnyAsync(x => x.RequestId == requestId, cancellationToken);

    public Task AddAsync(Request request, CancellationToken cancellationToken) =>
        dbContext.Requests.AddAsync(request, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}