using Microsoft.EntityFrameworkCore;
using Updraft.Data;
using Updraft.Data.Entities;

namespace Updraft.Repositories;

public interface IAttachmentRepository
{
    IQueryable<Attachment> Query();
    IQueryable<Attachment> QueryByRequestId(Guid requestId);
    IQueryable<Attachment> QueryByDraftId(Guid draftId);
}

public sealed class AttachmentRepository(UpdraftDbContext dbContext) : IAttachmentRepository
{
    public IQueryable<Attachment> Query() => dbContext.Attachments.AsNoTracking();

    public IQueryable<Attachment> QueryByRequestId(Guid requestId) =>
        dbContext.Attachments.AsNoTracking().Where(x => x.RequestId == requestId);

    public IQueryable<Attachment> QueryByDraftId(Guid draftId) =>
        dbContext.Attachments.AsNoTracking().Where(x => x.DraftId == draftId);
}