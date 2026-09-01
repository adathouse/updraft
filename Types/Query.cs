using HotChocolate;
using HotChocolate.Authorization;
using Updraft.Data.Entities;
using Updraft.Repositories;
using Updraft.Security;

namespace Updraft.Types;

[QueryType]
public static partial class Query
{
    [Authorize(Policy = AuthorizationPolicies.RequesterOrFrontOffice)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Request> GetRequests([CurrentUser] CurrentUser? user, IRequestRepository requestRepository) =>
        requestRepository.Query().VisibleTo(user.OrThrow());

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Job> GetJobs([CurrentUser] CurrentUser? user, IJobRepository jobRepository) =>
        jobRepository.Query().VisibleTo(user.OrThrow());

    [Authorize(Policy = AuthorizationPolicies.DrafterOrRequester)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Draft> GetDrafts([CurrentUser] CurrentUser? user, IDraftRepository draftsRepository) =>
        draftsRepository.Query().VisibleTo(user.OrThrow());

    [Authorize(Policy = AuthorizationPolicies.DrafterOrFrontOffice)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Office> GetOffices(IOfficeRepository officesRepository) =>
        officesRepository.Query();

    [Authorize(Policy = AuthorizationPolicies.DrafterOrFrontOffice)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<User> GetUsers(IUserRepository usersRepository) =>
        usersRepository.Query();

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetNotes(
        [CurrentUser] CurrentUser? user,
        INoteRepository noteRepository,
        [ID<Request>] Guid? requestId,
        [ID<Job>] Guid? jobId,
        [ID<Draft>] Guid? draftId,
        [ID<Note>] Guid? parentNoteId)
    {
        int parentCount = new[] { requestId, jobId, draftId, parentNoteId }.Count(x => x.HasValue);
        if (parentCount != 1)
        {
            throw new InvalidOperationException("Exactly one note parent filter must be provided.");
        }

        IQueryable<Note> query = noteRepository.Query().VisibleTo(user.OrThrow());

        if (requestId.HasValue)
        {
            return query.Where(x => x.RequestId == requestId.Value);
        }

        if (jobId.HasValue)
        {
            return query.Where(x => x.JobId == jobId.Value);
        }

        if (draftId.HasValue)
        {
            return query.Where(x => x.DraftId == draftId.Value);
        }

        return query.Where(x => x.ParentNoteId == parentNoteId!.Value);
    }

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Attachment> GetAttachments(
       [CurrentUser] CurrentUser? user,
       IAttachmentRepository attachmentRepository,
       [ID<Request>] Guid? requestId,
       [ID<Draft>] Guid? draftId)
    {

        if (requestId.HasValue && draftId.HasValue)
        {
            throw new InvalidOperationException("Exactly one note parent filter must be provided.");
        }

        IQueryable<Attachment> query = attachmentRepository.Query().VisibleTo(user.OrThrow());

        if (requestId.HasValue)
        {
            return query.Where(x => x.RequestId == requestId.Value);
        }

        if (draftId.HasValue)
        {
            return query.Where(x => x.DraftId == draftId.Value);
        }

        return Enumerable.Empty<Attachment>().AsQueryable();
    }
}
