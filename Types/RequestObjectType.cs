using Updraft.Data.Entities;
using Updraft.Repositories;
using EntityTag = Updraft.Data.Entities.Tag;

namespace Updraft.Types;

[ObjectType<Request>]
public static partial class RequestObjectType
{
    [NodeResolver]
    public static Task<Request?> GetRequestByIdAsync(Guid id, IRequestRepository requestRepository, CancellationToken cancellationToken) =>
        requestRepository.GetByIdAsync(id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Attachment> GetAttachments([Parent] Request request, IAttachmentRepository attachmentRepository) =>
        attachmentRepository.QueryByRequestId(request.RequestId);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Note> GetNotes([Parent] Request request, INoteRepository noteRepository) =>
        noteRepository.Query().Where(x => x.RequestId == request.RequestId);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<EntityTag> GetTags([Parent] Request request, ITagRepository tagRepository) =>
        tagRepository.QueryByRequestId(request.RequestId);

    // [UsePaging]
    // [UseFiltering]
    // [UseSorting]
    // public static IQueryable<Committee> GetProposedCommittees([Parent] Request request, ICommitteeRepository committeeRepository) =>
    //     committeeRepository.QueryByRequestId(request.RequestId);
}