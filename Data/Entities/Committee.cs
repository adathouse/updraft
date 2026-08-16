namespace Updraft.Data.Entities;

public sealed class Committee : IChangeTracked
{
    public Guid CommitteeId { get; set; }
    public Guid OfficeId { get; set; }
    public string CommitteeCode { get; set; } = string.Empty;
    public string CommitteeName { get; set; } = string.Empty;
    public Guid ChangeId { get; set; }

    public Office Office { get; set; } = null!;
    public ICollection<RequestCommittee> RequestCommittees { get; set; } = [];
}