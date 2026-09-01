using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using Updraft.Repositories;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Updraft.Security;

namespace Updraft.Types;

[ObjectType<Note>]
public static partial class NoteObjectType
{
    static partial void Configure(IObjectTypeDescriptor<Note> descriptor)
    {
        // Identity and foreign keys are exposed only through the opaque node `id` and object relationships.
        descriptor.Field(x => x.NoteId).Name("id").ID();
        descriptor.Ignore(x => x.OwnerId);
        descriptor.Ignore(x => x.RequestId);
        descriptor.Ignore(x => x.JobId);
        descriptor.Ignore(x => x.DraftId);
        descriptor.Ignore(x => x.ParentNoteId);
        descriptor.Ignore(x => x.ChangeId);
    }

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [NodeResolver]
    public static Task<Note?> GetNoteByIdAsync(Guid id, [CurrentUser] CurrentUser? user, INoteRepository noteRepository, CancellationToken cancellationToken) =>
        noteRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.NoteId == id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetReplies([Parent] Note note, [CurrentUser] CurrentUser? user, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.ParentNoteId == note.NoteId).VisibleTo(user.OrThrow());

    public static Task<User?> GetOwnerAsync([Parent] Note note, IUserRepository userRepository, CancellationToken cancellationToken) =>
        note.OwnerId is null
            ? Task.FromResult<User?>(null)
            : userRepository.Query().FirstOrDefaultAsync(x => x.UserId == note.OwnerId, cancellationToken);

    public static Task<Request?> GetRequestAsync([Parent] Note note, [CurrentUser] CurrentUser? user, IRequestRepository requestRepository, CancellationToken cancellationToken) =>
        note.RequestId.HasValue
            ? requestRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.RequestId == note.RequestId.Value, cancellationToken)
            : Task.FromResult<Request?>(null);

    public static Task<Job?> GetJobAsync([Parent] Note note, [CurrentUser] CurrentUser? user, IJobRepository jobRepository, CancellationToken cancellationToken) =>
        note.JobId.HasValue
            ? jobRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.JobId == note.JobId.Value, cancellationToken)
            : Task.FromResult<Job?>(null);

    public static Task<Draft?> GetDraftAsync([Parent] Note note, [CurrentUser] CurrentUser? user, IDraftRepository draftRepository, CancellationToken cancellationToken) =>
        note.DraftId.HasValue
            ? draftRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.DraftId == note.DraftId.Value, cancellationToken)
            : Task.FromResult<Draft?>(null);

    public static Task<Note?> GetParentNoteAsync([Parent] Note note, [CurrentUser] CurrentUser? user, INoteRepository noteRepository, CancellationToken cancellationToken) =>
        note.ParentNoteId.HasValue
            ? noteRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.NoteId == note.ParentNoteId.Value, cancellationToken)
            : Task.FromResult<Note?>(null);
}