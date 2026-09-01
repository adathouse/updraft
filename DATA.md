# Data modeling notes for Updraft

This document contains notes about the data model and a description of the purpose of specific types and values.

## Data conventions

These rules apply to every entity and schema migration.

- **Primary keys** — All primary keys are `GUID` (uuid) except `Tags.tag_id`, which is `TEXT`.
- **Change tracking** — Every table has a `change_id GUID` column that is updated whenever the row changes. A full audit table is out of scope for now and will be added later.
- **Schema ownership** — flyway owns all DDL and schema migrations. EFCore is an ORM only; EF Core migrations are not used.
- **Polymorphic associations** — Where an entity can attach to more than one kind of parent, use a separate nullable FK column per possible parent rather than a single polymorphic column. Exactly one FK is non-null, and the non-null column identifies the parent type.
- **Naming** — Field names in this document are written as database column names (snake_case). In C#/GraphQL these map to PascalCase via EFCore column configuration and Hot Chocolate default naming.

## Data types

The following types will be needed to support the use cases.

All changes are tracked with a `change_id` that is updated whenever the row is updated. 
This allows consumers to see changes and internal services to detect changes for updates. 
Requests are created and managed by Member Office or Committee.
Jobs are assigned by Front Office staff and tracked and managed by Front Office staff and Drafters.
Drafts are created and managed by Drafters based on a Request from a Member or Committee.

### Request
- Description: A request for a new draft from an Office, including responses to the intake questionaire.
- Fields:
-- request_id: PK
-- office_id: FK to Office
-- proposal: TEXT
-- amending_bill: TEXT
-- reintroducing_bill: TEXT
-- related_agencies: TEXT
-- related_law: TEXT
-- proposed_committees: FK to Committee entries in Office, multiple
-- tags: FK to Tags, multiple
-- scope_response: TEXT - reponse to "What is the scope of the policy—To whom or what does it apply?"
-- administration_response: TEXT - response to "Questions of administration—Who will be responsible for carrying out the policy?"
-- enforcement_response: TEXT - response to "Questions of enforcement—What if the policy is not followed?"
-- timing_response: TEXT - response to "Questions of timing-when should the policy take effect?"
-- existing_law_response: TEXT - response to "What is the relation between the policy and existing law—Must existing law be amended to avoid conflicts with the policy?"
-- status: request workflow state, e.g. Unassigned, Assigned
-- attachments: Attachments with attached_to = this request
-- change_id: GUID, updated on every row change

### Tags
- Description: A lookup/authotity table that provides tags or annotations that can be associated with anything. E.g. Library of Congress Policy Areas - https://www.congress.gov/advanced-search/subject-policy-area or Legislative Subject Terms
- Fields:
-- tag_id: TEXT, PK
-- label: TEXT
-- category: TEXT 


### Office
- Description: An Office or related entity that can request legislation. Office represents both Member Offices and Committees. Requests can be made by both types of Offices. Office is also used for the list of Committees when Committees alone are needed. Office may be extended for Executive Agencies at some point.
- Fields:
-- office_id: PK
-- office_name: text
-- office_graph: text - key for filenames
-- office_type: Member, Committee or Caucus
-- bioguide: text - bioguide id
-- commcode: text - Committee code
-- change_id: GUID, updated on every row change

### Job
- Description: A unit of work for a Drafter that references the request and includes one or more drafts.
- Fields:
-- job_id: GUID, PK
-- request_id: FK to request, nullable
-- assignee: GUID referencing user with the DRAFTER role
-- description: text 
-- status: job workflow state, e.g. Open, Closed
-- change_id: GUID, updated on every row change


### Draft
- Description: A draft created by a Drafter, including a comment and at least one draft document. Notes may be attached to a draft.
- Fields:
-- draft_id: GUID, PK
-- job_id: FK to Job
-- comment: TEXT
-- attachments: Attachments with attached_to = this draft
-- change_id: GUID, updated on every row change

### Attachment
- Description: A reference to a single BLOB (stored in Foundatio) associated with a request or a draft. Holds only a pointer to the file, not its contents.
- Fields:
-- attachment_id: GUID, PK
-- request_id: FK to Request, nullable
-- draft_id: FK to Draft, nullable
-- storage_key: TEXT - Foundatio storage key for the BLOB
-- attachment_role: the purpose of the attachment, e.g. draft, prior legislation, or policy paper
-- change_id: GUID, updated on every row change
- Note: exactly one of request_id or draft_id is non-null; the non-null column identifies the parent.

### Note
- Description: free text attached to a draft, request or job. Or, a reply to another note.
- Fields:
-- note_id: GUID, PK
-- text: TEXT - the note body
-- request_id: FK to Request, nullable
-- job_id: FK to Job, nullable
-- draft_id: FK to Draft, nullable
-- parent_note_id: FK to Note, nullable (set when this note is a reply)
-- change_id: GUID, updated on every row change
- Note: exactly one of request_id, job_id, draft_id or parent_note_id is non-null; the non-null column identifies the parent.

### User
- Description: various system users, distinguished by role, associated with an identity in the Entra tenant (i.e. authenitcated). Users are a proxy for an Entra identity and a foreign key reference for data that is owned by or controlled by a specific user, including Request, Job, Draft and Note. Authenication claims for each user will determine roles. 
- Fields:
-- user_id: PK
-- entra_id: reference to the Entra entry, probably the sid
-- name: common name for the person
-- email: email address for responses
-- roles: TEXT list of roles. Maybe pull this from the JWT claims
-- change_id: GUID, updated on every row change
