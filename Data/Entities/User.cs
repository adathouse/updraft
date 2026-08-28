namespace Updraft.Data.Entities;

public sealed class User : IChangeTracked
{
    public Guid UserId { get; set; }
    public string EntraId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public Guid ChangeId { get; set; }

    public ICollection<Job> AssignedJobs { get; set; } = [];
    public ICollection<Request> Requests { get; set; } = [];
    public ICollection<Draft> Drafts { get; set; } = [];
    public ICollection<Note> OwnedNotes { get; set; } = [];
}