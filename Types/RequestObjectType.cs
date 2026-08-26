using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
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

    public static Task<Job?> GetJobAsync([Parent] Request request, IJobRepository jobRepository, CancellationToken cancellationToken) =>
        jobRepository.Query().FirstOrDefaultAsync(x => x.RequestId == request.RequestId, cancellationToken);

    public static Task<Office?> GetOfficeAsync([Parent] Request request, IOfficeRepository officeRepository, CancellationToken cancellationToken) =>
        officeRepository.Query().FirstOrDefaultAsync(x => x.OfficeId == request.OfficeId, cancellationToken);

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