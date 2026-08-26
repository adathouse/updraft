# updraft

## Database bootstrap

Run the bootstrap SQL as a superuser to create the application role, database, schema, and grants.

```bash
psql -v ON_ERROR_STOP=1 -f db/bootstrap.sql
```

## Connection environment

```bash
export PGUSER=updraft
export PGPASSWORD=updraft
export PGDATABASE=updraft
export PGHOST=db
export PGPORT=5432
```

## Flyway migration

Apply schema migrations with Flyway.

```bash
cd flyway; flyway migrate
```

Check migration state.

```bash
cd flyway; flyway info
```

## create some dummy users

```
INSERT INTO updraft.users (user_id, entra_id, name, email, roles, change_id)
VALUES
('2aac9785-5915-44e1-b990-c1dc6198be3e'::UUID, 'a bogus entra id', 'Lamont Sanford', 'big@dummy.com', 'Member Staff', '1527469f-64d2-4813-892d-fd7bf1e02524'::UUID),
('68c4adf8-8d42-4849-a8d5-8afc289d3689'::UUID, 'another bougus entra id', 'Michael Stivic', 'meathead@dummy.com', 'HOLC Staff', 'f9524b14-e39e-48a2-9e8b-0e8fd470bbe0'::UUID)
;
```


## sample queries and mutations

The API is served at `http://localhost:5048/graphql/`. Mutations use Hot Chocolate
mutation conventions, so every mutation takes a single `input` argument and returns a
payload containing the entity plus an `errors` field. Replace the sample GUIDs with
values from your own data.

### Create a Request and attach a document

Submit the request:

```graphql
mutation SubmitRequest($input: SubmitRequestInput!) {
  submitRequest(input: $input) {
    request {
      requestId
      status
    }
    errors {
      __typename
      ... on Error {
        message
      }
    }
  }
}
```

```json
{
  "input": {
    "officeId": "9d9bc76b-cddd-49de-b326-ae5229e519c2",
    "proposal": "Regulate widget safety standards",
    "scopeResponse": "Applies to all interstate widget sales",
    "administrationResponse": "Department of Commerce",
    "enforcementResponse": "Civil penalties",
    "timingResponse": "Effective January 4, 2027",
    "existingLawResponse": "Amends the Widget Act of 1998",
    "committeeIds": [],
    "tagIds": []
  }
}
```

Create the attachment record for the returned `requestId` (use role `INTAKE_SUPPORT`,
`PRIOR_LEGISLATION`, `POLICY_PAPER`, or `DRAFT`):

```graphql
mutation AddRequestAttachment($input: AddAttachmentInput!) {
  addAttachment(input: $input) {
    attachment {
      attachmentId
      attachmentUri
      attachmentRole
      storageKey
    }
    errors {
      __typename
      ... on Error {
        message
      }
    }
  }
}
```

```json
{
  "input": {
    "role": "INTAKE_SUPPORT",
    "requestId": "0da34bee-9895-4a5d-9218-ea6f1d4dcd95"
  }
}
```

Upload the document bytes for the returned `attachmentId`. This is a plain HTTP POST
(not GraphQL); the content type is read from the `Content-Type` header:

```bash
curl -X POST \
  "http://localhost:5048/attachments/ed029b3d-5d77-4dbf-930d-8745f80c354b/proposal.pdf" \
  -H "Content-Type: application/pdf" \
  --data-binary @proposal.pdf
```

### Create a Job

A job assigns a request to a drafter. Supply a `requestId` and the `assigneeId` of a user:

```graphql
mutation CreateJob($input: CreateJobInput!) {
  createJob(input: $input) {
    job {
      jobId
      status
    }
    errors {
      __typename
      ... on Error {
        message
      }
    }
  }
}
```

```json
{
  "input": {
    "requestId": "0da34bee-9895-4a5d-9218-ea6f1d4dcd95",
    "assigneeId": "68c4adf8-8d42-4849-a8d5-8afc289d3689",
    "description": "Draft the widget safety bill"
  }
}
```

### Create a Draft and attach a document

Submit a draft against an open job:

```graphql
mutation SubmitDraft($input: SubmitDraftInput!) {
  submitDraft(input: $input) {
    draft {
      draftId
      comment
    }
    errors {
      __typename
      ... on Error {
        message
      }
    }
  }
}
```

```json
{
  "input": {
    "jobId": "1c828e45-3aed-4007-b921-086be76f5759",
    "comment": "First draft for review"
  }
}
```

Add an attachment record to the returned `draftId`:

```graphql
mutation AddDraftAttachment($input: AddAttachmentInput!) {
  addAttachment(input: $input) {
    attachment {
      attachmentId
      attachmentUri
      attachmentRole
      storageKey
    }
    errors {
      __typename
      ... on Error {
        message
      }
    }
  }
}
```

```json
{
  "input": {
    "role": "DRAFT",
    "draftId": "d3aa6719-3f91-49a9-876c-64787a2ef1e2"
  }
}
```

Then upload the document bytes for the returned `attachmentId`:

```bash
curl -X POST \
  "http://localhost:5048/attachments/2a063bf2-9123-4c7e-81b6-ed8dd7db296d/draft.docx" \
  -H "Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document" \
  --data-binary @draft.docx
```
