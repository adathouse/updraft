namespace Updraft.Data.Entities;

public sealed class Attachment : IChangeTracked
{
    public Guid AttachmentId { get; set; }
    public Guid? RequestId { get; set; }
    public Guid? DraftId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public AttachmentRole AttachmentRole { get; set; }
    public Guid ChangeId { get; set; }

    public Request? Request { get; set; }
    public Draft? Draft { get; set; }
}