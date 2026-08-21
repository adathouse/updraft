namespace Updraft.Data.Entities;

public sealed class Tag : IChangeTracked
{
    public string TagId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid ChangeId { get; set; }

    public ICollection<RequestTag> RequestTags { get; set; } = [];
}

public sealed class RequestTag
{
    public Guid RequestId { get; set; }
    public string TagId { get; set; } = string.Empty;

    public Request Request { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}