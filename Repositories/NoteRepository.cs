using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface INoteRepository
{
    IQueryable<Note> Query();
    Task<Note?> GetByIdAsync(Guid noteId, CancellationToken cancellationToken);
    Task AddAsync(Note note, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class NoteRepository(UpdraftDbContext dbContext) : INoteRepository
{
    public IQueryable<Note> Query() => dbContext.Notes.AsNoTracking();

    public Task<Note?> GetByIdAsync(Guid noteId, CancellationToken cancellationToken) =>
        dbContext.Notes.FirstOrDefaultAsync(x => x.NoteId == noteId, cancellationToken);

    public Task AddAsync(Note note, CancellationToken cancellationToken) =>
        dbContext.Notes.AddAsync(note, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}