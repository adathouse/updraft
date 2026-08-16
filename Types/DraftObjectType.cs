using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Types;

[ObjectType<Draft>]
public static partial class DraftObjectType
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Attachment> GetAttachments([Parent] Draft draft, IAttachmentRepository attachmentRepository) =>
        attachmentRepository.QueryByDraftId(draft.DraftId);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetNotes([Parent] Draft draft, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.DraftId == draft.DraftId);

    public static Task<Job?> GetJobAsync([Parent] Draft draft, IJobRepository jobRepository, CancellationToken cancellationToken) =>
        jobRepository.GetByIdAsync(draft.JobId, cancellationToken);
}