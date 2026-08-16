namespace Updraft.Data.Entities;

public sealed class Request : IChangeTracked
{
    public Guid RequestId { get; set; }
    public Guid OfficeId { get; set; }
    public string? Proposal { get; set; }
    public string? AmendingBill { get; set; }
    public string? ReintroducingBill { get; set; }
    public string? RelatedAgencies { get; set; }
    public string? RelatedLaw { get; set; }
    public string ScopeResponse { get; set; } = string.Empty;
    public string AdministrationResponse { get; set; } = string.Empty;
    public string EnforcementResponse { get; set; } = string.Empty;
    public string TimingResponse { get; set; } = string.Empty;
    public string ExistingLawResponse { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public Guid ChangeId { get; set; }

    public Office Office { get; set; } = null!;
    public Job? Job { get; set; }
    public ICollection<RequestTag> RequestTags { get; set; } = [];
    public ICollection<RequestCommittee> RequestCommittees { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
}