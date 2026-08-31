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

## App registration

The Entra app registration is used for real tokens in non-dev environments. Local
development does not require it — see [Local development tokens](#local-development-tokens).

- name: Updraft - DEV
- tenant: 4979d838-afe7-4f16-ac52-461bafc329ae
- client id: 4d67f493-8e21-46ec-825a-afed3b38e9e5
- scope: api://4d67f493-8e21-46ec-825a-afed3b38e9e5/Updraft.Users

## Local development tokens

The API validates JWTs. For local development, mint tokens with the built-in
[`dotnet user-jwts`](https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn)
tool — no Entra sign-in required. The tool stores a dev signing key in user secrets and
writes the issuer/audience into configuration, which the JwtBearer handler validates
against automatically. The default audience is taken from `launchSettings.json`
(`http://0.0.0.0:5048`), so no `--audience` flag is needed.

NOTE: You must create the token before you start the service.

### Required claims

`PrincipalIdentity.FromPrincipal(...)` builds a `PrincipalIdentity` from the claims
below. The JwtBearer handler maps the short JWT claim names to .NET `ClaimTypes` through its
default inbound map, so the token must carry the JWT claim in the third column:

| PrincipalIdentity field | .NET claim (`ClaimTypes`) | JWT claim | `dotnet user-jwts` flag |
| --- | --- | --- | --- |
| `EntraId` | `NameIdentifier` | `sub` | `--name` |
| `Name` | `Name` | `unique_name` | `--name` |
| `Email` | `Email` | `email` | `--claim email=<email>` |
| `Roles` | `Role` | `role` | `--role <role>` (repeatable) |

`EntraId` is the value used to create and later find the row in the `users` table, so it
must be stable. `--name` sets **both** `sub` (→ `EntraId`) and `unique_name` (→ `Name`), so
the value you pass to `--name` is stored as the user's `entra_id` and doubles as the display
name. Do not try to override `sub` with `--claim sub=...`; that emits a second `sub` value
and the mapping keeps the `--name` value instead.

Mint a token that carries every field, one per role (roles: `Requester`, `Drafter`,
`FrontOffice`):

```bash
dotnet user-jwts create --name "entra-requester-001"   --claim email=requester@example.com   --role Requester
dotnet user-jwts create --name "entra-drafter-001"     --claim email=drafter@example.com     --role Drafter
dotnet user-jwts create --name "entra-frontoffice-001" --claim email=frontoffice@example.com --role FrontOffice
```

Assign multiple roles by repeating `--role`:

```bash
dotnet user-jwts create --name "entra-lead-001" --claim email=lead@example.com --role Drafter --role FrontOffice
```

### Register the user row

On first use, tie the token's identity to a `users` row with the `registerCurrentUser`
mutation. It reads the claims above and inserts `entra_id`, `name`, `email`, and the
comma-joined `roles`; it is idempotent and returns the existing row on later calls:

```bash
curl http://localhost:5048/graphql \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"query":"mutation { registerCurrentUser { user { userId entraId name email roles } } }"}'
```

After registering, the same token resolves to that user on every request; a token whose
identity has not been registered is rejected as an unknown user.

Send the printed token in the `Authorization` header (scheme `Bearer`) on both `/graphql`
requests and attachment uploads:

```bash
curl http://localhost:5048/graphql \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"query":"{ requests { nodes { requestId } } }"}'
```

List or remove existing dev tokens:

```bash
dotnet user-jwts list
dotnet user-jwts clear
```


## Running the tests

Integration tests live in `tests/Updraft.Tests` and boot the API in-memory with
`WebApplicationFactory`. They mint their own JWTs with a dedicated test signing key, so no
`dotnet user-jwts` setup is required. A reachable PostgreSQL database (see
[Connection environment](#connection-environment)) is required for the authorized-path tests.

From the repository root:

```bash
dotnet test tests/Updraft.Tests
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
    "requesterId": "2aac9785-5915-44e1-b990-c1dc6198be3e",
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
    "requestId": "8c32da20-3b18-42dc-88a6-aa6edcdb9bd8"
  }
}
```

Upload the document bytes for the returned `attachmentId`. This is a plain HTTP POST
(not GraphQL); the content type is read from the `Content-Type` header:

```bash
curl -X POST \
  "http://localhost:5048/attachments/85a6a50c-3e21-4ce6-aadb-b0335554b60e/H2821_RH_xml.pdf" \
  -H "Content-Type: application/pdf" \
  --data-binary @H2821_RH_xml.pdf
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
    "requestId": "8c32da20-3b18-42dc-88a6-aa6edcdb9bd8",
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
    "jobId": "d21c7aa4-f48f-4638-ac8b-3476fe95d899",
    "comment": "First draft for review",
    "drafterId": "68c4adf8-8d42-4849-a8d5-8afc289d3689"
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
    "draftId": "ea2fa22c-1025-4ccb-b00a-736a44c78f85"
  }
}
```

Then upload the document bytes for the returned `attachmentId`:

```bash
curl -X POST \
  "http://localhost:5048/attachments/ed94459a-c173-4940-ad5d-9ac038d717ca/H4348_RH_xml.pdf" \
  -H "Content-Type: application/xml" \
  --data-binary @H4348_RH_xml.pdf
```
