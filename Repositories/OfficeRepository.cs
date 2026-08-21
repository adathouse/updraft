using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface IOfficeRepository
{
    IQueryable<Office> Query();
    Task<Office?> GetByIdAsync(Guid officeId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid officeId, CancellationToken cancellationToken);

    Task<List<Office>> GetByIdsAsync(IEnumerable<Guid> officeIds, CancellationToken cancellationToken);
}

public sealed class OfficeRepository(UpdraftDbContext dbContext) : IOfficeRepository
{
    public IQueryable<Office> Query() => dbContext.Offices.AsNoTracking();

    public Task<Office?> GetByIdAsync(Guid officeId, CancellationToken cancellationToken) =>
        dbContext.Offices.FirstOrDefaultAsync(x => x.OfficeId == officeId, cancellationToken);

    public Task<List<Office>> GetByIdsAsync(IEnumerable<Guid> officeIds, CancellationToken cancellationToken) =>
        dbContext.Offices.Where(x => officeIds.Contains(x.OfficeId) && x.OfficeType == OfficeType.Committee).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid officeId, CancellationToken cancellationToken) =>
        dbContext.Offices.AnyAsync(x => x.OfficeId == officeId, cancellationToken);
}
