using Updraft.Data.Entities;

namespace Updraft.Security;

// Row-level visibility filters. Front Office sees all Requests/Jobs (never Drafts); Requesters and
// Drafters see only their own data. Note/Attachment visibility follows their parent object.
public static class ResourceAccess
{
    public static IQueryable<Request> VisibleTo(this IQueryable<Request> query, CurrentUser user)
    {
        if (user.IsFrontOffice)
        {
            return query;
        }

        return query.Where(r =>
            (user.IsRequester && r.RequesterId == user.UserId)
            || (user.IsDrafter && r.Job != null && r.Job.AssigneeId == user.UserId));
    }

    public static IQueryable<Job> VisibleTo(this IQueryable<Job> query, CurrentUser user)
    {
        if (user.IsFrontOffice)
        {
            return query;
        }

        return query.Where(j =>
            (user.IsDrafter && j.AssigneeId == user.UserId)
            || (user.IsRequester && j.Request != null && j.Request.RequesterId == user.UserId));
    }

    public static IQueryable<Draft> VisibleTo(this IQueryable<Draft> query, CurrentUser user) =>
        query.Where(d =>
            (user.IsDrafter && d.DrafterId == user.UserId)
            || (user.IsRequester && d.Job.Request != null && d.Job.Request.RequesterId == user.UserId));

    // A reply inherits access from the root note it answers (parents attached to a request/job/draft).
    public static IQueryable<Note> VisibleTo(this IQueryable<Note> query, CurrentUser user) =>
        query.Where(n =>
            (n.Request != null && (user.IsFrontOffice
                || (user.IsRequester && n.Request.RequesterId == user.UserId)
                || (user.IsDrafter && n.Request.Job != null && n.Request.Job.AssigneeId == user.UserId)))
            || (n.Job != null && (user.IsFrontOffice
                || (user.IsDrafter && n.Job.AssigneeId == user.UserId)
                || (user.IsRequester && n.Job.Request != null && n.Job.Request.RequesterId == user.UserId)))
            || (n.Draft != null && (
                (user.IsDrafter && n.Draft.DrafterId == user.UserId)
                || (user.IsRequester && n.Draft.Job.Request != null && n.Draft.Job.Request.RequesterId == user.UserId)))
            || (n.ParentNote != null && (
                (n.ParentNote.Request != null && (user.IsFrontOffice
                    || (user.IsRequester && n.ParentNote.Request.RequesterId == user.UserId)
                    || (user.IsDrafter && n.ParentNote.Request.Job != null && n.ParentNote.Request.Job.AssigneeId == user.UserId)))
                || (n.ParentNote.Job != null && (user.IsFrontOffice
                    || (user.IsDrafter && n.ParentNote.Job.AssigneeId == user.UserId)
                    || (user.IsRequester && n.ParentNote.Job.Request != null && n.ParentNote.Job.Request.RequesterId == user.UserId)))
                || (n.ParentNote.Draft != null && (
                    (user.IsDrafter && n.ParentNote.Draft.DrafterId == user.UserId)
                    || (user.IsRequester && n.ParentNote.Draft.Job.Request != null && n.ParentNote.Draft.Job.Request.RequesterId == user.UserId))))));

    public static IQueryable<Attachment> VisibleTo(this IQueryable<Attachment> query, CurrentUser user) =>
        query.Where(a =>
            (a.Request != null && (user.IsFrontOffice
                || (user.IsRequester && a.Request.RequesterId == user.UserId)
                || (user.IsDrafter && a.Request.Job != null && a.Request.Job.AssigneeId == user.UserId)))
            || (a.Draft != null && (
                (user.IsDrafter && a.Draft.DrafterId == user.UserId)
                || (user.IsRequester && a.Draft.Job.Request != null && a.Draft.Job.Request.RequesterId == user.UserId))));
}
