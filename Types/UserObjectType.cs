using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Types;

[ObjectType<User>]
public static partial class UserObjectType
{
    [NodeResolver]
    public static Task<User?> GetUserByIdAsync(Guid id, IUserRepository userRepository, CancellationToken cancellationToken) =>
        userRepository.GetByIdAsync(id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Job> GetAssignedJobs([Parent] User user, IJobRepository jobRepository) =>
        jobRepository.Query().Where(x => x.AssigneeId == user.UserId);
}
