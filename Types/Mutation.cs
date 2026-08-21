using HotChocolate.Authorization;
using Updraft.Data.Entities;
using Updraft.Security;
using Updraft.Services;
using Updraft.Types.Inputs;

namespace Updraft.Types;

[MutationType]
public static partial class Mutation
{
    //[Authorize(Policy = AuthorizationPolicies.Requester)]
    public static Task<Request> SubmitRequestAsync(
        CreateRequestInput input,
        RequestService requestService,
        CancellationToken cancellationToken) =>
        requestService.CreateRequestAsync(
            new CreateRequestCommand(
                input.OfficeId,
                input.Proposal,
                input.AmendingBill,
                input.ReintroducingBill,
                input.RelatedAgencies,
                input.RelatedLaw,
                input.ScopeResponse,
                input.AdministrationResponse,
                input.EnforcementResponse,
                input.TimingResponse,
                input.ExistingLawResponse,
                input.CommitteeIds,
                input.TagIds),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.FrontOffice)]
    public static Task<Job> CreateJobAsync(
        CreateJobInput input,
        JobService jobService,
        CancellationToken cancellationToken) =>
        jobService.CreateJobAsync(
            new CreateJobCommand(input.RequestId, input.AssigneeId, input.Description),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.Drafter)]
    public static Task<Draft> SubmitDraftAsync(
        SubmitDraftInput input,
        DraftService draftService,
        CancellationToken cancellationToken) =>
        draftService.SubmitDraftAsync(
            new SubmitDraftCommand(
                input.JobId,
                input.Comment),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    public static Task<Note> AddNoteAsync(
        AddNoteInput input,
        NoteService noteService,
        CancellationToken cancellationToken) =>
        noteService.AddNoteAsync(
            new AddNoteCommand(input.Text, input.RequestId, input.JobId, input.DraftId),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    public static Task<Note> ReplyToNoteAsync(
        ReplyToNoteInput input,
        NoteService noteService,
        CancellationToken cancellationToken) =>
        noteService.ReplyToNoteAsync(
            new ReplyToNoteCommand(input.ParentNoteId, input.Text),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    public static Task<Attachment> AddAttachmentAsync(
        AddAttachmentInput input,
        AttachmentService attachmentService,
        CancellationToken cancellationToken) =>
        attachmentService.AddAttachmentAsync(
            new AddAttachmentCommand(input.Role, input.RequestId, input.DraftId),
            cancellationToken);
}
