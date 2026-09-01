using Updraft.Data.Entities;
using Updraft.Repositories;
using HotChocolate.Authorization;
using Updraft.Security;

namespace Updraft.Types;

[ObjectType<User>]
public static partial class UserObjectType
{
    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [NodeResolver]
    public static Task<User?> GetUserByIdAsync(Guid id, IUserRepository userRepository, CancellationToken cancellationToken) =>
        userRepository.GetByIdAsync(id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Job> GetAssignedJobs([Parent] User user, [CurrentUser] CurrentUser? currentUser, IJobRepository jobRepository) =>
        jobRepository.Query().Where(x => x.AssigneeId == user.UserId).VisibleTo(currentUser.OrThrow());

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Request> GetRequests([Parent] User user, [CurrentUser] CurrentUser? currentUser, IRequestRepository requestRepository) =>
        requestRepository.Query().Where(x => x.RequesterId == user.UserId).VisibleTo(currentUser.OrThrow());

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Draft> GetDrafts([Parent] User user, [CurrentUser] CurrentUser? currentUser, IDraftRepository draftRepository) =>
        draftRepository.Query().Where(x => x.DrafterId == user.UserId).VisibleTo(currentUser.OrThrow());

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetOwnedNotes([Parent] User user, [CurrentUser] CurrentUser? currentUser, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.OwnerId == user.UserId).VisibleTo(currentUser.OrThrow());
}
