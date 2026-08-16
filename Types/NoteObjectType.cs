using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Types;

[ObjectType<Note>]
public static partial class NoteObjectType
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetReplies([Parent] Note note, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.ParentNoteId == note.NoteId);
}