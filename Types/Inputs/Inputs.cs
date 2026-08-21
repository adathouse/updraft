using Updraft.Data.Entities;

namespace Updraft.Types.Inputs;

public sealed record CreateRequestInput(
    Guid OfficeId,
    string? Proposal,
    string? AmendingBill,
    string? ReintroducingBill,
    string? RelatedAgencies,
    string? RelatedLaw,
    string ScopeResponse,
    string AdministrationResponse,
    string EnforcementResponse,
    string TimingResponse,
    string ExistingLawResponse,
    IReadOnlyList<Guid> CommitteeIds,
    IReadOnlyList<string> TagIds);

public sealed record CreateJobInput(Guid RequestId, Guid AssigneeId, string Description);

public sealed record SubmitDraftInput(Guid JobId, string Comment);

public sealed record AddNoteInput(string Text, Guid? RequestId, Guid? JobId, Guid? DraftId);

public sealed record ReplyToNoteInput(Guid ParentNoteId, string Text);

public sealed record AddAttachmentInput(AttachmentRole Role, Guid? RequestId, Guid? DraftId);
