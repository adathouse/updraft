using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;
using EntityTag = Updraft.Data.Entities.Tag;

namespace Updraft.Repositories;

public interface ITagRepository
{
    IQueryable<EntityTag> Query();
    IQueryable<EntityTag> QueryByRequestId(Guid requestId);
    Task<EntityTag?> GetByIdAsync(string tagId, CancellationToken cancellationToken);
    Task<List<EntityTag>> GetByIdsAsync(IEnumerable<string> tagIds, CancellationToken cancellationToken);
}

public sealed class TagRepository(UpdraftDbContext dbContext) : ITagRepository
{
    public IQueryable<EntityTag> Query() => dbContext.Tags.AsNoTracking();

    public IQueryable<EntityTag> QueryByRequestId(Guid requestId) =>
        dbContext.RequestTags
            .AsNoTracking()
            .Where(x => x.RequestId == requestId)
            .Select(x => x.Tag);

    public Task<EntityTag?> GetByIdAsync(string tagId, CancellationToken cancellationToken) =>
        dbContext.Tags.FirstOrDefaultAsync(x => x.TagId == tagId, cancellationToken);

    public Task<List<EntityTag>> GetByIdsAsync(IEnumerable<string> tagIds, CancellationToken cancellationToken)
    {
        var ids = tagIds.Distinct().ToArray();
        return dbContext.Tags
            .Where(x => ids.Contains(x.TagId))
            .ToListAsync(cancellationToken);
    }
}