# Project State

## Current Phase

Phase 1.10 — Role-Based Authorization

## Completed

- Phase 0.1 — Project Definition
- Phase 0.2 — Requirements
- Phase 0.3 — Feature Map
- Phase 0.4 — Business Rules
- Phase 0.5 — Domain Model
- Phase 0.6.1 — Architecture style and layer responsibilities
- Phase 0.6.3 — SOLID and Dependency Inversion guidelines
- Phase 0.6.4 — Business logic and use-case boundaries
- Phase 0.6.5 — Transactions and business consistency
- Phase 0.6.6 — Architecture verification
- Phase 0.7.1 — Backend technical stack decision
- Phase 0.7.2 — Database and persistence stack decision
- Phase 0.7.3 — Frontend technical stack decision
- Phase 0.7.4 — Authentication and security direction
- Phase 0.7.5 — Layered testing responsibilities
- Phase 0.7.6 — Docker, CI/CD, and deployment direction
- Phase 0.7.7 — Technical stack consistency verification
- Phase 1.1 — .NET solution and project foundation
- Phase 1.2 — Layered .NET testing project foundation
- Phase 1.3 — Product and Category Domain
- Phase 1.4 — Category and Product Application Use Cases
- Phase 1.5 — Category and Product Persistence
- Phase 1.6 — Catalog API
- Phase 1.7 — Users and Authentication Foundation
- Phase 1.8 — User Persistence and Authentication Infrastructure
- Phase 1.9 — JWT Authentication API
- Phase 1.10 — Role-Based Authorization

## Current Work

The API supports registration, login, JWT bearer validation, retrieval of the validated caller identity, and initial role-based access rules for the catalog.

## Phase 1.7 User and Authentication Decisions

- `EmailAddress` trims and lowercases email values to provide one case-insensitive login identity. Syntax validation is deliberately basic; email verification and delivery workflows are not part of this phase.
- `User` requires a nonblank name, email identity, and `PasswordHash`. Password hash representation is immutable and its string representation is redacted. Domain does not hash passwords or depend on a hashing framework.
- `UserRole` is a small enum with `User` (ordinary User/Client) and `Admin`. Users can hold more than one role; a new user defaults to `User` and the Domain prevents removing the last role.
- New users start inactive and require explicit activation before successful authentication. Role and activation methods establish domain behavior; administrative authorization/workflows are not implemented in this phase.
- `IUserRepository` expresses email lookup, duplicate check, and add operations. `IPasswordHasher` is the Application boundary for producing and verifying hashes; Infrastructure implements it with the established ASP.NET Core Identity password hasher and no custom cryptography.
- Registration hashes the supplied password before creating the User. Password policy and hashing parameters remain deferred.
- Authentication returns `Authenticated`, `InactiveUser`, or `InvalidCredentials`. Missing users and incorrect passwords both return `InvalidCredentials`; an inactive-user result is returned only after correct password verification. No JWT is generated.
- Email duplicate pre-checks are for application-level feedback only; the User table's unique email index enforces durable uniqueness under concurrency.
- Any later operation that accepts an Admin role or changes roles must be protected by backend authorization; this phase adds no API surface or authorization enforcement.

## Phase 1.8 User Persistence and Password Hashing

- Added `users` table with UUID primary key, required name, normalized email, active state, password hash, and role collection.
- Email is stored as `varchar(320)` and has unique index `ux_users_email`; `EmailAddress` normalizes email before persistence and lookup, and the database unique constraint remains authoritative under concurrent writes.
- Password hashes are stored as required `varchar(512)`; no plaintext password column exists. `PasswordHash` is mapped through an explicit value converter.
- Roles are serialized as a required JSONB array of numeric `UserRole` values and mapped from the domain's private role set, with explicit EF value conversion and change tracking.
- `IUserRepository` is implemented by `UserRepository`; `IPasswordHasher` is implemented by `AspNetPasswordHasher`. Both are registered through existing `AddPersistence`.
- `AspNetPasswordHasher` uses `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` from `Microsoft.Extensions.Identity.Core` 10.0.12. The framework's Identity V3 format uses PBKDF2-HMAC-SHA512 with a per-password random salt and embedded format/work-factor data; verification accepts successful and rehash-needed results. No custom cryptography or credential logging is used.
- Migration: `20261008032603_AddUsers`. Existing Category/Product tables and mappings are unchanged.
- Infrastructure integration tests reuse the existing PostgreSQL 17 Testcontainers fixture; clean test databases apply both migrations in order.

## Phase 1.9 JWT Authentication API

- Added `POST /auth/register`, `POST /auth/login`, and protected `GET /auth/me` Minimal API endpoints. Register and login use the existing Application authentication use cases; endpoints do not call repositories or `CatalogDbContext`.
- Registration accepts name, email, and password only. It hashes through `IPasswordHasher`, returns only the new user ID and active state, and never returns a password or hash. Duplicate email maps to 409; request validation maps to 400.
- Existing Domain behavior is preserved: newly registered users are inactive. Registration therefore does not issue a token, and no activation endpoint or workflow is introduced in this phase.
- Invalid credentials and inactive accounts receive the same generic 401 Problem Details response; neither case issues a token. Successful login returns only a bearer access token and its UTC expiration.
- Infrastructure implements `IAccessTokenIssuer` using HS256. Tokens contain `sub` (user ID), `jti`, and one `role` claim for each assigned role. No password, hash, or other sensitive profile data is included.
- Bearer validation requires a valid signature, configured issuer and audience, a signed token, a GUID subject, and a valid expiration. Clock skew is 30 seconds. No validation is disabled for development.
- Required settings are `Authentication:Jwt:Issuer`, `Authentication:Jwt:Audience`, and `Authentication:Jwt:SigningKey`; the key must be at least 32 UTF-8 bytes and is supplied externally (for example, `Authentication__Jwt__SigningKey`). Token lifetime defaults to 15 minutes and can be configured from 1 to 60 minutes. No signing secret or JWT settings with secret values are committed.
- Application defines `ICurrentUser` (`UserId`, `IsAuthenticated`, and role names) and `IAccessTokenIssuer`; API implements current identity from the validated principal. Application and Domain do not reference HttpContext, ASP.NET Core, or JWT libraries.
- Only `/auth/me` requires authentication in this phase. Catalog endpoints remain unchanged and unauthenticated; no role policies or business authorization rules are added.
- This describes the Phase 1.9 state; Phase 1.10 now requires authentication for catalog endpoints and restricts catalog mutations to Admin.
- API integration tests reuse WebApplicationFactory and the PostgreSQL 17 Testcontainers fixture. Coverage includes registration, duplicate and malformed input, hash-only persistence, secret-free responses, valid login, generic invalid/inactive login, user/role claims, authenticated current-user context, and missing, malformed, wrong-signature, wrong-issuer, wrong-audience, and expired-token rejection.

## Phase 1.10 Role-Based Authorization

- API defines the `AdminOnly` role policy using the existing validated JWT `role` claims and `RequireRole("Admin")`. No new permission model, role claim format, or current-user abstraction was added.
- All Category and Product endpoints require an authenticated principal. Authenticated `User` and `Admin` roles may list and retrieve catalog data.
- Category mutations (`POST`, `PUT`, `DELETE`) and Product mutations (`POST`, `PUT`) require `Admin`. These are catalog maintenance operations; regular users retain read access without being given catalog administration authority.
- Public endpoints remain `POST /auth/register` and `POST /auth/login`; `GET /auth/me` and all catalog endpoints require authentication. No user/admin-management endpoints exist.
- Authorization is based only on role claims from a bearer token that passes the existing signature, issuer, audience, subject, and lifetime validation. Request bodies cannot grant roles. The authorization middleware returns 401 for missing/invalid identity and 403 for an authenticated caller without the required Admin role; forbidden responses use Problem Details without exposing policy internals.
- API integration tests cover anonymous access denial, User catalog reads, User denial for Admin writes, Admin success, multi-role token behavior, unrecognized role denial, and rejection of a registration payload attempting to assign Admin.
- The test fixture reuses the PostgreSQL Testcontainers host, registration/login endpoints, and domain activation/role behavior to obtain real signed tokens; production authentication, Domain, Application, and Infrastructure contracts are unchanged.
- New registrations remain inactive and default to `User`. No activation or role assignment workflow is introduced. The public self-registration route is retained from Phase 1.9; access to catalog operations still requires an active account and a valid JWT.

## Phase 1.6 API Decisions

- Categories are exposed at `/api/categories`: create, get by ID, list, update name/status, and remove.
- Products are exposed at `/api/products`: create, get by identifier, list/filter by category/status/search term, and update name/category/status.
- API contracts use request/response DTOs; Domain entities and EF Core types are not exposed.
- API request shape validation is distinct from Application business validation. Endpoints call the existing Application use cases and do not call repositories or `CatalogDbContext`.
- HTTP outcomes use 201 for creation, 200 for query/update, 204 for deletion, 400 for invalid input, 404 for missing resources, and 409 for conflicts. Errors use Problem Details; database error details are not returned to clients.
- The API registers the existing `AddPersistence` composition with `ConnectionStrings:InventoryDatabase`. Set it through `ConnectionStrings__InventoryDatabase` or equivalent external configuration; no credentials are committed.
- The API does not apply migrations automatically. Development exposes the generated OpenAPI document at `/openapi/v1.json`.
- API integration tests use WebApplicationFactory and an isolated PostgreSQL 17 Testcontainers database, migrated by the test fixture.

## Phase 1.5 Persistence Decisions

- EF Core 10.0.12 and Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 provide PostgreSQL persistence. PostgreSQL 17 in Docker/Testcontainers is used for integration tests.
- `categories` uses a generated UUID key; category name is limited to 200 characters. A stored generated lowercase name key has a unique index to enforce the case-insensitive uniqueness already checked by Application.
- `products` uses an Infrastructure-only UUID persistence key; its immutable `ProductIdentifier` value object is stored in a required `varchar(100)` column with a unique index. Product name is required text; active state is required.
- Products have a required Category foreign key, indexed for lookups. Delete behavior is explicitly `NO ACTION`; product rows are not cascaded away when categories are deleted.
- EF maps the Category product collection through its backing field. Orphan deletion is deferred until save so a tracked product can be reassigned between required categories without an intermediate orphan failure.
- `AddPersistence` registers the scoped repositories and `CatalogDbContext` from an externally supplied connection string. The design-time context factory reads `ConnectionStrings__InventoryDatabase`; no connection string or credential is committed.
- Migration: `20261007224342_InitialCategoryProductCatalog`.

## Next

Await human approval before defining or starting another phase. Inventory and Sales work has not begun.

## Known Issues

- No concrete Angular project structure has been created yet.
- No detailed frontend feature layout has been defined yet.
- Detailed API contracts beyond the current catalog and authentication endpoints have not been selected.
- API JWT authentication is implemented; production signing-key provisioning, secure storage, and key rotation remain deployment decisions.
- Dockerfiles, local orchestration files, GitHub Actions workflows, and deployment resources have not been created.
- Database schema outside Category, Product, and User and detailed persistence, deployment, and runtime configurations remain to be designed.
- The Application pre-checks remain for user-friendly conflicts; PostgreSQL unique indexes are authoritative under concurrent writes.
- Category uniqueness is case-insensitive; the exact Unicode/collation equivalence between .NET `OrdinalIgnoreCase` and PostgreSQL `lower()` has not been formalized.
- Category removal is blocked by the database while any product (including inactive products) references the category. Reassign inactive products before deletion; the business's finer inactive-product deletion policy remains unresolved.
- Category name (200 characters) and ProductIdentifier (100 characters) database limits are persistence constraints; corresponding business-level maximum-length policy is not otherwise specified.
- Migration deployment/rollback process, database seeding, transaction/concurrency strategy, and production database operations remain deferred.
- Product listing supports category, active status, name, and identifier filters. Supplier filtering remains unavailable until supplier relationships are implemented.
- Product supplier relationships, pricing/cost, tax context, stock, and movement behaviors remain outside this phase.
- Authorization for inventory, sales, supplier, customer, and user-management endpoints, plus frontend authorization, remains unimplemented.
- There is no application workflow yet for activating new users or provisioning the first Admin. Until that is defined, new registrations remain inactive and cannot access catalog endpoints; operational bootstrap must be handled outside this phase.

## Deferred decisions still pending

- exact Angular version and project configuration
- routing strategy and state-management model
- detailed API contracts for resources beyond Category and Product
- JWT signing-key provisioning, secure storage and rotation
- Password-hash work-factor upgrades and automatic rehash-on-login behavior
- refresh-token lifecycle, revocation/logout behavior, and role/account-change propagation
- frontend token storage and browser-specific CSRF/XSS/cross-origin protections
- fine-grained permissions and policies for future non-catalog business areas
- authorized user provisioning, account activation, and initial Admin bootstrap
- credential recovery, multi-factor authentication, login abuse controls, and audit retention/privacy policies
- forbidden-response details for future authorization policies and deployment-specific protection settings
- specific component and feature layout
- frontend validation, caching, and logging conventions
- deployment and environment-specific frontend configuration
- hosting provider, deployment topology, production runtime sizing, container registry, and release process
- deployment and rollback automation, production database operations, and environment/secret-management services
- container hardening, networking, TLS, observability, scaling, and operational ownership
- concrete Dockerfiles, local orchestration configuration, and GitHub Actions workflow definitions

## Verification

### Phase 1.9

- API authentication integration tests (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj --no-build --no-restore`): PASS — 19 passed, 0 failed, 0 skipped.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — Domain 40, Application 31, Infrastructure 17, API 19; 107 passed, 0 failed, 0 skipped.
- Full solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors.
- Authentication integration: PASS — registration and login execute through WebApplicationFactory, Application, Infrastructure, and the existing PostgreSQL 17 Testcontainers database.
- JWT validation: PASS — generated token authenticates `/auth/me`; missing, malformed, wrong-signature, wrong-issuer, wrong-audience, and expired tokens receive 401. Claims verified include user ID (`sub`) and `User`/`Admin` roles (`role`).
- Security behavior: PASS — invalid and unknown-user credentials have the same public failure details; inactive users receive no token; registration/login responses contain neither plaintext password nor password hash.
- Architecture verification: PASS — Domain and Application contain no ASP.NET Core/HttpContext/JWT references; API authentication endpoints call Application use cases and Application token/current-user contracts; endpoint handlers do not access repositories or `CatalogDbContext`. Catalog endpoints and persistence migrations are unchanged.
- Configuration review: PASS — production JWT signing key is required from external configuration, has a 32-byte minimum, is not committed, and validation is not weakened in development.

### Phase 1.10

- Role authorization integration tests (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj --filter "FullyQualifiedName~AuthorizationApiTests"`): PASS — 6 passed, 0 failed, 0 skipped.
- API integration suite (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj --no-build --no-restore`): PASS — 25 passed, 0 failed, 0 skipped, including existing Authentication and Catalog tests.
- Anonymous protected catalog requests return 401; authenticated User reads succeed and Admin-only writes return 403; Admin writes succeed: PASS.
- Multiple valid roles are carried through `/auth/me` and Admin role grants the Admin policy: PASS.
- A signed token with an unrecognized role and a registration payload attempting role assignment do not grant Admin access: PASS.
- Existing JWT validation remains in force; Domain, Application, and Infrastructure authorization contracts were not modified. Endpoint handlers still access Application use cases only and no repositories/DbContext: PASS.
- Full solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — 113 passed, 0 failed, 0 skipped: Domain 40, Application 31, Infrastructure 17, API 25.

### Phase 1.8

- Infrastructure tests (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests/SistemaInventarioVentas.Infrastructure.Tests.csproj --no-restore`): PASS — 17 passed, 0 failed, 0 skipped, including all existing Category/Product integration tests.
- Domain tests (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj --no-restore`): PASS — 40 passed, 0 failed, 0 skipped.
- Application tests (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj --no-restore`): PASS — 31 passed, 0 failed, 0 skipped.
- API tests (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj --no-restore`): PASS — 12 passed, 0 failed, 0 skipped.
- Full solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors.
- Full test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — 100 passed, 0 failed, 0 skipped.
- Migration application: PASS — the existing PostgreSQL fixture created clean PostgreSQL 17 containers and applied the original Category/Product migration followed by `20261008032603_AddUsers`.
- EF model snapshot (`dotnet ef migrations has-pending-model-changes`): PASS — no pending changes. EF CLI 10.0.11 reports it is older than the EF runtime 10.0.12.
- User persistence coverage: normalized email retrieval/existence, database email uniqueness conflict, role set, active/inactive status, password-hash round-trip, and registration in shared existing PostgreSQL fixture: PASS.
- Password hashing coverage: separate hashes for repeated same-password input, correct-password verification, incorrect-password rejection, and no plaintext substring in the stored hash: PASS.
- Security/architecture verification: Domain and Application remain free of EF Core, PostgreSQL, ASP.NET Core, and JWT dependencies; Infrastructure implements Application abstractions. API source was not modified; no authentication endpoints, middleware, policies, tokens, or JWT packages were added.
- Catalog migration and Category/Product integration tests continue to pass: PASS.

### Phase 1.7

- Domain tests (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj`): PASS — 40 passed, 0 failed, 0 skipped.
- Application tests (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj`): PASS — 31 passed, 0 failed, 0 skipped.
- Infrastructure tests (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests/SistemaInventarioVentas.Infrastructure.Tests.csproj --no-restore`): PASS — 11 passed, 0 failed, 0 skipped.
- API tests (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj --no-restore`): PASS — 12 passed, 0 failed, 0 skipped.
- Full solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors.
- Full test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — 94 passed, 0 failed, 0 skipped.
- Architecture verification: PASS — Domain has no framework/authentication dependencies; Application references Domain only and has no JWT dependency; Infrastructure and API production code were not changed for user persistence or authentication.
- Catalog regression coverage: PASS — all previous Domain, Application, Infrastructure, and API tests remain passing.
- No JWT generation/validation, API authentication endpoint/configuration, authorization policies, EF User persistence, PostgreSQL migration, refresh tokens, password reset, email verification, MFA, frontend authentication, permissions matrix, or audit implementation was added.

### Phase 1.6

- API integration tests (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj --no-restore`): PASS — 12 passed, 0 failed, 0 skipped. Tests exercised HTTP requests through Application and Infrastructure against PostgreSQL 17 Testcontainers.
- API integration coverage: Category create/get/list/update/remove, invalid input, not-found, duplicate-name conflict, active-product removal conflict, database foreign-key conflict; Product create/get/list filters/update, invalid input/identifier, not-found, duplicate-identifier conflict, inactive-category conflict; Development OpenAPI document.
- API runtime and PostgreSQL communication through the started WebApplicationFactory host: PASS — migration applied by the fixture and catalog endpoints performed database-backed reads/writes.
- Full solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — 65 passed, 0 failed, 0 skipped.
- API dependency verification: PASS — API depends on Application and Infrastructure for composition; endpoint handlers use Application use cases and DTOs, not repositories or `CatalogDbContext`; no circular project references.
- Clean Architecture verification: PASS — Application references Domain only; Infrastructure implements Application/Domain persistence; Domain has no framework or outer-layer dependencies.
- No authentication, authorization, frontend, inventory, sales, supplier, or customer implementation was added: PASS.

### Phase 1.5

- Infrastructure PostgreSQL integration tests (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests/SistemaInventarioVentas.Infrastructure.Tests.csproj --no-restore`): PASS — 11 passed, 0 failed, 0 skipped
- Application tests (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj --no-restore`): PASS — 19 passed, 0 failed, 0 skipped
- Domain tests (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj --no-restore`): PASS — 23 passed, 0 failed, 0 skipped
- Full solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — 53 passed, 0 failed; API test project has no test cases yet
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — no pending model changes
- Domain dependency check: PASS — no project/package references or source references to Application, Infrastructure, API, EF Core, ASP.NET Core, Npgsql, or PostgreSQL
- Application dependency check: PASS — Domain is the only project reference; no package references or source references to Infrastructure, API, EF Core, ASP.NET Core, Npgsql, or PostgreSQL
- Migration application, Category/Product persistence, relationship loading/reassignment, ProductIdentifier mapping, database uniqueness, FK delete restriction, filtered repository queries, and cancellation behavior verified against PostgreSQL 17: PASS
- No API endpoints, authentication, authorization, frontend, inventory, sales, supplier, or customer persistence was added: PASS
- Tooling note: installed EF CLI is 10.0.11 while the EF Core runtime is 10.0.12; migration generation and snapshot verification succeeded, but the CLI reported the version mismatch.

### Earlier phases

- Phase 1.4 Application tests (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj --no-restore`): PASS — 19 passed, 0 failed, 0 skipped
- Phase 1.4 complete solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors
- Phase 1.4 full test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — 42 passed, 0 failed; API and Infrastructure test projects contain no test cases and reported no tests available
- Application dependency check: PASS — Application references Domain only and has no package references or source references to Infrastructure, API, EF Core, ASP.NET Core, Npgsql, or PostgreSQL
- Application contracts and use cases use asynchronous repository operations with cancellation support; category reads explicitly request associated products: PASS
- Expected validation, not-found, duplicate, inactive-category, and category-in-use outcomes are represented as application results; domain rules remain enforced by Domain: PASS
- No EF Core, PostgreSQL, repositories with infrastructure implementations, API endpoints, authentication, authorization, migrations, or frontend code were added: PASS
- Domain tests (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj`): PASS — 23 passed, 0 failed, 0 skipped
- Complete solution build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors
- Complete test suite (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore`): PASS — 23 passed, 0 failed; API, Application, and Infrastructure test projects were discovered and reported no test cases yet
- Domain dependency check: PASS — Domain has no project/package references to Application, Infrastructure, API, EF Core, or ASP.NET Core
- Implemented Category, Product, and ProductIdentifier behaviors covered for valid construction, normalization, category association/reassignment, active/inactive state, removal guard, and invalid inputs: PASS
- No repositories, persistence configuration, schema, migrations, use cases, endpoints, authentication, or frontend code were added: PASS
- Created test projects: `SistemaInventarioVentas.Domain.Tests`, `SistemaInventarioVentas.Application.Tests`, `SistemaInventarioVentas.Infrastructure.Tests`, and `SistemaInventarioVentas.API.Tests`, all targeting `net10.0`: PASS
- Test references are scoped: Domain.Tests → Domain; Application.Tests → Application; Infrastructure.Tests → Infrastructure; API.Tests → API: PASS
- Test projects introduce no references from production projects and no coverage, database, or API-hosting packages: PASS
- Production projects do not reference test projects; test projects introduce no cross-layer or invalid production dependencies: PASS
- Restore (`dotnet restore SistemaInventarioVentas.slnx`): PASS
- Build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors
- Test execution (`dotnet test SistemaInventarioVentas.slnx --no-build --no-restore --verbosity normal`): PASS — all four xUnit adapters launched and discovered their assemblies; 0 test cases exist yet. VSTest reported “No test is available” for each project and returned exit code 0.
- No business tests or production code were added: PASS
- Created `SistemaInventarioVentas.slnx` with the .NET 10 SDK's current default solution format: PASS
- Created projects: Domain, Application, Infrastructure, and API, each targeting `net10.0`: PASS
- Configured references: API → Application and Infrastructure; Infrastructure → Application and Domain; Application → Domain: PASS
- Domain has no project references; project files contain no EF Core or unrelated package dependencies: PASS
- Restore (`dotnet restore SistemaInventarioVentas.slnx`): PASS
- Build (`dotnet build SistemaInventarioVentas.slnx --no-restore`): PASS — 0 warnings, 0 errors
- No business entities, use cases, repositories, database context, authentication, frontend code, or API endpoints were added: PASS
- Technical stack decisions are consistent across architecture and decision log: C#/.NET 10, ASP.NET Core, Minimal APIs, Clean Architecture, PostgreSQL, EF Core, Angular, TypeScript, JWT, Docker, and GitHub Actions: PASS
- Role-based authorization and security/secret-management principles are consistent with the architecture and requirements: PASS
- Layer responsibilities remain consistent: Domain independent; Application orchestrates use cases; Infrastructure owns persistence; API is transport-focused; frontend is not a business-rule authority: PASS
- Layered testing strategy is consistent with requirements and architecture: PASS
- Development and deployment boundaries remain distinct; GitHub Actions restore/build/test checks are required to block unvalidated merges: PASS
- Sale workflow atomicity and the non-negative inventory invariant remain consistent with requirements and architecture: PASS
- Deferred implementation and deployment decisions are explicitly identified: PASS
- No premature implementation decisions contradict the approved architecture: PASS
- Corrected stale deferrals that contradicted later backend and persistence selections: PASS
- No application code, projects, technology configuration, Dockerfiles, or CI workflows were created: PASS
- Docker and local PostgreSQL direction are documented without container configuration: PASS
- Backend and frontend container strategies are documented at the requested level: PASS
- Environment separation and source-control exclusions for secrets/sensitive configuration are documented: PASS
- GitHub Actions and restore/build/test gates, required before merge, are documented: PASS
- Local-development and deployment concerns are distinguished; cloud provider and deployment topology remain unselected: PASS
- Consistency with deployment and CI/CD requirements, architecture, and prior decisions: PASS
- No Dockerfiles, orchestration files, workflows, or deployment resources were created: PASS
- Testing responsibilities align with the Domain, Application, Infrastructure, API, and Frontend boundaries: PASS
- Existing automated testing requirements remain consistent with the layered strategy: PASS
- xUnit test framework and one test project per backend layer selected; coverage targets and test-environment tooling remain deferred: PASS
- Authentication and authorization responsibilities are distinct: PASS
- Backend is identified as the authoritative security boundary: PASS
- JWT is recorded as the planned API authentication mechanism without selecting a library: PASS
- Password storage, token lifecycle, role-based authorization, Application identity, frontend state, and failure outcomes are documented conceptually: PASS
- Consistency with architecture, domain model, business rules, and requirements: PASS
- Implementation details requested to remain deferred are explicitly listed: PASS
- No authentication or frontend implementation was created: PASS