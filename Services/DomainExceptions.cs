namespace Updraft.Services;

public sealed class OfficeNotFoundException(Guid officeId) : Exception("Office was not found.")
{
    public Guid OfficeId { get; } = officeId;
}

public sealed class TagsNotFoundException() : Exception("One or more tags were not found.")
{
}

public sealed class CommitteesNotFoundException() : Exception("One or more committees were not found.")
{
}

public sealed class RequestNotFoundException(Guid requestId) : Exception("Request was not found.")
{
    public Guid RequestId { get; } = requestId;
}

public sealed class RequestNotUnassignedException(Guid requestId) : Exception("Request is not unassigned.")
{
    public Guid RequestId { get; } = requestId;
}

public sealed class AssigneeNotFoundException(Guid assigneeId) : Exception("Assignee was not found.")
{
    public Guid AssigneeId { get; } = assigneeId;
}

public sealed class UserNotFoundException(Guid userId) : Exception("User was not found.")
{
    public Guid UserId { get; } = userId;
}

public sealed class JobNotFoundException(Guid jobId) : Exception("Job was not found.")
{
    public Guid JobId { get; } = jobId;
}

public sealed class JobNotOpenException(Guid jobId) : Exception("Job is not open.")
{
    public Guid JobId { get; } = jobId;
}

public sealed class DraftNotFoundException(Guid draftId) : Exception("Draft was not found.")
{
    public Guid DraftId { get; } = draftId;
}

public sealed class NoteNotFoundException(Guid noteId) : Exception("Parent note was not found.")
{
    public Guid NoteId { get; } = noteId;
}

public sealed class InvalidNoteParentException() : Exception("Exactly one parent (request, job, draft) must be provided.")
{
}

public sealed class InvalidAttachmentParentException() : Exception("Specify either draft or request.")
{
}

public sealed class AttachmentNotFoundException(Guid attachmentId) : Exception("Attachment was not found.")
{
    public Guid AttachmentId { get; } = attachmentId;
}
