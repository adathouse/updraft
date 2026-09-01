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
        descriptor.Ignore(x => x.NoteId);
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
    public static IQueryable<Note> GetReplies([Parent] Note note, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.ParentNoteId == note.NoteId);

    public static Task<User?> GetOwnerAsync([Parent] Note note, IUserRepository userRepository, CancellationToken cancellationToken) =>
        note.OwnerId is null
            ? Task.FromResult<User?>(null)
            : userRepository.Query().FirstOrDefaultAsync(x => x.UserId == note.OwnerId, cancellationToken);
}