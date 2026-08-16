using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface ICommitteeRepository
{
    IQueryable<Committee> Query();
    IQueryable<Committee> QueryByRequestId(Guid requestId);
    Task<List<Committee>> GetByIdsAsync(IEnumerable<Guid> committeeIds, CancellationToken cancellationToken);
}

public sealed class CommitteeRepository(UpdraftDbContext dbContext) : ICommitteeRepository
{
    public IQueryable<Committee> Query() => dbContext.Committees.AsNoTracking();

    public IQueryable<Committee> QueryByRequestId(Guid requestId) =>
        dbContext.RequestCommittees
            .AsNoTracking()
            .Where(x => x.RequestId == requestId)
            .Select(x => x.Committee);

    public Task<List<Committee>> GetByIdsAsync(IEnumerable<Guid> committeeIds, CancellationToken cancellationToken)
    {
        var ids = committeeIds.Distinct().ToArray();
        return dbContext.Committees
            .Where(x => ids.Contains(x.CommitteeId))
            .ToListAsync(cancellationToken);
    }
}