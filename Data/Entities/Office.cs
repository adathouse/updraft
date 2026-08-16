namespace Updraft.Data.Entities;

public sealed class Office : IChangeTracked
{
    public Guid OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string OfficeGraph { get; set; } = string.Empty;
    public OfficeType OfficeType { get; set; }
    public string? Bioguide { get; set; }
    public string? Commcode { get; set; }
    public Guid ChangeId { get; set; }

    public ICollection<Request> Requests { get; set; } = [];
    public ICollection<Committee> Committees { get; set; } = [];
}
