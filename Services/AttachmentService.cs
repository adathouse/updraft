


using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record AddAttachmentCommand(AttachmentRole Role, Guid? RequestId, Guid? DraftId);

public sealed class AttachmentService(IAttachmentRepository attachmentRepository,
    IRequestRepository requestRepository,
    IDraftRepository draftRepository)
{
    public async Task<Attachment> AddAttachmentAsync(AddAttachmentCommand command, CancellationToken cancellation)
    {
        if (command.RequestId.HasValue && !await requestRepository.ExistsAsync(command.RequestId.Value, cancellation))
        {
            throw new RequestNotFoundException(command.RequestId.Value);
        }

        if (command.DraftId.HasValue && await draftRepository.GetByIdAsync(command.DraftId.Value, cancellation) is null)
        {
            throw new DraftNotFoundException(command.DraftId.Value);
        }

        if (command.RequestId.HasValue && command.DraftId.HasValue)
        {
            throw new InvalidAttachmentParentException();
        }

        var prefix = command.RequestId.HasValue ? "request" : "draft";
        var pathId = command.RequestId.HasValue ? command.RequestId : command.DraftId;
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

}
