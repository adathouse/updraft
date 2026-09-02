# Updraft Use Cases

This document defines the required workflows and permissions for the Updraft API. `DATA.md` defines the associated data contract. Differences between these requirements and the current implementation are listed under Implementation Status; they do not redefine the requirements.

## Roles

- **Requester**: Staff in a Member Office or Committee who submits and manages requests for legislative drafts.
- **Drafter**: A drafting attorney who works on assigned Jobs, creates Drafts, and communicates with Requesters about revisions.
- **FrontOffice**: Drafting-office staff who triage Requests, assign Jobs, respond to inquiries, and monitor work.

## Shared Preconditions

- The caller has a valid JWT containing at least one recognized role.
- The authenticated identity has a registered User record. A caller registers through `registerCurrentUser` when no record exists.
- The caller must be able to access a resource before accessing its Notes or Attachments.
- A client-supplied resource ID does not grant access.
- The same permissions apply through top-level queries, relationship fields, and global node lookups.

## Workflow Terms

- A new Request has status `Unassigned`.
- An **unassigned Request** has no Job.
- Creating a Job associates it with one Request and changes the Request status to `Assigned`.
- An **open Job** has status `Open`.
- Request statuses are `Unassigned`, `Assigned`, and `Closed`.
- Job statuses are `Open` and `Closed`.
- Submitting a Draft includes creating its record and uploading at least one document associated with that Draft.

## Permissions

| Resource or action | Requester | Drafter | FrontOffice |
| --- | --- | --- | --- |
| View Requests | Own Requests | Requests whose Job is assigned to them | All Requests |
| Submit a Request | Yes, for an authorized Office | No | No |
| Update a Request | Own Requests | No | No |
| View Jobs | Jobs for own Requests | Assigned Jobs | All Jobs |
| Create a Job | No | No | Yes |
| Update a Job | No | Assigned Jobs | All Jobs |
| View Drafts | Drafts under own Requests | Drafts they created | No |
| Submit a Draft | No | For an assigned open Job | No |
| View Request Attachments | Inherits Request access | Inherits Request access | Inherits Request access |
| View Draft Attachments | Inherits Draft access | Inherits Draft access | No |
| Add a Request Attachment | To own Requests | No | No |
| Add a Draft Attachment | No | To Drafts they created | No |
| Add a Note | To an accessible Request, Job, or Draft | To an accessible Request, Job, or Draft | To an accessible Request or Job |
| Reply to a Note | When the root resource is accessible | When the root resource is accessible | When the root resource is accessible |

Notes and Attachments inherit visibility from their root parent resource. A reply inherits visibility from the Request, Job, or Draft at the root of its Note thread.

## Request Workflows

### Submit a Request

- **Allowed role**: Requester.
- **Preconditions**: The Requester is authorized to act for the selected Office. Referenced Tags and proposed Committee Offices exist.
- **Workflow**:
	1. Complete the intake questionnaire and identify the requesting Office.
	2. Optionally select proposed Committees and Tags.
	3. Submit the Request.
	4. Optionally create Request Attachment metadata and upload each document.
- **Outcome**: The Request is owned by the current User and has status `Unassigned`. Uploaded files are stored in BLOB storage and associated with the Request.
- **Visibility**: The Requester and FrontOffice can view it. A Drafter can view it after its Job is assigned to that Drafter.
- **API mapping**: `submitRequest`, followed by `addAttachment` and the REST attachment upload when files are included.
- **Status**: Partially implemented. Office authority is not currently modeled or enforced.

### Browse and View Requests

- **Allowed roles**: Requester, Drafter, and FrontOffice according to the permissions matrix.
- **Preconditions**: None beyond the shared authentication and registration requirements.
- **Workflow**: List visible Requests, apply supported filters and sorting, then open a visible Request.
- **Outcome**: Only Requests visible to the caller are returned. Related Job, Attachments, Notes, Committees, and Tags remain subject to their own access rules.
- **API mapping**: `requests`. Unassigned Requests use the generic connection filtered to Requests with no Job or status `Unassigned`; there is no dedicated unassigned-Requests field.
- **Status**: Partially implemented. Requester and FrontOffice top-level visibility is enforced, but Drafters cannot use the top-level `requests` query. Some Office and Tag relationship paths do not apply equivalent Request visibility.

### Update a Request

- **Allowed role**: Requester.
- **Preconditions**: The caller owns the Request.
- **Workflow**: Update intake details or the workflow status, then optionally add and upload Request Attachments.
- **Outcome**: The Request is updated and its `change_id` changes.
- **Visibility**: Updating the Request does not change its row-level visibility rules.
- **API mapping**: `updateRequest`, optionally followed by `addAttachment` and the REST attachment upload.
- **Status**: Implemented for owner updates.

## Job Workflows

### Create a Job

- **Allowed role**: FrontOffice.
- **Preconditions**: The Request exists, is unassigned, has no Job, and the selected assignee is a Drafter.
- **Workflow**:
	1. Browse unassigned Requests.
	2. Select a Request and Drafter.
	3. Enter the Job description.
	4. Create the Job.
- **Outcome**: An open Job is associated with the Request, and the Request status becomes `Assigned`.
- **Visibility**: FrontOffice can view the Job, the assigned Drafter can view it, and the Requester who owns the Request can view it.
- **API mapping**: `createJob`.
- **Status**: Partially implemented. The assignee must exist, but the Drafter role is not currently validated.

### Browse and View Jobs

- **Allowed roles**: Requester, Drafter, and FrontOffice according to the permissions matrix.
- **Preconditions**: None beyond the shared authentication and registration requirements.
- **Workflow**: List visible Jobs, filter or sort them, and open a visible Job with its Request and Notes.
- **Outcome**: The caller sees Job status and workflow information. Draft details and Draft Attachments are returned only when the caller also has Draft access.
- **API mapping**: `jobs`. Open Jobs use the generic connection filtered by status `Open`; Drafter visibility automatically limits results to assigned Jobs.
- **Status**: Implemented for top-level Job visibility.

### Update a Job

- **Allowed roles**: Drafter and FrontOffice.
- **Preconditions**: A Drafter is assigned to the Job. FrontOffice can update any Job. A new assignee must be a Drafter.
- **Workflow**: Change the description, status, or assignee and save the Job.
- **Outcome**: The Job is updated and its `change_id` changes.
- **Visibility**: Reassignment changes which Drafter can view and update the Job. Requester and FrontOffice visibility is unchanged.
- **API mapping**: `updateJob`.
- **Status**: Partially implemented. Ownership checks are implemented, but a new assignee's Drafter role is not currently validated.

## Draft Workflows

### Submit a Draft

- **Allowed role**: Drafter.
- **Preconditions**: The Job exists, is open, and is assigned to the current Drafter.
- **Workflow**:
	1. Create a Draft with a comment for the open Job.
	2. Create Attachment metadata linked to that Draft.
	3. Upload at least one document for the Attachment.
- **Outcome**: The Draft records the current Drafter and has at least one document stored in BLOB storage.
- **Visibility**: The submitting Drafter and the Requester who owns the parent Request can view it. FrontOffice cannot view Draft details.
- **API mapping**: `submitDraft`, followed by `addAttachment`, then `POST /attachments/{attachmentId}/{fileName}`.
- **Status**: Partially implemented. Draft creation and upload are separate, non-atomic operations, and the schema permits a Draft without an Attachment.

### Browse and View Drafts

- **Allowed roles**: Requester and Drafter according to the permissions matrix.
- **Preconditions**: The Draft must be visible through ownership of the parent Request or Draft authorship.
- **Workflow**: List visible Drafts and open a Draft with its comment, Attachments, and Notes.
- **Outcome**: Only visible Drafts and their related resources are returned.
- **API mapping**: `drafts` and Draft relationship fields.
- **Status**: Implemented for top-level visibility. Relationship and node paths must be covered by equivalent authorization tests.

## Note Workflows

### Add a Note

- **Allowed roles**: Any role with access to the selected parent resource.
- **Preconditions**: Exactly one Request, Job, or Draft parent is selected, and the caller can access it.
- **Workflow**: Enter text and save the Note against the selected parent.
- **Outcome**: The Note records the current User as owner and is attached to exactly one resource.
- **Visibility**: The Note inherits visibility from its parent. FrontOffice cannot add a Note directly to a Draft because FrontOffice cannot access Draft details.
- **API mapping**: `addNote`.
- **Status**: Implemented with parent-access checks.

### Reply to a Note

- **Allowed roles**: Any role that can access the root resource of the Note thread.
- **Preconditions**: The parent Note exists and is visible to the caller.
- **Workflow**: Enter reply text and save it against the parent Note.
- **Outcome**: The reply records the current User as owner and is attached to the parent Note.
- **Visibility**: The reply inherits access from the root Request, Job, or Draft.
- **API mapping**: `replyToNote`.
- **Status**: Partially implemented. Replies to root Notes are supported, but authorization does not traverse an arbitrarily deep reply chain.

### Browse Notes and Replies

- **Allowed roles**: Any role that can access the root resource.
- **Preconditions**: Exactly one Request, Job, Draft, or parent Note filter is supplied.
- **Workflow**: List Notes for a visible resource and traverse replies as a thread.
- **Outcome**: Only Notes visible through the root resource are returned.
- **API mapping**: `notes` and Note reply relationship fields.
- **Status**: Partially implemented. Root Notes and direct replies are supported, but visibility resolution does not traverse an arbitrarily deep reply chain.

## Attachment Workflows

### Add and Upload an Attachment

- **Allowed roles**: A Requester adding a file to an owned Request, or a Drafter adding a file to a Draft they created.
- **Preconditions**: Exactly one Request or Draft parent is selected and visible to the caller. The Attachment role is `Draft`, `PriorLegislation`, `PolicyPaper`, or `IntakeSupport`.
- **Workflow**:
	1. Create Attachment metadata for the parent resource.
	2. Upload the file bytes using the returned Attachment ID.
- **Outcome**: Metadata is stored in PostgreSQL, file bytes are stored through Foundatio, and the Attachment remains associated with exactly one parent.
- **Visibility**: The Attachment inherits visibility from its Request or Draft.
- **API mapping**: `addAttachment`, followed by `POST /attachments/{attachmentId}/{fileName}`.
- **Status**: Partially implemented. The API currently permits any known role with parent access to create metadata or upload bytes, and no attachment retrieval endpoint exists.

### Browse Attachments

- **Allowed roles**: Any role that can access the parent Request or Draft.
- **Preconditions**: A Request or Draft parent filter is supplied.
- **Workflow**: List visible Attachment metadata for the selected parent.
- **Outcome**: Only Attachment metadata visible through the parent is returned.
- **API mapping**: `attachments` and parent relationship fields.
- **Status**: Metadata listing is implemented. File retrieval is not implemented.

## API Mapping

| Capability | API operation |
| --- | --- |
| Register the authenticated User | `registerCurrentUser` mutation |
| Submit or update a Request | `submitRequest`, `updateRequest` mutations |
| Browse Requests | `requests` query with paging, filtering, and sorting |
| Create or update a Job | `createJob`, `updateJob` mutations |
| Browse Jobs | `jobs` query with paging, filtering, and sorting |
| Submit a Draft | `submitDraft` mutation |
| Browse Drafts | `drafts` query with paging, filtering, and sorting |
| Add or reply to a Note | `addNote`, `replyToNote` mutations |
| Browse Notes | `notes` query with one parent filter |
| Create Attachment metadata | `addAttachment` mutation |
| Upload Attachment bytes | `POST /attachments/{attachmentId}/{fileName}` |
| Browse Attachment metadata | `attachments` query with a Request or Draft filter |
| Retrieve Attachment bytes | Not implemented |

## Implementation Status

| Requirement | Status | Current difference |
| --- | --- | --- |
| Valid JWT and registered User | Implemented | `registerCurrentUser` creates or returns the User record. |
| Top-level row visibility | Partially implemented | Resource filters exist, but the `requests` query policy excludes Drafters who should see assigned Requests. |
| Equivalent visibility through every relationship and node path | Partially implemented | Some Office and Tag Request relationship paths do not apply Request visibility. |
| Requester authority for the selected Office | Not implemented | No User-to-Office authority relationship is modeled. |
| Job assignee must be a Drafter | Partially implemented | The assignee must exist, but role membership is not validated. |
| Draft submission includes at least one uploaded document | Partially implemented | Draft creation and file upload are separate; no database constraint requires an Attachment. |
| Attachment modification follows workflow roles | Partially implemented | Any known role with parent visibility can currently add or upload an Attachment. |
| Arbitrarily nested Note replies inherit root-resource access | Partially implemented | Visibility handles direct replies but does not recursively resolve deeper reply ancestry. |
| Attachment byte retrieval | Not implemented | Only metadata queries and byte upload are available. |

## Out of Scope

- Notifying a Requester when a Draft is submitted.

