using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using Updraft.Data.Entities;
using Updraft.Security;
using Updraft.Services;

namespace Updraft.Types;

[MutationType]
public static partial class Mutation
{
    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    public static Task<User> RegisterCurrentUserAsync(
        ClaimsPrincipal claimsPrincipal,
        UserService userService,
        CancellationToken cancellationToken) =>
        userService.RegisterCurrentUserAsync(claimsPrincipal, cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.Requester)]
    [Error<OfficeNotFoundException>]
    [Error<TagsNotFoundException>]
    [Error<CommitteesNotFoundException>]
    public static Task<Request> SubmitRequestAsync(
        [ID<Office>] Guid officeId,
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
        [ID<Office>] IReadOnlyList<Guid> committeeIds,
        [ID<Updraft.Data.Entities.Tag>] IReadOnlyList<string> tagIds,
        [CurrentUser] CurrentUser? user,
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
            user.OrThrow(),
            cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.Requester)]
    [Error<RequestNotFoundException>]
    [Error<ForbiddenAccessException>]
    public static Task<Request> UpdateRequestAsync(
        [ID<Request>] Guid requestId,
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
        [CurrentUser] CurrentUser? user,
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
            user.OrThrow(),
            cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.FrontOffice)]
    [Error<RequestNotFoundException>]
    [Error<RequestNotUnassignedException>]
    [Error<AssigneeNotFoundException>]
    public static Task<Job> CreateJobAsync(
        [ID<Request>] Guid requestId,
        [ID<User>] Guid assigneeId,
        string description,
        JobService jobService,
        CancellationToken cancellationToken) =>
        jobService.CreateJobAsync(
            new CreateJobCommand(requestId, assigneeId, description),
            cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.DrafterOrFrontOffice)]
    [Error<JobNotFoundException>]
    [Error<AssigneeNotFoundException>]
    [Error<ForbiddenAccessException>]
    public static Task<Job> UpdateJobAsync(
        [ID<Job>] Guid jobId,
        [ID<User>] Guid assigneeId,
        string description,
        JobStatus status,
        [CurrentUser] CurrentUser? user,
        JobService jobService,
        CancellationToken cancellationToken) =>
        jobService.UpdateJobAsync(
            new UpdateJobCommand(jobId, assigneeId, description, status),
            user.OrThrow(),
            cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.Drafter)]
    [Error<JobNotFoundException>]
    [Error<JobNotOpenException>]
    [Error<ForbiddenAccessException>]
    public static Task<Draft> SubmitDraftAsync(
        [ID<Job>] Guid jobId,
        string comment,
        [CurrentUser] CurrentUser? user,
        DraftService draftService,
        CancellationToken cancellationToken) =>
        draftService.SubmitDraftAsync(
            new SubmitDraftCommand(
                jobId,
                comment),
            user.OrThrow(),
            cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [Error<InvalidNoteParentException>]
    [Error<RequestNotFoundException>]
    [Error<JobNotFoundException>]
    [Error<DraftNotFoundException>]
    [Error<ForbiddenAccessException>]
    public static Task<Note> AddNoteAsync(
        string text,
        [ID<Request>] Guid? requestId,
        [ID<Job>] Guid? jobId,
        [ID<Draft>] Guid? draftId,
        [CurrentUser] CurrentUser? user,
        NoteService noteService,
        CancellationToken cancellationToken) =>
        noteService.AddNoteAsync(
            new AddNoteCommand(text, requestId, jobId, draftId),
            user.OrThrow(),
            cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [Error<NoteNotFoundException>]
    [Error<ForbiddenAccessException>]
    public static Task<Note> ReplyToNoteAsync(
        [ID<Note>] Guid parentNoteId,
        string text,
        [CurrentUser] CurrentUser? user,
        NoteService noteService,
        CancellationToken cancellationToken) =>
        noteService.ReplyToNoteAsync(
            new ReplyToNoteCommand(parentNoteId, text),
            user.OrThrow(),
            cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [Error<RequestNotFoundException>]
    [Error<DraftNotFoundException>]
    [Error<InvalidAttachmentParentException>]
    [Error<ForbiddenAccessException>]
    public static Task<Attachment> AddAttachmentAsync(
        AttachmentRole role,
        [ID<Request>] Guid? requestId,
        [ID<Draft>] Guid? draftId,
        [CurrentUser] CurrentUser? user,
        AttachmentService attachmentService,
        CancellationToken cancellationToken) =>
        attachmentService.AddAttachmentAsync(
            new AddAttachmentCommand(role, requestId, draftId),
            user.OrThrow(),
            cancellationToken);
}
