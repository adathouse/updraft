namespace Updraft.Data.Entities;

public sealed class RequestCommittee
{
    public Guid RequestId { get; set; }
    public Guid CommitteeId { get; set; }

    public Request Request { get; set; } = null!;
    public Committee Committee { get; set; } = null!;
}