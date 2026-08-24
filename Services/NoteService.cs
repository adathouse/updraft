using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record AddNoteCommand(string Text, Guid? RequestId, Guid? JobId, Guid? DraftId);
public sealed record ReplyToNoteCommand(Guid ParentNoteId, string Text);

public sealed class NoteService(
    INoteRepository noteRepository,
    IRequestRepository requestRepository,
    IJobRepository jobRepository,
    IDraftRepository draftRepository)
{
    public async Task<Note> AddNoteAsync(AddNoteCommand command, CancellationToken cancellationToken)
    {
        int parentCount = new[] { command.RequestId, command.JobId, command.DraftId }.Count(x => x.HasValue);
        if (parentCount != 1)
        {
            throw new InvalidNoteParentException();
        }

        if (command.RequestId.HasValue && !await requestRepository.ExistsAsync(command.RequestId.Value, cancellationToken))
        {
            throw new RequestNotFoundException(command.RequestId.Value);
        }

        if (command.JobId.HasValue && await jobRepository.GetByIdAsync(command.JobId.Value, cancellationToken) is null)
        {
            throw new JobNotFoundException(command.JobId.Value);
        }

        if (command.DraftId.HasValue && await draftRepository.GetByIdAsync(command.DraftId.Value, cancellationToken) is null)
        {
            throw new DraftNotFoundException(command.DraftId.Value);
        }

        var note = new Note
        {
            NoteId = Guid.NewGuid(),
            Text = command.Text,
            RequestId = command.RequestId,
            JobId = command.JobId,
            DraftId = command.DraftId
        };

        await noteRepository.AddAsync(note, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task<Note> ReplyToNoteAsync(ReplyToNoteCommand command, CancellationToken cancellationToken)
    {
        Note? parent = await noteRepository.GetByIdAsync(command.ParentNoteId, cancellationToken);
        if (parent is null)
        {
            throw new NoteNotFoundException(command.ParentNoteId);
        }

        var reply = new Note
        {
            NoteId = Guid.NewGuid(),
            Text = command.Text,
            ParentNoteId = command.ParentNoteId
        };

        await noteRepository.AddAsync(reply, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);
        return reply;
    }
}
