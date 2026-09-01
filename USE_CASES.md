# Use cases for the Updraft API

This documents outline use cases that the API needs to support.
Updraft is used to manage requests for legislative drafts. It enforces workflow and permissions to manage the work of delivering drafts to clients and ensuring that only the right people can see drafts and monitor requests.

## Roles

- Requester - staff in a Member Office or Committee who needs to have a new draft created. 
- FrontOffice - drafting office staff who manage requests, respond to client inquries and interface with drafters to track the status if a request.
- Drafter - a drafting attorney who creates drafts in response to requests. They manage many requests and drafts at one time and sometimes have extensive dialog with Member and Committee staff about a draft that may result in multiple drafts for one request and multiple inquires about a draft to resolve questions and create revisions.

## Use cases

The following use cases should be supported by the API.

### Request a draft
- Role: Requester
- Action: Requester submits a request for new draft legislation on behalf of an Office
- Steps:
-- Requester fills out an intake questionaire
-- Requester optionally attaches file to the request
-- the new request is stored in the the database and attachments are stored in the BLOB store for review by the Front Office
-- The status of the Request is "NEW"

### Update a Request
- Role: Requester
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
- Permissions:
-- Requesters can only see Requests they submitted. 
-- FrontOffice users can see any Request.

### Create a job
- Role: FrontOffice
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
- Permissions:
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
- Permissions:
-- Drafters can only see Drafts they created.
-- Requesters can only see Drafts for Requests they created. 
-- Only Drafters and Requesters can see Drafts.

### Submit a note to a request, draft or job
- Role: Requester, Drafter or FrontOffice
- Action: add a note to a request, draft or job
- Steps: 
-- Requester, Drafter or FrontOffice select "Add note" to a selected job, draft or request.
-- Requester, Drafter or FrontOffice enters text for the note.
-- Requester, Drafter or FrontOffice clicks "Save" and the note is saved attached to the selected job, draft or request.


### Reply to a note
- Role: Requester, Drafter or FrontOffice
- Action: reply to a note attached to a request, draft or job
- Steps: 
-- Requester, Drafter or FrontOffice select "Reply" to a selected note.
-- Requester, Drafter or FrontOffice enters text for the reply.
-- Requester, Drafter or FrontOffice clicks "Save" and the reply is saved attached to the selected note.

### List unassigned requests
- Role: FrontOffice
- Action: view all Requests that do not have a Job assigned to them.

### List open jobs
- Role: Drafter, FrontOffice
- Action: view jobs with an open status, filtered by assignee where appropriate.

### Update a Job
- Role: Drafter, FrontOffice
- Action: Update the assignee or status of a Job.
- Steps:
-- Select a job you own if you are a Drafter, or any Job if you are the Front Office.
-- Update the status or assignee.
-- Save the Job.

### View a job
- Role: Requester, Drafter, FrontOffice
- Action: view a single job with its request, drafts, attachments and notes.
- Steps:
-- A Drafter navigates to a list of Jobs assigned to them.
-- A FrontOffice staffer navigates to a list of all Jobs
-- A Requester only sees Job associated with a Request.
- Permissions:
-- Drafters only see Jobs assigned to them.
-- FronOffice staff can see all Jobs.
-- Requesters only see Jobs that are attached to their requests. 

### Browse notes and replies
- Role: Requester, Drafter, FrontOffice
- Action: view the notes attached to a request, job or draft, including threaded replies.
- Steps:
-- Users navigate to a detailed view for a Request, Draft or Job
-- Users can see notes attached to the object they are viewing.
- Permissions:
-- Access to a Note is controlled by access to the object it is attached to. If you can see the object details you can see the Notes attached to it. 

