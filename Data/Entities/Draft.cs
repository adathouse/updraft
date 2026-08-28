namespace Updraft.Data.Entities;

public sealed class Draft : IChangeTracked
{
    public Guid DraftId { get; set; }
    public Guid JobId { get; set; }
    public Guid DrafterId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public Guid ChangeId { get; set; }

    public Job Job { get; set; } = null!;
    public User Drafter { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
}