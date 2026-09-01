using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using Updraft.Repositories;
using HotChocolate.Authorization;
using Updraft.Security;

namespace Updraft.Types;

[ObjectType<Attachment>]
public static partial class AttachmentObjectType
{
    [Authorize(Policy = AuthorizationPolicies.AnyKnownRole)]
    [NodeResolver]
    public static Task<Attachment?> GetAttachmentByIdAsync(Guid id, [CurrentUser] CurrentUser? user, IAttachmentRepository attachmentRepository, CancellationToken cancellationToken) =>
        attachmentRepository.Query().VisibleTo(user.OrThrow()).FirstOrDefaultAsync(x => x.AttachmentId == id, cancellationToken);

    public static Task<Request?> GetRequestAsync([Parent] Attachment attachment, IRequestRepository requestRepository, CancellationToken cancellationToken) =>
        attachment.RequestId.HasValue
            ? requestRepository.GetByIdAsync(attachment.RequestId.Value, cancellationToken)
            : Task.FromResult<Request?>(null);

    public static Task<Draft?> GetDraftAsync([Parent] Attachment attachment, IDraftRepository draftRepository, CancellationToken cancellationToken) =>
        attachment.DraftId.HasValue
            ? draftRepository.GetByIdAsync(attachment.DraftId.Value, cancellationToken)
            : Task.FromResult<Draft?>(null);
}
