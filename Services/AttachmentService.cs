

using Foundatio.Storage;
using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using Updraft.Repositories;
using Updraft.Security;

namespace Updraft.Services;

public sealed record AddAttachmentCommand(AttachmentRole Role, Guid? RequestId, Guid? DraftId);

public sealed record AttachDocumentCommand(
    Guid AttachmentId,
    Stream Content,
    string FileName,
    string ContentType);

public sealed class AttachmentService(IAttachmentRepository attachmentRepository,
    IRequestRepository requestRepository,
    IDraftRepository draftRepository,
    IFileStorage fileStorage)
{
    public async Task<Attachment> AddAttachmentAsync(AddAttachmentCommand command, CurrentUser currentUser, CancellationToken cancellation)
    {
        await ValidateParentAsync(command.RequestId, command.DraftId, cancellation);
        await EnsureCanAccessParentAsync(command.RequestId, command.DraftId, currentUser, cancellation);

        var (prefix, pathId) = ResolveParent(command.RequestId, command.DraftId);
        var attachmentId = Guid.NewGuid();

        var result = new Attachment
        {
            AttachmentId = attachmentId,
            AttachmentRole = command.Role,
            RequestId = command.RequestId,
            DraftId = command.DraftId,
            StorageKey = "TBD",
            AttachmentUri = $"{prefix}/{pathId}/{attachmentId}"
        };

        await attachmentRepository.AddAsync(result, cancellation);
        await attachmentRepository.SaveChangesAsync(cancellation);
        return result;
    }

    public async Task<string> AttachDocumentAsync(AttachDocumentCommand command, CurrentUser currentUser, CancellationToken cancellation)
    {
        var attachment = await attachmentRepository.GetByIdAsync(command.AttachmentId, cancellation)
            ?? throw new AttachmentNotFoundException(command.AttachmentId);

        await ValidateParentAsync(attachment.RequestId, attachment.DraftId, cancellation);
        await EnsureCanAccessParentAsync(attachment.RequestId, attachment.DraftId, currentUser, cancellation);
        var (prefix, pathId) = ResolveParent(attachment.RequestId, attachment.DraftId);
        var fileName = System.IO.Path.GetFileName(command.FileName);
        var storageKey = $"{prefix}/{pathId}/{attachment.AttachmentId}/{fileName}";

        await fileStorage.SaveFileAsync(storageKey, command.Content, cancellation);

        attachment.StorageKey = storageKey;
        await attachmentRepository.SaveChangesAsync(cancellation);
        return attachment.StorageKey;
    }

    private async Task EnsureCanAccessParentAsync(Guid? requestId, Guid? draftId, CurrentUser currentUser, CancellationToken cancellation)
    {
        if (requestId.HasValue
            && !await requestRepository.Query().VisibleTo(currentUser).AnyAsync(x => x.RequestId == requestId.Value, cancellation))
        {
            throw new ForbiddenAccessException();
        }

        if (draftId.HasValue
            && !await draftRepository.Query().VisibleTo(currentUser).AnyAsync(x => x.DraftId == draftId.Value, cancellation))
        {
            throw new ForbiddenAccessException();
        }
    }

    private async Task ValidateParentAsync(Guid? requestId, Guid? draftId, CancellationToken cancellation)
    {
        if (requestId.HasValue && !await requestRepository.ExistsAsync(requestId.Value, cancellation))
        {
            throw new RequestNotFoundException(requestId.Value);
        }

        if (draftId.HasValue && await draftRepository.GetByIdAsync(draftId.Value, cancellation) is null)
        {
            throw new DraftNotFoundException(draftId.Value);
        }

        if (requestId.HasValue && draftId.HasValue)
        {
            throw new InvalidAttachmentParentException();
        }
    }

    private static (string Prefix, Guid? PathId) ResolveParent(Guid? requestId, Guid? draftId) =>
        requestId.HasValue ? ("request", requestId) : ("draft", draftId);
}
