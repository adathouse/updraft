using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using Updraft.Repositories;
using Updraft.Security;

namespace Updraft.Services;

public sealed record AddNoteCommand(string Text, Guid? RequestId, Guid? JobId, Guid? DraftId);
public sealed record ReplyToNoteCommand(Guid ParentNoteId, string Text);

public sealed class NoteService(
    INoteRepository noteRepository,
    IRequestRepository requestRepository,
    IJobRepository jobRepository,
    IDraftRepository draftRepository)
{
    public async Task<Note> AddNoteAsync(AddNoteCommand command, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        int parentCount = new[] { command.RequestId, command.JobId, command.DraftId }.Count(x => x.HasValue);
        if (parentCount != 1)
        {
            throw new InvalidNoteParentException();
        }

        if (command.RequestId.HasValue)
        {
            if (!await requestRepository.ExistsAsync(command.RequestId.Value, cancellationToken))
            {
                throw new RequestNotFoundException(command.RequestId.Value);
            }

            if (!await requestRepository.Query().VisibleTo(currentUser).AnyAsync(x => x.RequestId == command.RequestId.Value, cancellationToken))
            {
                throw new ForbiddenAccessException();
            }
        }

        if (command.JobId.HasValue)
        {
            if (await jobRepository.GetByIdAsync(command.JobId.Value, cancellationToken) is null)
            {
                throw new JobNotFoundException(command.JobId.Value);
            }

            if (!await jobRepository.Query().VisibleTo(currentUser).AnyAsync(x => x.JobId == command.JobId.Value, cancellationToken))
            {
                throw new ForbiddenAccessException();
            }
        }

        if (command.DraftId.HasValue)
        {
            if (await draftRepository.GetByIdAsync(command.DraftId.Value, cancellationToken) is null)
            {
                throw new DraftNotFoundException(command.DraftId.Value);
            }

            if (!await draftRepository.Query().VisibleTo(currentUser).AnyAsync(x => x.DraftId == command.DraftId.Value, cancellationToken))
            {
                throw new ForbiddenAccessException();
            }
        }

        var note = new Note
        {
            NoteId = Guid.NewGuid(),
            Text = command.Text,
            OwnerId = currentUser.UserId,
            RequestId = command.RequestId,
            JobId = command.JobId,
            DraftId = command.DraftId
        };

        await noteRepository.AddAsync(note, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task<Note> ReplyToNoteAsync(ReplyToNoteCommand command, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        Note? parent = await noteRepository.GetByIdAsync(command.ParentNoteId, cancellationToken);
        if (parent is null)
        {
            throw new NoteNotFoundException(command.ParentNoteId);
        }

        if (!await noteRepository.Query().VisibleTo(currentUser).AnyAsync(x => x.NoteId == command.ParentNoteId, cancellationToken))
        {
            throw new ForbiddenAccessException();
        }

        var reply = new Note
        {
            NoteId = Guid.NewGuid(),
            Text = command.Text,
            OwnerId = currentUser.UserId,
            ParentNoteId = command.ParentNoteId
        };

        await noteRepository.AddAsync(reply, cancellationToken);
        await noteRepository.SaveChangesAsync(cancellationToken);
        return reply;
    }
}
