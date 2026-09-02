using HotChocolate.Types;
using Updraft.Repositories;
using EntityTag = Updraft.Data.Entities.Tag;
using Updraft.Data.Entities;
using HotChocolate.Authorization;
using Updraft.Security;

namespace Updraft.Types;

[ObjectType<EntityTag>]
public static partial class TagObjectType
{
    static partial void Configure(IObjectTypeDescriptor<EntityTag> descriptor)
    {
        descriptor.Ignore(x => x.RequestTags);
        // Identity and change token are not part of the public surface.
        descriptor.Field(x => x.TagId).Name("id").ID();
        descriptor.Ignore(x => x.ChangeId);
    }

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [NodeResolver]
    public static Task<EntityTag?> GetTagByIdAsync(string id, ITagRepository tagRepository, CancellationToken cancellationToken) =>
        tagRepository.GetByIdAsync(id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Request> GetRequests([Parent] EntityTag tag, [CurrentUser] CurrentUser? user, IRequestRepository requestRepository) =>
        requestRepository.QueryByTagId(tag.TagId).VisibleTo(user.OrThrow());
}
