namespace Updraft.Data.Entities;

public sealed class Note : IChangeTracked
{
    public Guid NoteId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid? RequestId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? DraftId { get; set; }
    public Guid? ParentNoteId { get; set; }
    public Guid ChangeId { get; set; }

    public Request? Request { get; set; }
    public Job? Job { get; set; }
    public Draft? Draft { get; set; }
    public Note? ParentNote { get; set; }
    public ICollection<Note> Replies { get; set; } = [];
}