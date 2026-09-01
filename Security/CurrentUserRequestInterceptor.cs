using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        ICurrentUserResolver resolver = context.RequestServices.GetRequiredService<ICurrentUserResolver>();
        CurrentUser? currentUser = await resolver.ResolveAsync(context.User, cancellationToken);
        requestBuilder.SetGlobalState(CurrentUserAttribute.StateKey, currentUser);
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
