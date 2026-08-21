namespace Updraft.Data.Entities;

public sealed class Job : IChangeTracked
{
    public Guid JobId { get; set; }
    public Guid? RequestId { get; set; }
    public Guid AssigneeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public Guid ChangeId { get; set; }

    public Request? Request { get; set; }
    public User Assignee { get; set; } = null!;
    public ICollection<Draft> Drafts { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
}