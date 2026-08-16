using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record SubmitDraftCommand(Guid JobId, string Comment, IReadOnlyList<NewAttachmentCommand> Attachments);

public sealed class DraftService(IJobRepository jobRepository, IDraftRepository draftRepository)
{
    public async Task<Draft> SubmitDraftAsync(SubmitDraftCommand command, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            throw new InvalidOperationException("Job was not found.");
        }

        if (job.Status != JobStatus.Open)
        {
            throw new InvalidOperationException("Job is not open.");
        }

        if (command.Attachments.Count == 0)
        {
            throw new InvalidOperationException("At least one attachment is required.");
        }

        var draftId = Guid.NewGuid();
        var draft = new Draft
        {
            DraftId = draftId,
            JobId = command.JobId,
            Comment = command.Comment,
            Attachments = command.Attachments
                .Select(attachment => new Attachment
                {
                    AttachmentId = Guid.NewGuid(),
                    DraftId = draftId,
                    StorageKey = attachment.StorageKey,
                    AttachmentRole = attachment.AttachmentRole
                })
                .ToList()
        };

        await draftRepository.AddAsync(draft, cancellationToken);
        await draftRepository.SaveChangesAsync(cancellationToken);
        return draft;
    }
}