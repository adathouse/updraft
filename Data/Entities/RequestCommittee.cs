namespace Updraft.Data.Entities;

public sealed class RequestCommittee
{
    public Guid RequestId { get; set; }
    public Guid OfficeId { get; set; }

    public Request Request { get; set; } = null!;
    public Office Office { get; set; } = null!;
}