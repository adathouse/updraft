namespace Updraft.Data.Entities;

public sealed class Office
{
    public Guid OfficeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FormalName { get; set; } = string.Empty;
    public string Directory { get; set; } = string.Empty;
    public OfficeType OfficeType { get; set; }
    public string? IdCode { get; set; }

    public ICollection<Request> Requests { get; set; } = [];
    public ICollection<RequestCommittee> RequestCommittees { get; set; } = [];
}
