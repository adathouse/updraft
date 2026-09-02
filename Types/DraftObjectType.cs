using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using Updraft.Repositories;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Updraft.Security;

namespace Updraft.Types;

[ObjectType<Draft>]
public static partial class DraftObjectType
{
    static partial void Configure(IObjectTypeDescriptor<Draft> descriptor)
    {
        // Identity and foreign keys are exposed only through the opaque node `id` and object relationships.
        descriptor.Field(x => x.DraftId).Name("id").ID();
        descriptor.Ignore(x => x.JobId);
        descriptor.Ignore(x => x.DrafterId);
        descriptor.Ignore(x => x.ChangeId);
    }

    [Authorize(Policy = AuthorizationPolicies.DrafterOrRequester)]
    [NodeResolver]
    public static Task<Draft?> GetDraftByIdAsync(Guid id, [CurrentUser] CurrentUser? user, IDraftRepository draftRepository, CancellationToken cancellationToken) =>
        draftRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.DraftId == id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Attachment> GetAttachments([Parent] Draft draft, [CurrentUser] CurrentUser? user, IAttachmentRepository attachmentRepository) =>
        attachmentRepository.QueryByDraftId(draft.DraftId).VisibleTo(user.OrThrow());

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetNotes([Parent] Draft draft, [CurrentUser] CurrentUser? user, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.DraftId == draft.DraftId).VisibleTo(user.OrThrow());

    public static Task<Job?> GetJobAsync([Parent] Draft draft, [CurrentUser] CurrentUser? user, IJobRepository jobRepository, CancellationToken cancellationToken) =>
        jobRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.JobId == draft.JobId, cancellationToken);

    public static Task<User?> GetDrafterAsync([Parent] Draft draft, IUserRepository userRepository, CancellationToken cancellationToken) =>
        userRepository.Query().FirstOrDefaultAsync(x => x.UserId == draft.DrafterId, cancellationToken);
}