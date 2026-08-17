using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Types;

[ObjectType<Note>]
public static partial class NoteObjectType
{
    [NodeResolver]
    public static Task<Note?> GetNoteByIdAsync(Guid id, INoteRepository noteRepository, CancellationToken cancellationToken) =>
        noteRepository.GetByIdAsync(id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetReplies([Parent] Note note, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.ParentNoteId == note.NoteId);
}