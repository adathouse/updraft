# Updraft Data Contract

This document describes the Updraft domain and its persisted PostgreSQL schema. The Flyway migrations are the source of truth.

## Domain Invariants

- A Request belongs to one Office and one Requester.
- A Request can have zero or one Job. 
- A Job has one assignee and can have many Drafts.
- A Draft belongs to one Job and records the User who created it.
- An Attachment belongs to exactly one Request or Draft.
- A Note belongs to exactly one Request, Job, Draft, or parent Note.
- Requests and Drafts can have many Attachments and Notes. Jobs can have many Notes.
- Requests can be associated with many proposed Committee Offices and many Tags through join tables.

## Schema Conventions

### Ownership

- Flyway owns all schema, constraint, index, and reference-data migrations.
- EF Core maps the schema and provides data access. Do not create or apply EF Core migrations.
- Keep this document, Flyway SQL, and `Data/UpdraftDbContext.cs` aligned.

### Naming and Keys

- PostgreSQL tables and columns use `snake_case`.
- C# properties and GraphQL fields use `PascalCase` through EF Core and Hot Chocolate naming conventions.
- Root entities use `uuid` primary keys. Lookup tables, such as `tags` may use human readable text keys.
- `request_committees` and `request_tags` use composite primary keys.

### Change Tracking

- Data entities have a non-null `change_id uuid` to track updates to a record. A change tracking table will be implemented later. 
- `change_id` defaults to `gen_random_uuid()` when a row is inserted.
- EF Core refreshes `change_id` when an `IChangeTracked` entity is added or modified. SQL executed outside EF Core must update it explicitly when required.
- `tags`, `offices` and other lookup tables do not have change tracking.
- Join tables such as `request_committees`, and `request_tags` do not have `change_id` columns.
- `change_id` supports change detection. It is not a full audit history.

### Multi-Parent Associations

Attachments and Notes use separate nullable foreign keys for each supported parent type. A check constraint requires exactly one parent foreign key to be non-null. Do not replace these columns with a generic parent type and parent ID pair.

### Sample Queries

Get requests with the user information
```
SELECT request_id, proposal, requester_id, name, entra_id 
  FROM requests 
  LEFT JOIN users ON (user_id = requester_id);
```



## Entity Reference

### Office

An Office is a Member office, Committee, Caucus, or other organizational entry that can own or be associated with a Request.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `office_id` | `uuid` | No | Primary key. |
| `name` | `text` | No | Common display name. |
| `formal_name` | `text` | No | Full formal name. |
| `directory` | `text` | No | HOLC directory value. |
| `office_type` | `text` | No | Office classification. |
| `id_code` | `text` | Yes | External identifier such as a Bioguide or Committee code. |

Relationships:

- One Office can own many Requests through `requests.office_id`.
- One Office can be associated with many Requests through `request_committees.office_id`.
- Deleting an Office is restricted while either relationship exists.

### User

A User is the application record associated with an authenticated identity. Users can own Requests and Notes, be assigned Jobs, and create Drafts.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `user_id` | `uuid` | No | Primary key. |
| `entra_id` | `text` | No | Unique stable identifier resolved from the authenticated principal. |
| `name` | `text` | No | Display name. |
| `email` | `text` | No | Email address. |
| `roles` | `text` | No | Persisted role value or values recorded for the user. |
| `change_id` | `uuid` | No | Change-detection token. |

Constraints and relationships:

- `entra_id` is unique.
- Deleting a User is restricted while the User owns a Request or Note, is assigned a Job, or created a Draft.
- Runtime authorization uses validated JWT role claims. The persisted `roles` value is not the authorization source for the current request.

### Tag

A Tag is an authority or lookup value used to classify Requests, such as a Library of Congress policy area or legislative subject term.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `tag_id` | `text` | No | Primary key. |
| `label` | `text` | No | Display label. |
| `category` | `text` | No | Tag authority or category. |
| `change_id` | `uuid` | No | Change-detection token. |

A Tag can be associated with many Requests through `request_tags`. Deleting a Tag is restricted while those associations exist.

### Request

A Request captures an Office's request for a legislative draft and its intake questionnaire responses.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `request_id` | `uuid` | No | Primary key. |
| `office_id` | `uuid` | No | Foreign key to the requesting Office. |
| `requester_id` | `uuid` | No | Foreign key to the User who owns the Request. |
| `proposal` | `text` | Yes | Proposed policy or drafting request. |
| `amending_bill` | `text` | Yes | Bill to amend, when applicable. |
| `reintroducing_bill` | `text` | Yes | Bill to reintroduce, when applicable. |
| `related_agencies` | `text` | Yes | Related agencies. |
| `related_law` | `text` | Yes | Related law. |
| `scope_response` | `text` | No | Scope of the proposed policy. |
| `administration_response` | `text` | No | Responsible administering organization. |
| `enforcement_response` | `text` | No | Consequences when the policy is not followed. |
| `timing_response` | `text` | No | Intended effective timing. |
| `existing_law_response` | `text` | No | Relationship to existing law. |
| `status` | `text` | No | Workflow status: `Unassigned`, `Assigned`, or `Closed`. |
| `change_id` | `uuid` | No | Change-detection token. |

Relationships:

- `office_id` uses `ON DELETE RESTRICT`.
- `requester_id` uses `ON DELETE RESTRICT`.
- A Request can have zero or one Job because `jobs.request_id` is unique when present.
- A Request can have many Attachments and Notes.
- Proposed Committees and Tags are represented by `request_committees` and `request_tags`, not columns on `requests`.

Indexes exist on `office_id` and `status`.

### Job

A Job is a unit of drafting work assigned to a User and optionally linked to one Request.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `job_id` | `uuid` | No | Primary key. |
| `request_id` | `uuid` | Yes | Foreign key to Request, unique when present. |
| `assignee_id` | `uuid` | No | Foreign key to the assigned User. |
| `description` | `text` | No | Description of the drafting work. |
| `status` | `text` | No | Workflow status: `Open` or `Closed`. |
| `change_id` | `uuid` | No | Change-detection token. |

Relationships:

- Deleting the linked Request sets `request_id` to null.
- Deleting the assignee is restricted while the Job exists.
- A Job can have many Drafts and Notes.

Indexes exist on `assignee_id` and `status`.

### Draft

A Draft is a version of legislative text created for a Job by a User.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `draft_id` | `uuid` | No | Primary key. |
| `job_id` | `uuid` | No | Foreign key to Job. |
| `drafter_id` | `uuid` | No | Foreign key to the User who created the Draft. |
| `comment` | `text` | No | Comment accompanying the Draft. |
| `change_id` | `uuid` | No | Change-detection token. |

Relationships:

- Deleting the Job cascades to its Drafts.
- Deleting the Drafter is restricted while the Draft exists.
- A Draft can have many Attachments and Notes.

An index exists on `job_id`.

### Attachment

An Attachment stores metadata for one BLOB in Foundatio storage. File content is not stored in PostgreSQL.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `attachment_id` | `uuid` | No | Primary key. |
| `request_id` | `uuid` | Yes | Foreign key to Request. |
| `draft_id` | `uuid` | Yes | Foreign key to Draft. |
| `storage_key` | `text` | No | Foundatio storage key. |
| `attachment_uri` | `text` | No | Application URI used to request the content. |
| `attachment_role` | `text` | No | Role: `Draft`, `PriorLegislation`, `PolicyPaper`, or `IntakeSupport`. |
| `change_id` | `uuid` | No | Change-detection token. |

Constraints and relationships:

- `ck_attachments_single_parent` requires exactly one of `request_id` and `draft_id` to be non-null.
- `ck_attachments_role` restricts `attachment_role` to the listed values.
- The foreign keys use PostgreSQL's default `NO ACTION` delete behavior.

Indexes exist on `request_id` and `draft_id`.

### Note

A Note is text attached to a Request, Job, Draft, or parent Note. A Note whose parent is another Note is a reply.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `note_id` | `uuid` | No | Primary key. |
| `text` | `text` | No | Note body. |
| `owner_id` | `uuid` | Yes | Foreign key to the User who authored the Note. |
| `request_id` | `uuid` | Yes | Foreign key to Request. |
| `job_id` | `uuid` | Yes | Foreign key to Job. |
| `draft_id` | `uuid` | Yes | Foreign key to Draft. |
| `parent_note_id` | `uuid` | Yes | Foreign key to the parent Note for a reply. |
| `change_id` | `uuid` | No | Change-detection token. |

Constraints and relationships:

- `ck_notes_single_parent` requires exactly one of `request_id`, `job_id`, `draft_id`, and `parent_note_id` to be non-null.
- Deleting a Request, Job, Draft, or parent Note cascades to directly attached Notes.
- Deleting the owner is restricted while the Note exists.

Indexes exist on `request_id`, `job_id`, `draft_id`, and `parent_note_id`.

### Request Committee

`request_committees` associates Requests with proposed Committee Offices.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `request_id` | `uuid` | No | Foreign key to Request and part of the composite primary key. |
| `office_id` | `uuid` | No | Foreign key to Office and part of the composite primary key. |

Deleting a Request cascades to its associations. Deleting an Office is restricted while an association exists. An index exists on `office_id`.

### Request Tag

`request_tags` associates Requests with Tags.

| Column | Type | Null | Description |
| --- | --- | --- | --- |
| `request_id` | `uuid` | No | Foreign key to Request and part of the composite primary key. |
| `tag_id` | `text` | No | Foreign key to Tag and part of the composite primary key. |

Deleting a Request cascades to its associations. Deleting a Tag is restricted while an association exists. An index exists on `tag_id`.

## Enforcement Boundaries

The schema enforces structural integrity, nullability, foreign keys, parent cardinality, Request and Job statuses, Attachment roles, and one Job per Request. The following rules are enforced by application workflows or remain domain expectations rather than database constraints:

| Rule | Enforcement |
| --- | --- |
| A Job assignee has the Drafter role. | Application policy; `users.roles` is not constrained or joined by the database. |
| A Draft includes at least one document. | Workflow expectation; the schema permits a Draft without an Attachment. |
| A proposed Committee association references an Office whose `office_type` is `Committee`. | Application validation; the foreign key accepts any Office. |
| `office_type` uses a supported application value. | Application mapping; the schema accepts any non-null text. |
| A User's role text uses a supported role value or representation. | Authentication and application concern; the schema accepts any non-null text. |
-- change_id: GUID, updated on every row change
