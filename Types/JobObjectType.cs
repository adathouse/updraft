using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using Updraft.Repositories;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Updraft.Security;

namespace Updraft.Types;

[ObjectType<Job>]
public static partial class JobObjectType
{
    static partial void Configure(IObjectTypeDescriptor<Job> descriptor)
    {
        // Identity and foreign keys are exposed only through the opaque node `id` and object relationships.
        descriptor.Ignore(x => x.JobId);
        descriptor.Ignore(x => x.RequestId);
        descriptor.Ignore(x => x.AssigneeId);
        descriptor.Ignore(x => x.ChangeId);
    }

    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [NodeResolver]
    public static Task<Job?> GetJobByIdAsync(Guid id, [CurrentUser] CurrentUser? user, IJobRepository jobRepository, CancellationToken cancellationToken) =>
        jobRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Draft> GetDrafts([Parent] Job job, [CurrentUser] CurrentUser? user, IDraftRepository draftRepository) =>
        draftRepository.Query().Where(x => x.JobId == job.JobId).VisibleTo(user.OrThrow());

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