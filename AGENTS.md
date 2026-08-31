# AGENTS.md

Instructions for AI agents working with this code base.

## Project

Updraft is a .NET 10 GraphQL API using Hot Chocolate 16.x. 
It uses EFCore to interface with a PostgreSQL database and Foundatio to interface with BLOB storage. 
It uses flyway to manage the database schema. 
Updraft provides an API for consumers to request and collaborate on draft legislation.

## Build & Verify

```bash
dotnet build
dotnet run
```

The project must build with zero warnings and zero errors before any change is considered complete.

## Architecture

- **Annotation-based types** — GraphQL types use Hot Chocolate attribute annotations (`[ObjectType<T>]`, `[QueryType]`) and static partial extension classes. Do NOT use the older `ObjectType<T>` base class pattern.
- **Source generator** — `HotChocolate.Types.Analyzers` generates the `.AddUpdraftTypes()` extension method. All annotated types are discovered automatically; do not manually register types in `Program.cs`.
- **Conventions** — The project uses Hot Chocolate query conventions: global object identification, cursor pagination (`[UsePaging]`), filtering (`[UseFiltering]`), and sorting (`[UseSorting]`). Apply these attributes to any new `IQueryable`-returning resolver.
- **Service layer** — All mutations should use service classes (e.g. `RequestService`) to handle inputs and enforce business logic. 
- **Repository layer** - Updraft uses EFCore to interface with a Postgres database. All database access should be routed through the repository layer. 
- **BLOB storage** - Updraft uses Foundatio to interface with BLOB storage and store all files. 
- **Database** - Updraft uses a Postgres database to store structured data. flyway manages ALL schema/DDL; EFCore is used strictly as an ORM (EF Core migrations are disabled — do not generate or apply them).
- **Authorization** - Access is role-based using Hot Chocolate `[Authorize]` policies, enforced on every query and mutation, on the `[NodeResolver]` entry points (so global object identification cannot bypass them), and on the REST attachment upload endpoint. Policies are defined in `Security/AuthorizationPolicies.cs`: `Requester`, `Drafter`, `FrontOffice`, `DrafterOrFrontOffice`, and `AnyKnownRole`. Roles come from the authenticated user's JWT `role` claim (`RoleClaimType = ClaimTypes.Role`); Entra validation is stubbed for non-dev environments (see `Program.cs`). Annotate any new mutation, resolver, or node resolver with the appropriate policy. For local development, mint tokens with `dotnet user-jwts` (see `README.md`); the token must be created before the service starts so the signing key/audience config is loaded.

## Naming

- Avoid the name `Node` by itself — it conflicts with Hot Chocolate's Relay `Node` interface and .NET types. 


## Style

- Use file-scoped namespaces.
- Use `IQueryable<T>` return types for any list resolver that should support pagination, filtering, or sorting.
- Keep resolvers in static partial extension classes annotated with `[ObjectType<T>]`.
- Register services in `Program.cs`; do not use `[Service]` attribute injection.

## Data conventions

These rules apply to every entity and schema migration.

- **Primary keys** — All primary keys are `GUID` (uuid) except `Tags.tag_id`, which is `TEXT`.
- **Change tracking** — Every table has a `change_id GUID` column that is updated whenever the row changes. A full audit table is out of scope for now and will be added later.
- **Schema ownership** — flyway owns all DDL and schema migrations. EFCore is an ORM only; EF Core migrations are not used.
- **Polymorphic associations** — Where an entity can attach to more than one kind of parent, use a separate nullable FK column per possible parent rather than a single polymorphic column. Exactly one FK is non-null, and the non-null column identifies the parent type.
- **Naming** — Field names in this document are written as database column names (snake_case). In C#/GraphQL these map to PascalCase via EFCore column configuration and Hot Chocolate default naming.

## Use cases

The following use cases should be supported by the API.

### Request a draft
- Role: Requester (staff in a Member or Committee Office)
- Action: Requester submits a request for new draft legislation on behalf of an Office
- Steps:
-- Requester fills out an intake questionaire
-- Requester optionally attaches file to the request
-- the new request is stored in the the database and attachments are stored in the BLOB store for review by the Front Office
-- The status of the Request is "NEW"

### Update a Request
- Role: Requester (staff in a Member or Committee Office)
- Action: Requester updates a draft to modify the description or update the status.
- Steps:
-- Requester updates the description or status.
-- Requester optionally attaches file to the request
-- The updated Request is stored. 

### View Requests
- Role: Requester or FrontOffice
- Action: Review a list of Requests
- Steps:
-- Open the list of Requests.
-- View basic information about all Requests you can see
-- Filter requests by status or time. See Requests with a new Job or Draft.
- Constraints:
-- Requesters can only see Requests they submitted. 
-- FrontOffice users can see any Request.

### Create a job
- Role: FrontOffice Staff 
- Action: Front Office Staff reviews new requests and creates jobs to assign a request to a Drafter.
- Steps:
-- FrontOffice reviews the list of Requests without Jobs.
-- FrontOffice selects an unassigned request and chooses "Create Job"
-- FrontOffice adds key information and comments to the job and selects a Drafter
-- FrontOffice saves the new job associated with the request. 

### View Jobs
- Role: FrontOffice or Drafter
- Action: View information on Jobs
- Steps:
-- Open the list of Jobs
-- View information on Job status, including when drafts were added, but no other Draft details.
-- Only Drafters can open a view with details about Drafts they created for a Job. 
- Constraints:
-- FrontOffice can see any Job.
-- Drafters can only see Jobs assigned to them.

### Submit a draft
- Role: Drafter 
- Action: Upload a new draft document, add comments and notify the requester. 
- Steps:
-- Drafter selects an open job and chooses "Send Draft"
-- Drafter attaches one or more documents to the job.
-- Drafter adds comments to the draft.
-- Drafter selects "Send draft" and the comments and documents are saved.
-- (Notifying the requester with a link to the new draft is out of scope for now.)

### View Drafts
- Role: Drafter
- Action: View information about Drafts.
- Steps:
-- Open the list of Drafts.
-- View details of a Draft. 
- Constraints
-- Drafters can only see Drafts they created.
-- Requesters can only see Drafts for Requests they created. 
-- Only Drafters and Requesters can see Drafts.

### Submit a note to a request, draft or job
- Role: Requester, Drafter or Front Office
- Action: add a note to a request, draft or job
- Steps: 
-- Requester, Drafter or Front Office select "Add note" to a selected job, draft or request.
-- Requester, Drafter or Front Office enters text for the note.
-- Requester, Drafter or Front Office clicks "Save" and the note is saved attached to the selected job, draft or request.


### Reply to a note
- Role: Requester, Drafter or Front Office
- Action: reply to a note attached to a request, draft or job
- Steps: 
-- Requester, Drafter or Front Office select "Reply" to a selected note.
-- Requester, Drafter or Front Office enters text for the reply.
-- Requester, Drafter or Front Office clicks "Save" and the reply is saved attached to the selected note.

### List unassigned requests
- Role: Front Office Staff
- Action: view all Requests that do not have a Job assigned to them.

### List open jobs
- Role: Drafter, Front Office
- Action: view jobs with an open status, filtered by assignee where appropriate.

### Update a Job
- Role: Drafter, Front Office
- Action: Update the assignee or status of a Job.
- Steps:
-- Select a job you own if you are a Drafter, or any Job if you are the Front Office.
-- Update the status or assignee.
-- Save the Job.

### View a job
- Role: Requester, Drafter, Front Office
- Action: view a single job with its request, drafts, attachments and notes.
- Steps:
-- A Drafter navigates to a list of Jobs assigned to them.
-- A FrontOffice staffer navigates to a list of all Jobs
-- A Requester only sees Job associated with a Request.
- Constraints:
-- Drafters only see Jobs assigned to them.
-- FronOffice staff can see all Jobs.
-- Requesters only see Jobs that are attached to their requests. 

### Browse notes and replies
- Role: Requester, Drafter, Front Office
- Action: view the notes attached to a request, job or draft, including threaded replies.
- Steps:
-- Users navigate to a detailed view for a Request, Draft or Job
-- Users can see notes attached to the object they are viewing.
- Constraints:
-- Access to a Note is controlled by access to the object it is attached to. If you can see the object details you can see the Notes attached to it. 

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
