using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record CreateJobCommand(Guid RequestId, Guid AssigneeId, string Description);

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
}