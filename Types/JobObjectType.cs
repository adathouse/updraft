using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Types;

[ObjectType<Job>]
public static partial class JobObjectType
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Draft> GetDrafts([Parent] Job job, IDraftRepository draftRepository) =>
        draftRepository.Query().Where(x => x.JobId == job.JobId);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetNotes([Parent] Job job, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.JobId == job.JobId);

    public static Task<User?> GetAssigneeAsync([Parent] Job job, IUserRepository userRepository, CancellationToken cancellationToken) =>
        userRepository.GetByIdAsync(job.AssigneeId, cancellationToken);

    public static Task<Request?> GetRequestAsync([Parent] Job job, IRequestRepository requestRepository, CancellationToken cancellationToken) =>
        job.RequestId.HasValue
            ? requestRepository.GetByIdAsync(job.RequestId.Value, cancellationToken)
            : Task.FromResult<Request?>(null);
}