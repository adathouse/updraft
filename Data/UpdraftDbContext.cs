using Microsoft.EntityFrameworkCore;
using Updraft.Data.Entities;
using EntityTag = Updraft.Data.Entities.Tag;

namespace Updraft.Data;

public sealed class UpdraftDbContext(DbContextOptions<UpdraftDbContext> options) : DbContext(options)
{
    private const string DefaultSchema = "updraft";

    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Draft> Drafts => Set<Draft>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestCommittee> RequestCommittees => Set<RequestCommittee>();
    public DbSet<RequestTag> RequestTags => Set<RequestTag>();
    public DbSet<EntityTag> Tags => Set<EntityTag>();
    public DbSet<User> Users => Set<User>();

    public override int SaveChanges()
    {
        ApplyChangeTracking();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyChangeTracking();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("attachments");
            entity.ToTable(t => t.HasCheckConstraint("ck_attachments_single_parent",
                "(CASE WHEN request_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN draft_id IS NOT NULL THEN 1 ELSE 0 END) = 1"));
            entity.HasKey(x => x.AttachmentId);
            entity.Property(x => x.AttachmentId).HasColumnName("attachment_id");
            entity.Property(x => x.RequestId).HasColumnName("request_id");
            entity.Property(x => x.DraftId).HasColumnName("draft_id");
            entity.Property(x => x.StorageKey).HasColumnName("storage_key").IsRequired();
            entity.Property(x => x.AttachmentRole).HasColumnName("attachment_role").HasConversion<string>().IsRequired();
            entity.Property(x => x.ChangeId).HasColumnName("change_id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(x => x.Request)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Draft)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.DraftId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => x.RequestId).HasDatabaseName("idx_attachments_request_id");
            entity.HasIndex(x => x.DraftId).HasDatabaseName("idx_attachments_draft_id");
        });

        modelBuilder.Entity<Draft>(entity =>
        {
            entity.ToTable("drafts");
            entity.HasKey(x => x.DraftId);
            entity.Property(x => x.DraftId).HasColumnName("draft_id");
            entity.Property(x => x.JobId).HasColumnName("job_id");
            entity.Property(x => x.Comment).HasColumnName("comment").IsRequired();
            entity.Property(x => x.ChangeId).HasColumnName("change_id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(x => x.Job)
                .WithMany(x => x.Drafts)
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.JobId).HasDatabaseName("idx_drafts_job_id");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("jobs");
            entity.HasKey(x => x.JobId);
            entity.Property(x => x.JobId).HasColumnName("job_id");
            entity.Property(x => x.RequestId).HasColumnName("request_id");
            entity.Property(x => x.AssigneeId).HasColumnName("assignee_id");
            entity.Property(x => x.Description).HasColumnName("description").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
            entity.Property(x => x.ChangeId).HasColumnName("change_id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("uq_jobs_request_id");
            entity.HasIndex(x => x.AssigneeId).HasDatabaseName("idx_jobs_assignee_id");
            entity.HasIndex(x => x.Status).HasDatabaseName("idx_jobs_status");
            entity.HasOne(x => x.Request)
                .WithOne(x => x.Job)
                .HasForeignKey<Job>(x => x.RequestId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Assignee)
                .WithMany(x => x.AssignedJobs)
                .HasForeignKey(x => x.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("notes");
            entity.HasKey(x => x.NoteId);
            entity.Property(x => x.NoteId).HasColumnName("note_id");
            entity.Property(x => x.Text).HasColumnName("text").IsRequired();
            entity.Property(x => x.RequestId).HasColumnName("request_id");
            entity.Property(x => x.JobId).HasColumnName("job_id");
            entity.Property(x => x.DraftId).HasColumnName("draft_id");
            entity.Property(x => x.ParentNoteId).HasColumnName("parent_note_id");
            entity.Property(x => x.ChangeId).HasColumnName("change_id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(x => x.Request)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Job)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Draft)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.DraftId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ParentNote)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentNoteId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_notes_single_parent",
                "(CASE WHEN request_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN job_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN draft_id IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN parent_note_id IS NOT NULL THEN 1 ELSE 0 END) = 1"));
            entity.HasIndex(x => x.RequestId).HasDatabaseName("idx_notes_request_id");
            entity.HasIndex(x => x.JobId).HasDatabaseName("idx_notes_job_id");
            entity.HasIndex(x => x.DraftId).HasDatabaseName("idx_notes_draft_id");
            entity.HasIndex(x => x.ParentNoteId).HasDatabaseName("idx_notes_parent_note_id");
        });

        modelBuilder.Entity<Office>(entity =>
        {
            entity.ToTable("offices");
            entity.HasKey(x => x.OfficeId);
            entity.Property(x => x.OfficeId).HasColumnName("office_id");
            entity.Property(x => x.Name).HasColumnName("name").IsRequired();
            entity.Property(x => x.FormalName).HasColumnName("formal_name").IsRequired();
            entity.Property(x => x.Directory).HasColumnName("directory").IsRequired();
            entity.Property(x => x.OfficeType).HasColumnName("office_type").HasConversion<string>().IsRequired();
            entity.Property(x => x.IdCode).HasColumnName("id_code");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.ToTable("requests");
            entity.HasKey(x => x.RequestId);
            entity.Property(x => x.RequestId).HasColumnName("request_id");
            entity.Property(x => x.OfficeId).HasColumnName("office_id");
            entity.Property(x => x.Proposal).HasColumnName("proposal");
            entity.Property(x => x.AmendingBill).HasColumnName("amending_bill");
            entity.Property(x => x.ReintroducingBill).HasColumnName("reintroducing_bill");
            entity.Property(x => x.RelatedAgencies).HasColumnName("related_agencies");
            entity.Property(x => x.RelatedLaw).HasColumnName("related_law");
            entity.Property(x => x.ScopeResponse).HasColumnName("scope_response").IsRequired();
            entity.Property(x => x.AdministrationResponse).HasColumnName("administration_response").IsRequired();
            entity.Property(x => x.EnforcementResponse).HasColumnName("enforcement_response").IsRequired();
            entity.Property(x => x.TimingResponse).HasColumnName("timing_response").IsRequired();
            entity.Property(x => x.ExistingLawResponse).HasColumnName("existing_law_response").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
            entity.Property(x => x.ChangeId).HasColumnName("change_id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasOne(x => x.Office)
                .WithMany(x => x.Requests)
                .HasForeignKey(x => x.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OfficeId).HasDatabaseName("idx_requests_office_id");
            entity.HasIndex(x => x.Status).HasDatabaseName("idx_requests_status");
        });

        modelBuilder.Entity<RequestCommittee>(entity =>
        {
            entity.ToTable("request_committees");
            entity.HasKey(x => new { x.RequestId, x.OfficeId });
            entity.Property(x => x.RequestId).HasColumnName("request_id");
            entity.Property(x => x.OfficeId).HasColumnName("office_id");
            entity.HasOne(x => x.Request)
                .WithMany(x => x.RequestCommittees)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Office)
                .WithMany(x => x.RequestCommittees)
                .HasForeignKey(x => x.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OfficeId).HasDatabaseName("idx_request_committees_office_id");
        });

        modelBuilder.Entity<RequestTag>(entity =>
        {
            entity.ToTable("request_tags");
            entity.HasKey(x => new { x.RequestId, x.TagId });
            entity.Property(x => x.RequestId).HasColumnName("request_id");
            entity.Property(x => x.TagId).HasColumnName("tag_id");
            entity.HasOne(x => x.Request)
                .WithMany(x => x.RequestTags)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Tag)
                .WithMany(x => x.RequestTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.TagId).HasDatabaseName("idx_request_tags_tag_id");
        });

        modelBuilder.Entity<EntityTag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(x => x.TagId);
            entity.Property(x => x.TagId).HasColumnName("tag_id");
            entity.Property(x => x.Label).HasColumnName("label").IsRequired();
            entity.Property(x => x.Category).HasColumnName("category").IsRequired();
            entity.Property(x => x.ChangeId).HasColumnName("change_id").HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.EntraId).HasColumnName("entra_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").IsRequired();
            entity.Property(x => x.Roles).HasColumnName("roles").IsRequired();
            entity.Property(x => x.ChangeId).HasColumnName("change_id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(x => x.EntraId).IsUnique().HasDatabaseName("uq_users_entra_id");
        });
    }

    private void ApplyChangeTracking()
    {
        var trackedEntries = ChangeTracker
            .Entries<IChangeTracked>()
            .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified);

        foreach (var entry in trackedEntries)
        {
            entry.Entity.ChangeId = Guid.NewGuid();
        }
    }
}