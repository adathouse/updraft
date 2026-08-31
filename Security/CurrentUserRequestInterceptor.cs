using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Security;

// Resolves the current user once per HTTP request and exposes it as global state for [CurrentUser].
public sealed class CurrentUserRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        requestBuilder.SetGlobalState(CurrentUserAttribute.StateKey, await ResolveAsync(context, cancellationToken));
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }

    private static async ValueTask<CurrentUser?> ResolveAsync(HttpContext context, CancellationToken cancellationToken)
    {
        PrincipalIdentity? identity = PrincipalIdentity.FromPrincipal(context.User);
        if (identity is null)
        {
            return null;
        }

        IUserRepository userRepository = context.RequestServices.GetRequiredService<IUserRepository>();
        User? user = await userRepository.GetByEntraIdAsync(identity.EntraId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return new CurrentUser(
            user.UserId,
            user.EntraId,
            identity.Roles.Contains(RoleNames.Requester),
            identity.Roles.Contains(RoleNames.Drafter),
            identity.Roles.Contains(RoleNames.FrontOffice));
    }
}
