# AGENTS.md

Instructions for AI agents working with this codebase.

## Project

Updraft is a .NET 10 API for requesting and collaborating on draft legislation. It uses:

- Hot Chocolate 16.x for GraphQL.
- EF Core with PostgreSQL for structured data.
- Foundatio for BLOB storage.
- Flyway for database schema and reference-data migrations.

Read `DATA.md` for the domain and data contract. Read `USE_CASES.md` for supported workflows, roles, and permissions. Treat those documents as requirements; do not weaken them to match an implementation gap.

## Build and Verify

Run the narrowest relevant check while iterating, then complete the applicable repository checks:

```bash
dotnet build
dotnet test tests/Updraft.Tests/Updraft.Tests.csproj
```

Use `dotnet run` when the change needs a manual API startup check. The project must build with zero warnings and zero errors before a change is complete.

## Architecture and Conventions

### GraphQL and Hot Chocolate

- Define schema fields with annotation-based static partial classes using `[QueryType]`, `[MutationType]`, and `[ObjectType<T>]`. `HotChocolate.Types.Analyzers` discovers these types and generates `AddUpdraftTypes()`; do not manually register discovered types.
- Keep resolvers thin. Queries compose repository queries, and mutations delegate business rules and persistence to services.
- Inject registered dependencies through resolver parameters. Register application services and repositories in `Program.cs`; do not use `[Service]` parameter attributes.
- Return `IQueryable<T>` when filtering, sorting, projection, or pagination should execute in PostgreSQL. Do not materialize a query before Hot Chocolate data middleware runs.
- Apply data middleware only when the field supports that capability. When combined, preserve Hot Chocolate's required order:

	```csharp
	[UsePaging]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	```

- Prefer explicit filter and sort input types for public fields when exposing every mapped entity property would reveal internal fields or create an unnecessarily broad API.
- Use cursor pagination for unbounded collections. Ensure pagination has deterministic ordering with a unique tie-breaker so page boundaries remain stable.
- Use DataLoaders for repeated key-based or one-to-many lookups within one GraphQL request when direct `IQueryable` composition cannot produce one database query. Do not introduce a DataLoader for a resolver that already translates efficiently to a single query.
- Expose domain failures from mutations as typed payload errors with `[Error<TException>]`. Domain exceptions must not depend on Hot Chocolate. Reserve `GraphQLException` for GraphQL-specific technical failures that cannot be represented as domain errors.
- Support global object identification through `AddGlobalObjectIdentification()`. Every exposed node must have an opaque global `ID` field and a `[NodeResolver]` that applies the same authorization and visibility rules as other access paths. Mark input IDs with `[ID]` or `[ID<T>]` when accepting global IDs.
- Pass `CancellationToken` through asynchronous resolvers, services, repositories, EF Core calls, and storage operations.

### Authorization

- Use `HotChocolate.Authorization.AuthorizeAttribute`, not the ASP.NET Core attribute, on GraphQL fields. Use the Hot Chocolate `[AllowAnonymous]` attribute only for intentionally public fields.
- Keep role and policy names in `Security/AuthorizationPolicies.cs`. Do not duplicate the policy inventory in documentation or resolver code.
- Apply an appropriate policy to every query, mutation, and node resolver. Protect REST endpoints with ASP.NET Core endpoint authorization.
- Role authorization is necessary but not sufficient. Apply `ResourceAccess.VisibleTo(CurrentUser)` to every query returning protected resources, including top-level fields, node resolvers, and relationship fields.
- Resolve the registered application user once per GraphQL request through `CurrentUserRequestInterceptor` and consume it with `[CurrentUser]`. Do not use `IHttpContextAccessor` in resolvers.
- Enforce ownership and parent-resource access again in mutation services before reading, changing, or attaching data. Client-supplied IDs are never proof of access.
- Roles for the current request come from validated JWT role claims. Local development tokens are created with `dotnet user-jwts` as documented in `README.md`; create them before starting the service.
- Register authorization in both pipelines: ASP.NET Core with `builder.Services.AddAuthorization(...)` and Hot Chocolate with `.AddGraphQLServer().AddAuthorization()`.

### Services and Repositories

- Route database access through repository interfaces. Repositories expose composable queries and persistence operations; they do not contain HTTP or GraphQL concerns.
- Put workflow validation, ownership checks, and state transitions in service classes. Mutation resolvers should only construct a command and invoke the service.
- Use `AsNoTracking()` for read-only repository queries unless identity tracking or an update is required.
- Keep transaction boundaries around complete business operations. Do not save partially valid state when a workflow requires multiple writes to succeed together.
- Store files through Foundatio's `IFileStorage`. Persist only attachment metadata and storage keys in PostgreSQL.

### Database

- Flyway owns all schema, constraints, indexes, and reference-data migrations. Never create or apply EF Core migrations and never call `EnsureCreated` for application schema management.
- Keep EF Core mappings in `UpdraftDbContext` aligned with Flyway SQL and `DATA.md`.
- Add database constraints for invariants that must hold regardless of the application entry point. Also validate them in services when doing so produces a useful domain error.
- Preserve the primary-key, change-tracking, relationship, and naming rules in `DATA.md`.

## Code Style

- Use file-scoped namespaces and 4-space indentation.
- Always use braces for loops and conditionals.
- Avoid the standalone type name `Node`; it conflicts with Relay and .NET types.
- Use descriptive names. Do not make a parameter optional merely to avoid updating call sites; optional parameters require a meaningful semantic default.
- Keep comments and XML documentation to one or two sentences that state contracts or non-obvious constraints. Do not narrate the code or justify the implementation.
- Do not use em dash punctuation in documentation, comments, or XML documentation.

## Testing

- Name tests `Method_Should_Outcome_When_Condition`.
- Use unit tests for isolated domain logic and integration tests for GraphQL schema behavior, authorization, EF Core queries, PostgreSQL constraints, and storage integration.
- Integration tests use `WebApplicationFactory<Program>`, signed test JWTs, and PostgreSQL. Do not replace PostgreSQL with an in-memory EF provider for behavior that depends on query translation or database constraints.
- Authorization-sensitive fields require tests for unauthenticated, wrong-role, and permitted-role callers. Protected resources also require row-level visibility tests, including node and relationship traversal paths.
- Test observable behavior and complete result shapes. Avoid assertions that only prove a value is non-null or that one unexpected item is absent.
- Keep each test focused. Use arrange, act, and assert comments only when they make a multi-step test easier to scan.
- Run focused tests during iteration and the full affected test project before completion.

