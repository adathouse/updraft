using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface IJobRepository
{
    IQueryable<Job> Query();
    Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);
    Task AddAsync(Job job, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class JobRepository(UpdraftDbContext dbContext) : IJobRepository
{
    public IQueryable<Job> Query() => dbContext.Jobs.AsNoTracking();

    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken) =>
        dbContext.Jobs.FirstOrDefaultAsync(x => x.JobId == jobId, cancellationToken);

    public Task AddAsync(Job job, CancellationToken cancellationToken) =>
        dbContext.Jobs.AddAsync(job, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}