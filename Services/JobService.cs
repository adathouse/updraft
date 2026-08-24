using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record CreateJobCommand(Guid RequestId, Guid AssigneeId, string Description);

public sealed record UpdateJobCommand(Guid JobId, Guid AssigneeId, string Description, JobStatus Status);

public sealed class JobService(
    IJobRepository jobRepository,
    IRequestRepository requestRepository,
    IUserRepository userRepository)
{
    public async Task<Job> CreateJobAsync(CreateJobCommand command, CancellationToken cancellationToken)
    {
        Request? request = await requestRepository.GetByIdAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException("Request was not found.");
        }

        if (request.Status != RequestStatus.Unassigned)
        {
            throw new InvalidOperationException("Request is not unassigned.");
        }

        if (!await userRepository.ExistsAsync(command.AssigneeId, cancellationToken))
        {
            throw new InvalidOperationException("Assignee was not found.");
        }

        request.Status = RequestStatus.Assigned;

        var job = new Job
        {
            JobId = Guid.NewGuid(),
            RequestId = request.RequestId,
            AssigneeId = command.AssigneeId,
            Description = command.Description,
            Status = JobStatus.Open
        };

        await jobRepository.AddAsync(job, cancellationToken);
        await jobRepository.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<Job> UpdateJobAsync(UpdateJobCommand command, CancellationToken cancellationToken)
    {
        Job? job = await jobRepository.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
        {
            throw new InvalidOperationException("Job was not found.");
        }

        if (job.AssigneeId != command.AssigneeId
            && !await userRepository.ExistsAsync(command.AssigneeId, cancellationToken))
        {
            throw new InvalidOperationException("Assignee was not found.");
        }

        job.AssigneeId = command.AssigneeId;
        job.Description = command.Description;
        job.Status = command.Status;

        await jobRepository.SaveChangesAsync(cancellationToken);
        return job;
    }
}