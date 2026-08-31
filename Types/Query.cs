using HotChocolate.Authorization;
using Updraft.Data.Entities;
using Updraft.Repositories;
using Updraft.Security;

namespace Updraft.Types;

[QueryType]
public static partial class Query
{
    [Authorize(Policy = AuthorizationPolicies.FrontOffice)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Request> GetRequests(IRequestRepository requestRepository) =>
        requestRepository.Query();

    [Authorize(Policy = AuthorizationPolicies.DrafterOrFrontOffice)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Job> GetJobs(IJobRepository jobRepository) =>
        jobRepository.Query();

    [Authorize(Policy = AuthorizationPolicies.DrafterOrFrontOffice)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Draft> GetDrafts(IDraftRepository draftsRepository) =>
        draftsRepository.Query();

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
        INoteRepository noteRepository,
        Guid? requestId,
        Guid? jobId,
        Guid? draftId,
        Guid? parentNoteId)
    {
        int parentCount = new[] { requestId, jobId, draftId, parentNoteId }.Count(x => x.HasValue);
        if (parentCount != 1)
        {
            throw new InvalidOperationException("Exactly one note parent filter must be provided.");
        }

        IQueryable<Note> query = noteRepository.Query();

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
       IAttachmentRepository attachmentRepository,
       Guid? requestId,
       Guid? draftId)
    {

        if (requestId.HasValue && draftId.HasValue)
        {
            throw new InvalidOperationException("Exactly one note parent filter must be provided.");
        }

        if (requestId.HasValue)
        {
            return attachmentRepository.Query().Where(x => x.RequestId == requestId.Value);
        }

        if (draftId.HasValue)
        {
            return attachmentRepository.Query().Where(x => x.DraftId == draftId.Value);
        }

        return Enumerable.Empty<Attachment>().AsQueryable();
    }
}
