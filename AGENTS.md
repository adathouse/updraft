# AGENTS.md

Instructions for AI agents working with this code base.

## Project

Updraft is a .NET 10 GraphQL API using Hot Chocolate 16.x. 
It uses EFCore to interface with a PostgreSQL database and Foundatio to interface with BLOB storage. 
It uses flyway to manage the database schema. 
Updraft provides an API for consumers to request and collaborate on draft legislation.

See @DATA.md for details on the data model.

See @USE_CASES.md for details on the use cases and roles supported by this API.

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

## Conventions

### GraphQL / HotChocolate

- Resolver classes are `internal static partial` with `[QueryType]`, `[MutationType]`, and `[ObjectType<TEntity>]`, registered through the source generator: `Properties/ModuleInfo.cs` declares `[assembly: Module("XTypes")]` and `Program.cs` calls the generated `AddXTypes()`.
- By-id resolvers are `[NodeResolver, Lookup]`. Entities use natural keys that are plain model properties; models carry no GraphQL annotations. Each node type re-exposes its key as `id` via a `GetId` projection resolver (`[Parent(requires: ...)]`), with ID serialization inferred from the `[NodeResolver, Lookup]` resolver.
- Every connection resolver supplies a stable order: `order.IfEmpty(...)` for the default sort plus an always-appended unique tie-breaker (for example `.AddAscending(x => x.Id)`). Paged resolvers return `PageConnection<T>` via `.With(query, order).ToPageAsync(paging, ct)`.
- Child resolvers on `[ObjectType<T>]` classes declare parent data requirements with `[Parent(requires: ...)]`; omitting them lets projections drop the columns the resolver reads.
- Cross-subgraph references reuse the join entity as the runtime type (for example `[ObjectType<CommitteeBill>]` renamed to `"Bill"`): the node class ignores every raw property, re-exposes the key as `id` via a `GetId` projection resolver with an explicit `[ID<T>]` (no `@shareable`; the `[Lookup, Internal]` by-id resolver emits the `@key`), and the lookup constructs the join entity carrying only the key, with the unused carrier left empty. These references declare no input types of their own: where sorting is offered, `[UseSorting]` binds the related entity's existing sort input (for example `CommitteeSortInput` on `Bill.committees`), filtering is not exposed, and connections of these references (for example `Committee.bills`) take no filter/sort arguments at all.
- DataLoaders in all flavours must apply `.With(query.Include(x => x.Key))` so the key survives the projection.
- Mutations contain no business logic: the resolver injects `ISender` and dispatches a command record; the handler lives in `Commands/` and throws a sealed typed domain exception with identifying properties (for example `MemberNotFoundException`, no HotChocolate dependency) for domain errors. The mutation declares each exception with `[Error<T>]` so it surfaces as a typed payload error. `GraphQLException` is reserved for unexpected technical errors.
- Every Mocha handler must be registered through `AddMediator().AddHandler<THandler>()` in the subgraph composition setup. Nothing catches a missing registration at compile time; `SendAsync`/`QueryAsync` fails at runtime.
- In Layered, query records carry `QueryContext<T>` and `PagingArguments` as properties and return `Page<T>` from the handler (`IQuery<Page<T>>`); the resolver relies on the implicit `Page<T>` to `PageConnection<T>` conversion.

### EF Core

- One sealed `DbContext` per subgraph, primary-constructor style, model configured inline in `OnModelCreating`. No migrations: seeding calls `EnsureCreatedAsync()` and inserts only if empty.
- Every read query uses `AsNoTracking()`.
- Seeding is guarded by `!args.IsGraphQLCommand()` so schema-export CLI runs never touch the database.

## Code Quality

### C# / .NET

- Always use curly braces for loops and conditionals, no exceptions.
- Use file-scoped namespaces and 4-space indentation.
- Test naming: `Method_Should_Outcome_When_Condition`.
- No vacuous assertions (`Assert.NotNull` alone is not a test).
- If a test requires excessive stubs and reflection, you're at the wrong test tier.
- Do not use em dash style sentences in docs, comments, or XML documentation. Use commas, periods, parentheses, or colons instead.
- XML docs should describe the contract and concepts, not internals like pooling or iteration mechanics, and should not leak other implementation details.
- XML docs and comments are 1-2 sentences stating the contract: what it is, what null or edge values mean. No rationale, no use-case examples, no design justification. If a sentence explains why the design is right instead of what the member promises, delete it. The same applies to docs pages: every sentence must inform the reader, none may justify the design.
- Do not make new parameters optional just to avoid updating call sites. A parameter should only be optional when it has a sensible semantic default and the API is frequently used (where call-site brevity outweighs explicitness). If a parameter is logically required, make it required and update all call sites.

### Testing

- Prefer snapshot tests over manual `Assert` calls, use **CookieCrumble** for snapshots.
- CookieCrumble has native snapshot support for `IExecutionResult`, `GraphQLHttpResponse`, and other core types.
- For smaller snapshots, prefer **inline snapshots** (`MatchInlineSnapshot`) over snapshot files.
- For a collection of results (for example a stream of subscription events), snapshot the list with `MatchInlineSnapshots` (a parallel list of per-element inline snapshots). Do NOT concatenate with `string.Join("---", values).MatchInlineSnapshot(...)`: a manual separator hides element boundaries and reinvents what the collection overload does natively.
- For tests with multiple assertions, use **Markdown snapshots** (`MatchMarkdownSnapshot`).
- Hard limit: a single test method must contain at most 5 `Assert.*` calls. Anything beyond that is too hard to reason about in review, switch to a snapshot (Markdown for multi-shape state, inline or file for a single output).
- Use the AAA section marker style. Each section starts with a single-line comment, the test name documents intent, no paragraph-style block comments above sections:

  ```csharp
  // arrange
  // optional one-line description, only when the next code is non-obvious
  ... arrange code ...

  // act
  ... act code ...

  // assert
  ... assert code ...
  ```

- Avoid `Assert.DoesNotContain` as it is a weak assertion that easily goes out of date, it only proves something is absent without verifying what *is* present. Prefer `Assert.Equal` to check the entire string value, or `Assert.Collection` to verify the complete contents of a collection.
- Snapshot tests: update from `__mismatch__/` directory, understand ordering issues before updating.
- Filter tests during iteration, never run the full suite unnecessarily.
- Use real databases (PostgreSQL) in integration tests, not mocks (unless explicitly instructed otherwise).
