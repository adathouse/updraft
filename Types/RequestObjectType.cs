using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using Updraft.Repositories;
using EntityTag = Updraft.Data.Entities.Tag;
using HotChocolate.Authorization;
using Updraft.Security;

namespace Updraft.Types;

[ObjectType<Request>]
public static partial class RequestObjectType
{
    static partial void Configure(IObjectTypeDescriptor<Request> descriptor)
    {
        descriptor.Ignore(x => x.RequestTags);
        descriptor.Ignore(x => x.RequestCommittees);
        // Identity and foreign keys are exposed only through the opaque node `id` and object relationships.
        descriptor.Field(x => x.RequestId).Name("id").ID();
        descriptor.Ignore(x => x.OfficeId);
        descriptor.Ignore(x => x.RequesterId);
        descriptor.Ignore(x => x.ChangeId);
    }

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [NodeResolver]
    public static Task<Request?> GetRequestByIdAsync(Guid id, [CurrentUser] CurrentUser? user, IRequestRepository requestRepository, CancellationToken cancellationToken) =>
        requestRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.RequestId == id, cancellationToken);

    public static Task<Job?> GetJobAsync([Parent] Request request, [CurrentUser] CurrentUser? user, IJobRepository jobRepository, CancellationToken cancellationToken) =>
        jobRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.RequestId == request.RequestId, cancellationToken);

    public static Task<Office?> GetOfficeAsync([Parent] Request request, IOfficeRepository officeRepository, CancellationToken cancellationToken) =>
        officeRepository.Query().FirstOrDefaultAsync(x => x.OfficeId == request.OfficeId, cancellationToken);

    public static Task<User?> GetRequesterAsync([Parent] Request request, IUserRepository userRepository, CancellationToken cancellationToken) =>
        userRepository.Query().FirstOrDefaultAsync(x => x.UserId == request.RequesterId, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Attachment> GetAttachments([Parent] Request request, [CurrentUser] CurrentUser? user, IAttachmentRepository attachmentRepository) =>
        attachmentRepository.QueryByRequestId(request.RequestId).VisibleTo(user.OrThrow());

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetNotes([Parent] Request request, [CurrentUser] CurrentUser? user, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.RequestId == request.RequestId).VisibleTo(user.OrThrow());

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<EntityTag> GetTags([Parent] Request request, ITagRepository tagRepository) =>
        tagRepository.QueryByRequestId(request.RequestId);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Office> GetProposedCommittees([Parent] Request request, IOfficeRepository officeRepository) =>
        officeRepository.QueryByRequestId(request.RequestId);
}