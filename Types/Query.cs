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
    public static IQueryable<Request> GetUnassignedRequests(IRequestRepository requestRepository) =>
        requestRepository.Query().Where(x => x.Status == RequestStatus.Unassigned);

    [Authorize(Policy = AuthorizationPolicies.DrafterOrFrontOffice)]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Job> GetOpenJobs(IJobRepository jobRepository, Guid? assigneeId) =>
        assigneeId.HasValue
            ? jobRepository.Query().Where(x => x.Status == JobStatus.Open && x.AssigneeId == assigneeId.Value)
            : jobRepository.Query().Where(x => x.Status == JobStatus.Open);

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    public static Task<Job?> GetJobByIdAsync(Guid jobId, IJobRepository jobRepository, CancellationToken cancellationToken) =>
        jobRepository.GetByIdAsync(jobId, cancellationToken);

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
}