# Decision Log

## Decision 1 — Clean Architecture style with domain-first boundaries

- Decision: The project will use a Clean Architecture style, with business meaning centered in the Domain layer and all technical concerns arranged around it.
- Reason: The project’s domain model and business rules define strict invariants such as inventory non-negativity, valid sale confirmation, and user accountability. Those rules are too critical to be mixed with transport, database, or framework concerns.
- Alternatives considered: A purely layered application without explicit domain-first boundaries; a framework-centric architecture; a database-first design.
- Consequences: The business model remains stable and testable, while infrastructure and API concerns remain replaceable without altering core business logic.

## Decision 2 — Dependency direction is inward

- Decision: Dependencies will flow inward from API to Application to Infrastructure to Domain, with inner layers defining contracts and outer layers adapting to them.
- Reason: This preserves the business rule boundary and keeps the domain independent from technical implementation.
- Alternatives considered: Dependency flow from infrastructure toward the domain; mixed dependencies across all layers.
- Consequences: The architecture enforces separation of concerns and reduces the risk that persistence or HTTP decisions distort the domain.

## Decision 3 — Domain owns business policy; Application orchestrates workflows

- Decision: Business rules remain in the Domain layer, while the Application layer orchestrates use-case flows that combine multiple domain rules.
- Reason: The project includes both invariants and operational sequences; both are essential, but they belong in different layers.
- Alternatives considered: Putting workflow logic in the domain only; placing business validation in API controllers.
- Consequences: Business meaning remains centralized in the domain, while the application layer remains responsible for coordinating correct execution of use cases.

## Decision 4 — Infrastructure and API are external adapters, not business owners

- Decision: Persistence, storage, and HTTP requirements belong to Infrastructure and API respectively, and neither layer owns the business logic.
- Reason: The project must remain independent from implementation strategy and transport details while preserving clear accountability for operational behavior.
- Alternatives considered: Embedding validation and business logic in controllers or repositories; making infrastructure responsible for core rules.
- Consequences: The system can adapt to different technical implementations without rewriting the core domain logic.

## Decision 5 — SOLID and dependency inversion guide the architecture without forcing over-engineering

- Decision: The system will use SOLID principles and dependency inversion as its baseline architecture guidance, while avoiding unnecessary abstractions.
- Reason: The project has real business boundaries and external dependencies, but it does not yet warrant a large pattern framework or excessive generic wrappers.
- Alternatives considered: Creating broad abstractions for every concept; ignoring dependency inversion entirely.
- Consequences: The architecture remains disciplined and maintainable without building unnecessary complexity before the implementation phase begins.

## Decision 6 — Initial deferral of technology, persistence, and schema selection

- Decision: During the initial architecture-definition phase, the project deferred choosing a concrete technology stack, persistence strategy, database schema, and ORM. This was an initial, phase-scoped deferral; the technology and persistence choices were subsequently made in Decisions 9 and 10.
- Reason: The architecture definition is intentionally technology-neutral and should guide future implementation planning without prematurely locking in technical choices.
- Alternatives considered: Choosing an ORM, database, or server framework before the architecture was approved; mixing implementation choices into the architecture definition.
- Consequences: The architecture was first established independently of specific technologies. Decisions 9 and 10 later selected the backend and persistence stack; database schema and detailed persistence implementation remain deferred.

## Decision 7 — Business transactions are atomic at the sale workflow level

- Decision: The sale confirmation workflow is the atomic business operation. The success or failure of the full workflow must be treated as a single business outcome.
- Reason: The sale use case spans customer, inventory, movement, and sales records; leaving any part of that state behind would violate the project’s business integrity rules.
- Alternatives considered: Allowing partial confirmation and compensating later; treating inventory deduction and sale persistence as independent operations.
- Consequences: The architecture enforces a consistent and auditable business outcome while keeping the technical transaction mechanism separate from the business rule itself.

## Decision 8 — Inventories must be treated as a shared, concurrency-sensitive business invariant

- Decision: The system must protect the invariant that inventory never becomes negative even when multiple operations compete for the same stock change.
- Reason: The domain model and business rules state that stock levels must remain valid and traceable across all operations.
- Alternatives considered: Accepting stale inventory checks or partial stock reduction under concurrency; allowing negative stock as a tolerated temporary condition.
- Consequences: The business design remains correct even before the technical stack is selected, while concrete concurrency controls and isolation choices remain deferred for implementation planning.

## Decision 9 — Backend technology direction is C# on .NET 10 with ASP.NET Core and Minimal APIs

- Decision: The backend platform will be C# on .NET 10, using ASP.NET Core and Minimal APIs within a Clean Architecture structure.
- Reason: The project already defines a domain-first architecture with strict inventory, sales, and audit invariants. C# and .NET 10 provide a mature, strongly typed backend platform that supports this style well, while ASP.NET Core and Minimal APIs offer a thin HTTP boundary without constraining the domain model.
- Alternatives considered: selecting a different programming language or runtime; choosing a heavier controller-based API pattern; choosing a framework-first design that would blur business boundaries.
- Consequences: The implementation platform is chosen without changing the business architecture. The architecture remains consistent with the documented Domain, Application, Infrastructure, and API responsibilities. PostgreSQL and Entity Framework Core are selected separately in Decision 10; schema design and specific runtime mechanisms remain deferred to later phases.

## Decision 10 — Relational persistence will use PostgreSQL with Entity Framework Core

- Decision: The project will use PostgreSQL as the relational database and Entity Framework Core as the persistence framework in the Infrastructure layer.
- Reason: The system’s core business model is relational and transaction-sensitive, with strong requirements for stock accuracy, auditability, and sale consistency. PostgreSQL fits the operational data model well, and EF Core provides a .NET-native persistence implementation within the project’s Clean Architecture boundaries.
- Alternatives considered: choosing a non-relational store, bypassing ORM tooling, or mixing persistence logic into the Domain or API layers.
- Consequences: Persistence becomes an explicit infrastructure concern, while the Domain remains independent of database and ORM technology. The project preserves its business rules while moving storage decisions into the infrastructure boundary without changing the business architecture.

## Decision 11 — Frontend technology direction is Angular with TypeScript and HTTP-based API integration

- Decision: The project will use Angular as the client framework and TypeScript as the frontend language, communicating with the ASP.NET Core Minimal API over HTTP.
- Reason: The system has multiple feature areas and user-facing workflows that benefit from a structured client framework, reusable UI components, and a typed frontend language. Angular and TypeScript are well suited to a business application with a complex UI and defined backend contract boundaries.
- Alternatives considered: selecting a different frontend framework, using JavaScript without typing, or embedding business logic in the client layer.
- Consequences: The frontend becomes a presentation and integration layer rather than a new source of business policy. The client remains aligned with the project’s architecture by treating the backend API as the authoritative business boundary while keeping business rules in the Domain and Application layers.

## Decision 12 — JWT-based authentication direction and backend-enforced security

- Decision: The API will use signed JWT bearer access tokens as its planned authentication mechanism. Authentication establishes the caller’s identity; authorization determines access separately and is enforced by the backend using role-based access control and applicable business rules. Passwords will be stored only as one-way adaptive password hashes with unique salts. Access tokens should be short-lived, with a protected refresh mechanism that supports rotation and revocation. The validated user identifier will be made available to Application use cases through an application-facing boundary for accountability.
- Reason: Protected business operations need a consistent server-authoritative identity and access boundary, while sales, inventory, user administration, and audit records require accountability. The frontend needs authentication state for user experience but cannot be trusted to enforce access.
- Alternatives considered: Treating frontend checks as security enforcement; conflating authentication with authorization; persisting plaintext or reversibly encrypted passwords; selecting a concrete token library or browser storage mechanism before implementation requirements are defined.
- Consequences: Every protected API operation must independently validate identity and authorization, and business actions must be attributable to the validated user. The frontend may reflect authentication state and guide navigation only. Decision 19 subsequently selected the password-hashing implementation. Token claims and signing/key management, token lifetimes and refresh/revocation behavior, browser storage and protections, exact role/permission policies, and error contracts remain deferred.

## Decision 13 — Testing responsibilities follow architectural layers

- Decision: Domain behavior will be verified with unit tests; Application behavior with unit and use-case tests; Infrastructure adapters with integration tests; API behavior with API/integration tests; and frontend behavior with component/integration tests.
- Reason: Assigning tests to the layer that owns the behavior preserves the Clean Architecture boundaries while still verifying cross-layer integrations at their external seams.
- Alternatives considered: relying exclusively on end-to-end tests; testing all behavior through API calls; assigning one test type uniformly to every layer.
- Consequences: Inner-layer rules can be tested independently and quickly, while technical integrations and externally observable behavior are verified at their respective boundaries. The initial .NET test framework and test-project layout are selected in Decision 15; coverage targets and test-environment tooling remain deferred.

## Decision 14 — Docker-based development and GitHub Actions CI direction

- Decision: Docker is the standard containerization approach; PostgreSQL will run in Docker for local development; the ASP.NET Core API and Angular frontend will be containerized at a high level for consistent packaging. GitHub Actions is the planned CI/CD platform, with restore, build, and automated tests as minimum CI gates required to pass before merge.
- Reason: Reproducible local dependencies and packaged application components reduce environment drift, while automated pull-request validation helps prevent broken changes from entering the mainline.
- Alternatives considered: relying on manually installed local database services and environment-specific application setups; selecting a CI/CD platform only after implementation begins; allowing failed or missing CI checks to merge.
- Consequences: Local development, CI validation, and deployment are treated as separate concerns. Repository configuration must require the agreed CI checks before merge. Secrets and environment-specific sensitive configuration must stay out of source control and be supplied through appropriate environment mechanisms. No container files, workflows, deployment resources, or cloud-provider choice are introduced by this decision. Hosting, registry, release, rollout, operations, and concrete tool configuration remain deferred.

## Decision 15 — xUnit as the initial .NET test framework

- Decision: The initial .NET test projects will use xUnit with the Microsoft test SDK and Visual Studio test adapter.
- Reason: A runnable test foundation requires a test framework and runner; xUnit's .NET test template provides the minimal setup used for this phase.
- Alternatives considered: postponing the test framework choice, which would leave the requested projects unable to run tests; selecting another .NET test framework.
- Consequences: The four test projects can be discovered and executed by `dotnet test`. Optional coverage tooling and integration-hosting packages are not included until a concrete coverage or host-based integration test requires them.

## Decision 16 — Category and Product persistence mapping

- Decision: Infrastructure persists Category and Product with EF Core 10 and PostgreSQL through Npgsql. Category names are constrained case-insensitively using a stored generated lowercase key and unique index; ProductIdentifier is stored as a required string with an exact unique index. Products require a Category foreign key with `NO ACTION` delete behavior.
- Reason: The storage model must enforce catalog uniqueness even under concurrent writes while keeping EF Core and PostgreSQL concerns out of Domain and Application. Restrictive deletion preserves product records rather than cascading their removal.
- Alternatives considered: Relying on Application pre-checks alone; case-sensitive category-name uniqueness; cascading product deletion; storing the identifier as a primitive in Domain.
- Consequences: Category names are limited to 200 characters and identifiers to 100 characters for indexed persistence; exact business-level length limits remain unspecified. A category cannot be physically deleted while any product references it, including inactive products. Application pre-checks remain for friendly errors, while database indexes/constraints are authoritative. Integration tests run against an isolated PostgreSQL 17 Testcontainers instance. Production connection settings are supplied externally; migration deployment operations remain deferred.

## Decision 17 — Catalog HTTP boundary and error representation

- Decision: The API exposes the current Category and Product use cases through ASP.NET Core Minimal API route groups, maps to API-owned request/response DTOs, and represents validation, not-found, and conflict outcomes using Problem Details. Infrastructure is registered once at API startup from the externally supplied `ConnectionStrings:InventoryDatabase` setting; database migrations are not run automatically by the API.
- Reason: HTTP must remain a transport adapter over Application use cases, without exposing Domain entities, accessing repositories directly, or leaking database details. Constraint violations still need an appropriate conflict response when persistence is authoritative.
- Alternatives considered: returning Domain entities, duplicating business validation in endpoints, returning raw database exceptions, or applying migrations as a startup side effect.
- Consequences: Catalog endpoints return 201 for creation, 200 for queries/updates, 204 for deletion, 400 for invalid input, 404 for missing resources, and 409 for conflicts. PostgreSQL unique and foreign-key violations are logged and mapped to a generic 409 response; unexpected failures are logged and returned as generic 500 Problem Details with a trace identifier. The Development environment exposes OpenAPI. Authentication, authorization, user identity, and other resource contracts remain deferred.

## Decision 18 — User identity and authentication application boundary

- Decision: The Domain represents a user with a normalized, case-insensitive email identity, required name, active state, immutable password-hash representation, and a set of roles. Initial roles are `User` (the ordinary User/Client role) and `Admin`; new users default to `User`, start inactive, and must retain at least one role. Application defines `IUserRepository` and `IPasswordHasher`, hashes supplied credentials before constructing a User, and returns an application authentication outcome for authenticated, inactive, or invalid-credential cases. Missing users and wrong passwords share the invalid-credentials outcome; inactivity is reported only after valid credential verification.
- Reason: This supplies the minimum domain and use-case boundary for later authentication while preserving framework independence, least privilege, non-plaintext credential storage, and the documented user accountability model.
- Alternatives considered: storing raw passwords, placing hashing logic in Domain, allowing active accounts by default, returning distinct public outcomes for missing users and bad passwords, or introducing token/authentication framework dependencies before the JWT phase.
- Consequences: At this decision point, the concrete hashing strategy and user persistence were deferred; Decision 19 subsequently implemented both in Infrastructure. The implementation must use an established adaptive password-hashing implementation and no custom cryptography. Email uniqueness is enforced by a database constraint because Application pre-checks alone do not prevent concurrent duplicates. User registration and role assignment must be exposed only through authorization-appropriate operations in a later API phase. JWT issuance/validation, authorization enforcement, password policy, account recovery, and audit implementation remain deferred.

## Decision 19 — User persistence and adaptive password hashing

- Decision: Infrastructure persists users in PostgreSQL `users`, mapping the normalized email to a required 320-character column with unique index `ux_users_email`, and mapping user ID, name, active state, password hash, and roles. Roles are persisted as a JSONB array of numeric `UserRole` values and tracked as a value collection. Password hashing uses ASP.NET Core Identity's `PasswordHasher<TUser>` from `Microsoft.Extensions.Identity.Core` 10.0.12, with the versioned Identity V3 format using salted, adaptive PBKDF2-HMAC-SHA512; password verification uses the same framework implementation.
- Reason: The database must durably prevent duplicate login identities, and credentials require a supported password-specific adaptive hash without introducing custom cryptography or moving security implementation into Domain/Application.
- Alternatives considered: storing plaintext or reversible credentials; implementing a custom PBKDF2 routine; adding a separate role-permission subsystem or role tables beyond the current small enum model; using catalog duplicate pre-checks without a database constraint.
- Consequences: The migration `20261008032603_AddUsers` adds only the users table and email unique index; no Category/Product mapping changes are included. `AddPersistence` registers the repository and hasher. Framework-generated hashes contain random salt and version/parameter information; verification accepts both normal success and the framework's success-needing-rehash result. Automatic rehash-on-login is not wired because the current Application hasher contract returns only a Boolean; rehash migration/configuration remains deferred. User data migration, JWT/API authentication, authorization, account recovery, and audit remain later work. The EF CLI in the environment is 10.0.11 while the runtime is 10.0.12.

## Decision 20 — JWT bearer authentication and current-user boundary

- Decision: The API exposes `POST /auth/register`, `POST /auth/login`, and protected `GET /auth/me`. ASP.NET Core JWT bearer authentication uses `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12; Infrastructure issues tokens with `System.IdentityModel.Tokens.Jwt` 8.14.0, using HS256 and a 15-minute default lifetime (configuration may set 1–60 minutes). Tokens contain `sub` (User ID), `jti`, and `role` claims. The API requires configured issuer, audience, and an externally supplied signing key with at least 32 UTF-8 bytes; bearer validation requires the signature, issuer, audience, signed token, valid subject, and unexpired lifetime, allowing 30 seconds of clock skew. Application exposes `IAccessTokenIssuer` and `ICurrentUser` without JWT or ASP.NET Core dependencies.
- Reason: Authentication must use the existing Application registration and credential-validation workflows, keep token mechanics in Infrastructure, and make only validated identity available to API/Application consumers. The backend must not rely on frontend state or caller-supplied claims.
- Alternatives considered: accepting tokens without signature validation; omitting issuer/audience checks; embedding passwords or hashes in claims; putting HttpContext or JWT library types in Application; activating all registrations automatically; implementing role policies before the authorization phase.
- Consequences: At Phase 1.9, registration returned a user ID and inactive state but did not issue a token; existing Domain behavior keeps newly registered users inactive. Login returned no token for invalid credentials or inactive users, with both cases mapped to the same generic 401 response. Duplicate registration maps to 409 and malformed request data to 400. Catalog authorization was added separately in Decision 21. Refresh tokens, signing-key provisioning/rotation, frontend token storage, activation, abuse protections, and audit remain deferred.

## Decision 21 — Initial role-based catalog authorization

- Decision: The API applies authentication to every Category and Product route. Any authenticated `User` or `Admin` may read/list catalog data; an `AdminOnly` policy is required to create, update, or remove catalog records. The policy uses the existing validated JWT `role` claims and ASP.NET Core role authorization. `/auth/register` and `/auth/login` remain public, and `/auth/me` remains authenticated.
- Reason: Catalog reads support both ordinary business users and administrators, while catalog maintenance is an administrative responsibility. The split protects business data from anonymous access without treating every endpoint as Admin-only.
- Alternatives considered: leaving catalog routes public; requiring Admin for every catalog operation; adding per-operation permission names or a permission database; trusting role values from request payloads or frontend state.
- Consequences: Missing/invalid authentication returns 401, while an authenticated non-Admin attempting a catalog mutation receives 403 Problem Details. Multiple `role` claims are supported; a caller cannot elevate by submitting a role in registration data. No Domain/Application authorization dependencies or new current-user abstraction were added. User activation, initial Admin provisioning, user/role management, fine-grained permissions, and authorization for future inventory/sales/customer/supplier/report operations remain deferred. New accounts continue to default inactive, and public registration is retained from Phase 1.9.
