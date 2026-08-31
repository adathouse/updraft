using Updraft.Data.Entities;
using Updraft.Repositories;
using Updraft.Security;

namespace Updraft.Services;

public sealed record SubmitDraftCommand(Guid JobId, string Comment);

public sealed class DraftService(
    IJobRepository jobRepository,
    IDraftRepository draftRepository)
{
    public async Task<Draft> SubmitDraftAsync(SubmitDraftCommand command, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            throw new JobNotFoundException(command.JobId);
        }

        if (job.Status != JobStatus.Open)
        {
            throw new JobNotOpenException(command.JobId);
        }

        if (job.AssigneeId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        var draftId = Guid.NewGuid();
        var draft = new Draft
        {
            DraftId = draftId,
            JobId = command.JobId,
            DrafterId = currentUser.UserId,
            Comment = command.Comment
        };

        await draftRepository.AddAsync(draft, cancellationToken);
        await draftRepository.SaveChangesAsync(cancellationToken);
        return draft;
    }
}
