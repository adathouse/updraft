using Updraft.Repositories;
using EntityTag = Updraft.Data.Entities.Tag;
using Updraft.Data.Entities;

namespace Updraft.Types;

[ObjectType<EntityTag>]
public static partial class TagObjectType
{
    [NodeResolver]
    public static Task<EntityTag?> GetTagByIdAsync(string id, ITagRepository tagRepository, CancellationToken cancellationToken) =>
        tagRepository.GetByIdAsync(id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Request> GetRequests([Parent] EntityTag tag, IRequestRepository requestRepository) =>
        requestRepository.QueryByTagId(tag.TagId);
}
