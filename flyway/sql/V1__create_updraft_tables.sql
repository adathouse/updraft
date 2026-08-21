CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS offices (
    office_id uuid PRIMARY KEY,
    name text NOT NULL,
    formal_name text NOT NULL,
    directory text NOT NULL,
    office_type text NOT NULL,
    id_code text NULL
);

CREATE TABLE IF NOT EXISTS users (
    user_id uuid PRIMARY KEY,
    entra_id text NOT NULL,
    name text NOT NULL,
    email text NOT NULL,
    roles text NOT NULL,
    change_id uuid NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT uq_users_entra_id UNIQUE (entra_id)
);

CREATE TABLE IF NOT EXISTS tags (
    tag_id text PRIMARY KEY,
    label text NOT NULL,
    category text NOT NULL,
    change_id uuid NOT NULL DEFAULT gen_random_uuid()
);

CREATE TABLE IF NOT EXISTS requests (
    request_id uuid PRIMARY KEY,
    office_id uuid NOT NULL,
    proposal text NULL,
    amending_bill text NULL,
    reintroducing_bill text NULL,
    related_agencies text NULL,
    related_law text NULL,
    scope_response text NOT NULL,
    administration_response text NOT NULL,
    enforcement_response text NOT NULL,
    timing_response text NOT NULL,
    existing_law_response text NOT NULL,
    status text NOT NULL,
    change_id uuid NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT fk_requests_office FOREIGN KEY (office_id)
        REFERENCES offices (office_id)
        ON DELETE RESTRICT,
    CONSTRAINT ck_requests_status CHECK (status IN ('Unassigned', 'Assigned', 'Closed'))
);


CREATE TABLE IF NOT EXISTS jobs (
    job_id uuid PRIMARY KEY,
    request_id uuid NULL,
    assignee_id uuid NOT NULL,
    description text NOT NULL,
    status text NOT NULL,
    change_id uuid NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT fk_jobs_request FOREIGN KEY (request_id)
        REFERENCES requests (request_id)
        ON DELETE SET NULL,
    CONSTRAINT fk_jobs_assignee FOREIGN KEY (assignee_id)
        REFERENCES users (user_id)
        ON DELETE RESTRICT,
    CONSTRAINT ck_jobs_status CHECK (status IN ('Open', 'Closed')),
    CONSTRAINT uq_jobs_request_id UNIQUE (request_id)
);

CREATE TABLE IF NOT EXISTS drafts (
    draft_id uuid PRIMARY KEY,
    job_id uuid NOT NULL,
    comment text NOT NULL,
    change_id uuid NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT fk_drafts_job FOREIGN KEY (job_id)
        REFERENCES jobs (job_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS attachments (
    attachment_id uuid PRIMARY KEY,
    request_id uuid NULL,
    draft_id uuid NULL,
    storage_key text NOT NULL,
    attachment_uri text NOT NULL,
    attachment_role text NOT NULL,
    change_id uuid NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT fk_attachments_request FOREIGN KEY (request_id)
        REFERENCES requests (request_id),
    CONSTRAINT fk_attachments_draft FOREIGN KEY (draft_id)
        REFERENCES drafts (draft_id),
    CONSTRAINT ck_attachments_single_parent CHECK (
        (CASE WHEN request_id IS NOT NULL THEN 1 ELSE 0 END) +
        (CASE WHEN draft_id IS NOT NULL THEN 1 ELSE 0 END) = 1
    ),
    CONSTRAINT ck_attachments_role CHECK (attachment_role IN ('Draft', 'PriorLegislation', 'PolicyPaper', 'IntakeSupport'))
);

CREATE TABLE IF NOT EXISTS notes (
    note_id uuid PRIMARY KEY,
    text text NOT NULL,
    request_id uuid NULL,
    job_id uuid NULL,
    draft_id uuid NULL,
    parent_note_id uuid NULL,
    change_id uuid NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT fk_notes_request FOREIGN KEY (request_id)
        REFERENCES requests (request_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_notes_job FOREIGN KEY (job_id)
        REFERENCES jobs (job_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_notes_draft FOREIGN KEY (draft_id)
        REFERENCES drafts (draft_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_notes_parent_note FOREIGN KEY (parent_note_id)
        REFERENCES notes (note_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_notes_single_parent CHECK (
        (CASE WHEN request_id IS NOT NULL THEN 1 ELSE 0 END) +
        (CASE WHEN job_id IS NOT NULL THEN 1 ELSE 0 END) +
        (CASE WHEN draft_id IS NOT NULL THEN 1 ELSE 0 END) +
        (CASE WHEN parent_note_id IS NOT NULL THEN 1 ELSE 0 END) = 1
    )
);

CREATE TABLE IF NOT EXISTS request_committees (
    request_id uuid NOT NULL,
    office_id uuid NOT NULL,
    PRIMARY KEY (request_id, office_id),
    CONSTRAINT fk_request_committees_request FOREIGN KEY (request_id)
        REFERENCES requests (request_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_request_committees_committee FOREIGN KEY (office_id)
        REFERENCES offices (office_id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS request_tags (
    request_id uuid NOT NULL,
    tag_id text NOT NULL,
    PRIMARY KEY (request_id, tag_id),
    CONSTRAINT fk_request_tags_request FOREIGN KEY (request_id)
        REFERENCES requests (request_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_request_tags_tag FOREIGN KEY (tag_id)
        REFERENCES tags (tag_id)
        ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_requests_office_id ON requests (office_id);
CREATE INDEX IF NOT EXISTS idx_requests_status ON requests (status);
CREATE INDEX IF NOT EXISTS idx_jobs_assignee_id ON jobs (assignee_id);
CREATE INDEX IF NOT EXISTS idx_jobs_status ON jobs (status);
CREATE INDEX IF NOT EXISTS idx_drafts_job_id ON drafts (job_id);
CREATE INDEX IF NOT EXISTS idx_attachments_request_id ON attachments (request_id);
CREATE INDEX IF NOT EXISTS idx_attachments_draft_id ON attachments (draft_id);
CREATE INDEX IF NOT EXISTS idx_notes_request_id ON notes (request_id);
CREATE INDEX IF NOT EXISTS idx_notes_job_id ON notes (job_id);
CREATE INDEX IF NOT EXISTS idx_notes_draft_id ON notes (draft_id);
CREATE INDEX IF NOT EXISTS idx_notes_parent_note_id ON notes (parent_note_id);
CREATE INDEX IF NOT EXISTS idx_request_committees_office_id ON request_committees (office_id);
CREATE INDEX IF NOT EXISTS idx_request_tags_tag_id ON request_tags (tag_id);
