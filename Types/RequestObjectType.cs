using HotChocolate.Types;
using Updraft.Data.Entities;
using Updraft.Repositories;
using EntityTag = Updraft.Data.Entities.Tag;

namespace Updraft.Types;

[ObjectType<Request>]
public static partial class RequestObjectType
{
    static partial void Configure(IObjectTypeDescriptor<Request> descriptor)
    {
        descriptor.Ignore(x => x.RequestTags);
        descriptor.Ignore(x => x.RequestCommittees);
    }

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

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Office> GetProposedCommittees([Parent] Request request, IOfficeRepository officeRepository) =>
        officeRepository.QueryByRequestId(request.RequestId);
}