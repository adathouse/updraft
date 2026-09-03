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

The Entra app registration below is reserved for non-development authentication. Its
Authority, Audience, and `roles` claim mapping are not enabled yet; `Program.cs` tracks
that work as a TODO. Local development does not require Entra configuration. See
[Local development tokens](#local-development-tokens).

- name: Updraft - DEV
- tenant: 4979d838-afe7-4f16-ac52-461bafc329ae
- client id: 4d67f493-8e21-46ec-825a-afed3b38e9e5
- scope: api://4d67f493-8e21-46ec-825a-afed3b38e9e5/Updraft.Users

## Local development tokens

The API validates JWTs. For local development, mint tokens with the built-in
[`dotnet user-jwts`](https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn)
tool; no Entra sign-in is required. The tool stores a dev signing key in user secrets and
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
  -d '{"query":"mutation { registerCurrentUser { user { id entraId name email roles } } }"}'
```

After registering, the same token resolves to that user on every request; a token whose
identity has not been registered is rejected as an unknown user.

Send the printed token in the `Authorization` header (scheme `Bearer`) on both `/graphql`
requests and attachment uploads:

```bash
curl http://localhost:5048/graphql \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"query":"{ requests(first: 25) { nodes { id } pageInfo { hasNextPage endCursor } } }"}'
```

List or remove existing dev tokens:

```bash
dotnet user-jwts list
dotnet user-jwts clear
```


## Running the tests

Integration tests live in `tests/Updraft.Tests` and host the API in-process with
`WebApplicationFactory`. They mint their own JWTs with a dedicated test signing key, so no
`dotnet user-jwts` setup is required. A reachable PostgreSQL database (see
[Connection environment](#connection-environment)) is required for the authorized-path tests.

From the repository root:

```bash
dotnet test tests/Updraft.Tests
```

### Database requirement

The authorized-path and scenario tests run against a real PostgreSQL database; they do not
substitute an in-memory provider. The tests expect the schema and the Flyway-seeded
`offices` reference data to be present, so apply migrations before running them:

```bash
cd flyway; flyway migrate
```

`UpdraftWebApplicationFactory` only overrides the JWT signing key, issuer, and audience; it
leaves the real `DbContext` and connection string intact. If PostgreSQL is unreachable or
unmigrated, these tests fail rather than skip.

### End-to-end scenario test

`ScenarioWorkflowTests` exercises the full happy path (request, job, draft, attachment
record, file upload) across the Requester, FrontOffice, and Drafter roles. It registers
three users with stable identities, so `registerCurrentUser` is idempotent and the test is
safe to re-run; each run creates fresh request/job/draft rows, which accumulate by design.

```bash
dotnet test tests/Updraft.Tests --filter "FullyQualifiedName~ScenarioWorkflowTests"
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

## Sample queries and mutations

The API is served at `http://localhost:5048/graphql/`. Mutations use Hot Chocolate
mutation conventions, so every mutation takes a single `input` argument and returns a
payload containing the result. Mutations configured with typed domain errors also expose
an `errors` field. GraphQL exposes entities through opaque global `ID` values. Use the
`id` returned by the API for object-reference inputs such as `requestId`, `jobId`, and
`officeId`; clients must not construct IDs from database keys.

Every operation requires a Bearer token for a registered User. The workflow below changes
roles between steps: use a Requester token to create the Request, a FrontOffice token to
create the Job, and the assigned Drafter's token to submit the Draft. Use the same token
that created the parent resource when adding or uploading its Attachment.

> N.B. Make sure you use ids returned by the API. 
> GraphQL uses opaque global IDs that are not the same as the Guids.
> Guids don't work. 
> The ids have special sauce that isn't easy to replicate, so always query for the ids.

### Create a Request and attach a document

Use a registered Requester token to submit the Request:

```graphql
mutation SubmitRequest($input: SubmitRequestInput!) {
  submitRequest(input: $input) {
    request {
      id
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
    "officeId": "<office-id-not-the-guid>",
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

Using the same Requester token, create the Attachment record with the returned Request `id`.
The GraphQL enum literals are `INTAKE_SUPPORT`, `PRIOR_LEGISLATION`, `POLICY_PAPER`, and
`DRAFT`; PostgreSQL stores the corresponding values as `IntakeSupport`,
`PriorLegislation`, `PolicyPaper`, and `Draft`.

```graphql
mutation AddRequestAttachment($input: AddAttachmentInput!) {
  addAttachment(input: $input) {
    attachment {
      id
      attachmentUri
      attachmentRole
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
    "requestId": "<request-id>"
  }
}
```

Upload the document bytes using the opaque upload identifier or URI returned by the API.
This is a plain HTTP POST (not GraphQL); the content type is read from the `Content-Type`
header. Clients must not decode the GraphQL `id` to construct this route:

```bash
curl -X POST \
  "http://localhost:5048/attachments/<upload-id>/H2821_RH_xml.pdf" \
  -H "Authorization: Bearer <requester-token>" \
  -H "Content-Type: application/pdf" \
  --data-binary @H2821_RH_xml.pdf
```

### Create a Job

Use a registered FrontOffice token to create the Job. Supply the opaque Request `id` and
Drafter User `id` returned by GraphQL:

```graphql
mutation CreateJob($input: CreateJobInput!) {
  createJob(input: $input) {
    job {
      id
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
    "requestId": "<request-id>",
    "assigneeId": "<drafter-user-id>",
    "description": "Sample draft of the sampling bill."
  }
}
```

### Create a Draft and attach a document

Use the registered token for the Drafter assigned to the open Job:

```graphql
mutation SubmitDraft($input: SubmitDraftInput!) {
  submitDraft(input: $input) {
    draft {
      id
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
    "jobId": "<job-id>",
    "comment": "First draft for review"
  }
}
```

Using the same Drafter token, add an Attachment record with the returned Draft `id`:

```graphql
mutation AddDraftAttachment($input: AddAttachmentInput!) {
  addAttachment(input: $input) {
    attachment {
      id
      attachmentUri
      attachmentRole
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
    "draftId": "<draft-id>"
  }
}
```

Then upload the document bytes using the opaque upload identifier or URI returned by the
API:

```bash
curl -X POST \
  "http://localhost:5048/attachments/<attachment-guid>/H4348_RH_xml.pdf" \
  -H "Authorization: Bearer <drafter-token>" \
  -H "Content-Type: application/xml" \
  --data-binary @H4348_RH_xml.pdf
```
