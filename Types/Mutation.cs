using HotChocolate;
using HotChocolate.Authorization;
using Updraft.Data.Entities;
using Updraft.Security;
using Updraft.Services;

namespace Updraft.Types;

[MutationType]
public static partial class Mutation
{
    //[Authorize(Policy = AuthorizationPolicies.Requester)]
    [Error<OfficeNotFoundException>]
    [Error<TagsNotFoundException>]
    [Error<CommitteesNotFoundException>]
    public static Task<Request> SubmitRequestAsync(
        Guid officeId,
        string? proposal,
        string? amendingBill,
        string? reintroducingBill,
        string? relatedAgencies,
        string? relatedLaw,
        string scopeResponse,
        string administrationResponse,
        string enforcementResponse,
        string timingResponse,
        string existingLawResponse,
        IReadOnlyList<Guid> committeeIds,
        IReadOnlyList<string> tagIds,
        RequestService requestService,
        CancellationToken cancellationToken) =>
        requestService.CreateRequestAsync(
            new CreateRequestCommand(
                officeId,
                proposal,
                amendingBill,
                reintroducingBill,
                relatedAgencies,
                relatedLaw,
                scopeResponse,
                administrationResponse,
                enforcementResponse,
                timingResponse,
                existingLawResponse,
                committeeIds,
                tagIds),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.Requester)]
    [Error<RequestNotFoundException>]
    public static Task<Request> UpdateRequestAsync(
        Guid requestId,
        string? proposal,
        string? amendingBill,
        string? reintroducingBill,
        string? relatedAgencies,
        string? relatedLaw,
        string scopeResponse,
        string administrationResponse,
        string enforcementResponse,
        string timingResponse,
        string existingLawResponse,
        RequestStatus status,
        RequestService requestService,
        CancellationToken cancellationToken) =>
        requestService.UpdateRequestAsync(
            new UpdateRequestCommand(
                requestId,
                proposal,
                amendingBill,
                reintroducingBill,
                relatedAgencies,
                relatedLaw,
                scopeResponse,
                administrationResponse,
                enforcementResponse,
                timingResponse,
                existingLawResponse,
                status),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.FrontOffice)]
    [Error<RequestNotFoundException>]
    [Error<RequestNotUnassignedException>]
    [Error<AssigneeNotFoundException>]
    public static Task<Job> CreateJobAsync(
        Guid requestId,
        Guid assigneeId,
        string description,
        JobService jobService,
        CancellationToken cancellationToken) =>
        jobService.CreateJobAsync(
            new CreateJobCommand(requestId, assigneeId, description),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.DrafterOrFrontOffice)]
    [Error<JobNotFoundException>]
    [Error<AssigneeNotFoundException>]
    public static Task<Job> UpdateJobAsync(
        Guid jobId,
        Guid assigneeId,
        string description,
        JobStatus status,
        JobService jobService,
        CancellationToken cancellationToken) =>
        jobService.UpdateJobAsync(
            new UpdateJobCommand(jobId, assigneeId, description, status),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.Drafter)]
    [Error<JobNotFoundException>]
    [Error<JobNotOpenException>]
    public static Task<Draft> SubmitDraftAsync(
        Guid jobId,
        string comment,
        DraftService draftService,
        CancellationToken cancellationToken) =>
        draftService.SubmitDraftAsync(
            new SubmitDraftCommand(
                jobId,
                comment),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [Error<InvalidNoteParentException>]
    [Error<RequestNotFoundException>]
    [Error<JobNotFoundException>]
    [Error<DraftNotFoundException>]
    public static Task<Note> AddNoteAsync(
        string text,
        Guid? requestId,
        Guid? jobId,
        Guid? draftId,
        NoteService noteService,
        CancellationToken cancellationToken) =>
        noteService.AddNoteAsync(
            new AddNoteCommand(text, requestId, jobId, draftId),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [Error<NoteNotFoundException>]
    public static Task<Note> ReplyToNoteAsync(
        Guid parentNoteId,
        string text,
        NoteService noteService,
        CancellationToken cancellationToken) =>
        noteService.ReplyToNoteAsync(
            new ReplyToNoteCommand(parentNoteId, text),
            cancellationToken);

    //[Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [Error<RequestNotFoundException>]
    [Error<DraftNotFoundException>]
    [Error<InvalidAttachmentParentException>]
    public static Task<Attachment> AddAttachmentAsync(
        AttachmentRole role,
        Guid? requestId,
        Guid? draftId,
        AttachmentService attachmentService,
        CancellationToken cancellationToken) =>
        attachmentService.AddAttachmentAsync(
            new AddAttachmentCommand(role, requestId, draftId),
            cancellationToken);
}
