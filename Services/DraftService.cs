using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record SubmitDraftCommand(Guid JobId, string Comment);

public sealed class DraftService(IJobRepository jobRepository, IDraftRepository draftRepository)
{
    public async Task<Draft> SubmitDraftAsync(SubmitDraftCommand command, CancellationToken cancellationToken)
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

        var draftId = Guid.NewGuid();
        var draft = new Draft
        {
            DraftId = draftId,
            JobId = command.JobId,
            Comment = command.Comment
        };

        await draftRepository.AddAsync(draft, cancellationToken);
        await draftRepository.SaveChangesAsync(cancellationToken);
        return draft;
    }
}
