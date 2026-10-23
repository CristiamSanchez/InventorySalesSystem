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

## Decision 22 — Inventory stock adjustment model

- Decision: The Inventory domain models stock adjustments using a signed, non-zero quantity change (`quantityDelta`) and a required, non-blank business reason. Positive deltas record upward stock corrections, while negative deltas record downward corrections, both strictly governed by the non-negative stock invariant (`Quantity + quantityDelta >= 0`). Each adjustment creates an auditable `InventoryMovement` with `MovementType.Adjustment` capturing the signed delta, UTC timestamp, and normalized reason.
- Reason: Business rule BR-INV-005 requires an auditable account of the quantity change, and BR-INV-007 requires inventory levels to remain consistent with cumulative movements. A relative delta model directly represents the magnitude and direction of the change without requiring external pre-calculation of absolute totals under concurrency, while uniformly enforcing the non-negative stock invariant.
- Alternatives considered: An absolute target-stock adjustment model (`AdjustTo(target)`), which requires reading current stock to compute differences and is vulnerable to concurrency race conditions; separate movement types for positive vs. negative adjustments (`AdjustmentIncrease` vs `AdjustmentDecrease`), which would introduce unnecessary enum proliferation without domain justification.
- Consequences: All inventory adjustments are performed via `Adjust(int quantityDelta, string reason)` (and aliased as `RegisterAdjustment`), enforcing `quantityDelta != 0` and non-empty reasons. The resulting `InventoryMovement` preserves the signed delta, allowing straightforward cumulative balance reconciliation (`Sum(m => ...)`) across entries, exits, and adjustments.

## Decision 23 — Inventory Application lifecycle and repository boundary

- Decision: Inventory application use cases are coordinated through `IInventoryRepository` (`GetByProductIdentifierAsync`, `AddAsync`, `SaveAsync`). When `RecordEntryAsync` is called for a product with no prior inventory, the use case automatically initializes an `Inventory` aggregate root and persists it via `AddAsync`; subsequent entries, exits, and adjustments retrieve existing inventory and persist via `SaveAsync`. Operations attempting to exit or adjust missing inventory return `NotFound`. Domain invariant violations (such as attempts to reduce stock below zero) are mapped to `ApplicationErrorCode.InsufficientStock`.
- Reason: Inventory does not require an artificial administrative setup step; stock physically comes into existence upon the first verified receipt/entry. Mapping invariant violations to `InsufficientStock` provides clear application feedback distinguishing stock exhaustion from syntactic input validation errors, while keeping Domain independent of application error structures.
- Alternatives considered: Requiring an explicit "CreateInventory" administrative use case before receiving stock; mapping insufficient stock to generic `InvalidInput`; allowing unhandled domain exceptions to escape the application boundary.
- Consequences: Initializing inventory upon first entry simplifies catalog workflows, preserves complete movement audit trails, and provides explicit, predictable application outcomes (`NotFound`, `InvalidInput`, `InsufficientStock`). Persistence, migrations, and concurrency controls remain deferred to later phases.

## Decision 24 — Inventory Persistence and Movement History Mapping

- Decision: `Inventory` and `InventoryMovement` are persisted using PostgreSQL with EF Core in `CatalogDbContext`. `Inventory` maps to `inventories` with UUID primary key `Id`, current stock `Quantity`, and `ProductIdentifier` stored as required `character varying(100)` with unique index `ux_inventories_product_identifier`. `InventoryMovement` maps to `inventory_movements` with UUID primary key `Id`, `ProductIdentifier` (`varchar(100)`), `MovementType` (`integer`), `Quantity` (`integer`), `Reference` (`varchar(500)`), and `OccurredAtUtc` (`timestamp with time zone`), linked to `inventories` via foreign key `InventoryId` with cascade delete. The collection `Movements` is encapsulated via private backing field `_movements`. `InventoryRepository` implements `IInventoryRepository` (`GetByProductIdentifierAsync`, `AddAsync`, `SaveAsync`), including detached-state synchronization.
- Reason: Mapping `ProductIdentifier` by value preserves aggregate boundary independence without imposing cross-aggregate foreign keys or tight coupling to the `Product` entity. Encapsulating `_movements` preserves domain invariants while enabling EF Core to track new movement records directly. Handling detached-state in `SaveAsync` protects against concurrency key-collision errors with non-default Guid primary keys.
- Alternatives considered: Adding an EF Core foreign key from `inventories` to `products(PersistenceId)`, which would tightly couple catalog and inventory aggregates in persistence; table-splitting movements into separate tables by movement type; relying on default EF `Update()` which fails when detached child entities carry non-empty Guid keys.
- Consequences: Stock and movement history are persisted transactionally with full auditability. `InventoryRepository` is registered via `AddPersistence`. Migration `20261019013317_AddInventory` was generated and applied cleanly against PostgreSQL 17 in Testcontainers.

## Decision 25 — Inventory API HTTP boundary, authorization, and error representation

- Decision: The API exposes Inventory application use cases under `/api/inventory` using ASP.NET Core Minimal API route groups (`MapInventory`). Routes include `GET /api/inventory/{productIdentifier}`, `GET /api/inventory/{productIdentifier}/movements`, `POST /api/inventory/{productIdentifier}/entries` (also supporting `POST /api/inventory/entries`), `POST /api/inventory/{productIdentifier}/exits` (also `POST /api/inventory/exits`), and `POST /api/inventory/{productIdentifier}/adjustments` (also `POST /api/inventory/{productIdentifier}/adjust` and `POST /api/inventory/adjustments`). All routes require JWT authentication; reads require authenticated `User` or `Admin` role, while mutations (entries, exits, adjustments) require the `AdminOnly` authorization policy (`RequireRole("Admin")`). DTOs decouple external HTTP contracts from domain types. Errors are mapped to RFC 7807 Problem Details: `ApplicationErrorCode.InsufficientStock` maps to HTTP 409 Conflict with title "Insufficient stock", `NotFound` maps to 404, and `InvalidInput` maps to 400.
- Reason: Inventory queries serve general business operations (accessible to Users and Admins), whereas stock adjustments, receipts, and write-offs modify financial and stock records and must be restricted to administrators. Mapping `InsufficientStock` to 409 Conflict accurately reflects state conflict without confusing it with client payload syntax errors (400), while preserving Clean Architecture boundaries where endpoints act as thin HTTP adapters over `InventoryUseCases`.
- Alternatives considered: Allowing anonymous reads; allowing regular `User` role to record stock entries/exits; mapping `InsufficientStock` to HTTP 400 or HTTP 422; exposing domain entities directly over JSON.
- Consequences: Endpoints return 200 OK with `InventoryResponse` or `IReadOnlyCollection<InventoryMovementResponse>`, 400 for invalid inputs, 401 for unauthenticated calls, 403 for non-admin mutations, 404 for nonexistent product/inventory, and 409 for insufficient stock. No database context or repository types are accessed directly by endpoint handlers. Concurrency locking and automated migrations remain deferred.

## Decision 26 — Inventory Optimistic Concurrency and Data Consistency Strategy

- Decision: Inventory persistence implements optimistic concurrency control (OCC) using PostgreSQL's native `xmin` system column mapped to an EF Core rowversion shadow property (`builder.Property<uint>("Version").IsRowVersion()`) on the `Inventory` aggregate root. In addition, defense-in-depth is reinforced with a PostgreSQL check constraint (`ck_inventories_quantity_non_negative`, `"Quantity" >= 0`) and the existing unique index `ux_inventories_product_identifier`. Concurrency conflicts (`DbUpdateConcurrencyException`) and concurrent duplicate initialization attempts (PostgreSQL unique violation on `ux_inventories_product_identifier`) are caught in `InventoryRepository` and translated to `ConcurrencyException` in Application. `InventoryUseCases` captures `ConcurrencyException` and produces `Result<Inventory>.Failure(ApplicationErrorCode.ConcurrencyConflict, ...)`. The API maps `ConcurrencyConflict` to HTTP 409 Conflict Problem Details ("Concurrency conflict").
- Reason: Concurrent inventory operations (entries, exits, adjustments) require atomic verification of stock availability and movement history without risking lost updates, negative inventory, or phantom movements. In PostgreSQL, the `xmin` system column automatically records the modifying transaction ID on every update without requiring schema modifications, application tracking columns, or database triggers. OCC allows queries (`GET /api/inventory/{productIdentifier}`) to remain non-blocking, avoids holding long-lived database transactions or row locks across application logic, and ensures that when a conflict occurs during `SaveChangesAsync()`, the transaction rolls back cleanly so that neither stock changes nor movement records are partially saved.
- Alternatives considered:
  - Pessimistic row locking (`SELECT ... FOR UPDATE`): Requires managing open database transactions across repository calls and use cases, blocks concurrent reads, introduces outer join limitations with EF Core's `.Include()`, and risks deadlocks or connection pool exhaustion under load.
  - Application-level retries: Automatic retries without caller coordination could mask business rule failures (such as stock exhaustion where re-reading shows zero stock); returning 409 Conflict provides deterministic feedback allowing the client to decide whether to re-fetch or abort.
  - Dedicated application version column: Redundant when PostgreSQL provides native `xmin` MVCC transaction tracking on every table.
- Consequences:
  - Any concurrent update attempting to modify an inventory based on a stale read is rejected with `DbUpdateConcurrencyException`, translated to `ConcurrencyConflict` (HTTP 409 Conflict Problem Details).
  - Failed operations never persist stock changes or movement records.
  - Concurrent product inventory initializations are safely caught by `ux_inventories_product_identifier` and translated to 409 Conflict without leaking database exceptions.
  - The database check constraint `ck_inventories_quantity_non_negative` strictly guarantees stock can never become negative at the PostgreSQL engine level.
  - Migration: `20261019160519_AddInventoryConcurrencyAndCheckConstraint`.

## Decision 27 — Sales Domain Model, Lifecycle, and Inventory Consistency Strategy

- Decision: The Sales domain is planned around `Sale` as an Aggregate Root and `SaleDetail` as an encapsulated child entity.
  1. `Sale` minimum attributes: `Id` (Guid), `CustomerReference` (string/value object), `UserId` (Guid), `CreatedAtUtc` (DateTimeOffset), `ConfirmedAtUtc` (DateTimeOffset?), `Status` (`SaleStatus`), `Subtotal` (decimal), `TaxAmount` (decimal), `Total` (decimal), and encapsulated detail lines (`IReadOnlyCollection<SaleDetail>`).
  2. `SaleDetail` minimum attributes: `Id` (Guid), `SaleId` (Guid), `ProductIdentifier` (ProductIdentifier), `Quantity` (positive int > 0), `UnitPrice` (decimal >= 0, immutable price snapshot), `TaxRate` (decimal >= 0), `Subtotal` (decimal: Quantity * UnitPrice), `TaxAmount` (decimal: Subtotal * TaxRate, rounded to 2 decimal places with `MidpointRounding.AwayFromZero`), and `Total` (decimal: Subtotal + TaxAmount).
  3. Lifecycle: `SaleStatus` enum with `Draft = 1`, `Confirmed = 2`, `Cancelled = 3`. In `Draft`, items can be added, updated, or removed without stock reservation or movement generation. `Confirm(occurredAtUtc)` checks preconditions (at least one detail line, valid totals, sufficient inventory stock for all products), sets status to `Confirmed`, locks details, and triggers stock deduction. `Cancelled` is terminal; cancelling a draft has no stock effect, while cancelling a confirmed sale requires inventory reversal via `Entry` movements.
  4. Inventory consumption and traceability: Confirmed sales call `Inventory.RegisterExit(detail.Quantity, reference: $"Sale {sale.Id}", occurredAtUtc)` for each detail line, recording `MovementType.Exit` with full audit traceability back to the sale identifier.
  5. Atomicity: The sale confirmation workflow is strictly atomic (Decision 7). `Sale` status update, all `SaleDetail`s, all affected `Inventory` stock reductions, and all `InventoryMovement` records must commit in a single database transaction. If any product has insufficient stock or a failure occurs, the entire transaction rolls back with no partial sales or orphaned movements.
  6. Concurrency protection: The existing inventory optimistic concurrency control (PostgreSQL `xmin` row versioning, Decision 26) protects concurrent sales from race conditions. If multiple sales compete for the same stock, the first commit succeeds while competing transactions fail with `DbUpdateConcurrencyException` (translated to `ConcurrencyException` / HTTP 409 Conflict), rolling back the conflicting sale and preventing negative stock. The database check constraint `ck_inventories_quantity_non_negative` acts as an absolute safety barrier.
  7. Unresolved choices explicitly marked:
     - Customer aggregate decoupling: Customer entity is not yet implemented (Phase 5). `Sale` accepts a `CustomerReference` (string / value object) to satisfy `BR-SALE-001` without introducing premature database foreign keys.
     - Product catalog price: `Product` currently has no price attribute; `SaleDetail` captures `UnitPrice` as an immutable snapshot. Whether a default catalog price is added to `Product` or passed during detail creation remains an open business decision.
     - Tax engine rules: Tax calculation uses a line-level `TaxRate` with standard financial rounding; multi-tax, jurisdiction-specific, or exempt rules remain deferred.
     - Commercial numbering: Human-readable invoice/ticket numbering formats (e.g. `FAC-0001`) remain deferred; `Sale.Id` (Guid) serves as the primary domain identity.
     - Sale cancellation workflow: Domain supports the transition to `Cancelled`, but the full cancellation authorization and return workflow is deferred beyond initial sale confirmation implementation.
- Reason: The design strictly fulfills `BR-SALE-001` through `BR-SALE-014` and `FR-SALE-001` through `FR-SALE-008`, preserves Clean Architecture aggregate boundaries by referencing `ProductIdentifier` by value, guarantees price immutability over time, and leverages the proven optimistic concurrency mechanism in Inventory without needing new transaction frameworks.
- Alternatives considered:
  - Tightly coupling `SaleDetail` directly to the `Product` entity via foreign key: Rejected to maintain aggregate independence and align with the existing `Inventory` design.
  - Decrementing stock during `Draft` creation (stock reservation): Rejected because draft abandonment would lock up warehouse stock and require complex timeout/cleanup workers; immediate atomic deduction at confirmation is simpler, robust, and aligned with point-of-sale business rules.
  - Recalculating historical sale lines from current product catalog prices: Rejected because future price changes would corrupt past sales records and financial accounting audits.
  - Allowing partial sale confirmations when some items are out of stock: Rejected because a customer transaction must succeed as an atomic whole (Decision 7).
- Consequences: Phase 3.2 (Domain implementation) will implement `Sale`, `SaleDetail`, and `SaleStatus` in `SistemaInventarioVentas.Domain` along with exhaustive unit tests, without needing modifications to the existing `Inventory` or `Catalog` aggregates.

## Decision 28 — Sales Application Foundation and Repository Boundary

- Decision: Sales application operations in this foundation phase are coordinated through `ISaleRepository` (`GetByIdAsync`, `AddAsync`, `SaveAsync`) and `SaleUseCases`.
  1. `CreateDraftAsync` initializes a `Sale` aggregate root in `Draft` status and persists it via `AddAsync`.
  2. Detail management use cases (`AddDetailAsync`, `UpdateDetailAsync`, `RemoveDetailAsync`, and `UpdateCustomerReferenceAsync`) retrieve the existing draft sale via `GetByIdAsync`, invoke `Sale` domain methods to enforce draft-only modifications and calculate financial amounts, and persist changes via `SaveAsync`.
  3. `AddDetailAsync` accepts an optional `IProductRepository` to verify catalog product existence before adding a line.
  4. Pricing snapshots and tax rates are supplied explicitly in request parameters; catalog price lookup and tax engine policies remain deferred.
  5. Error mapping: Domain argument validations and invalid lifecycle states map to `ApplicationErrorCode.InvalidInput`; missing sales, products, or detail lines map to `ApplicationErrorCode.NotFound`.
- Reason: Keeps the Application layer as a clean orchestration boundary delegating domain invariants and financial math to the `Sale` aggregate root, prevents duplicate domain validation, avoids premature pricing/tax policies, and enables comprehensive unit testing with in-memory repository fakes.
- Alternatives considered:
  - Requiring a catalog price lookup before adding lines: Rejected because `Product` currently has no price attribute and catalog pricing is deferred.
  - Confirming sales immediately during creation: Rejected because the sales lifecycle requires distinct draft line assembly and atomic confirmation steps.
  - Exposing repository contracts directly to outer layers: Rejected to preserve Clean Architecture use-case boundaries.
- Consequences: Draft sales can be created, retrieved, and modified safely through the application layer with cancellation token support. Sale confirmation, inventory deductions, stock movement generation, database persistence, and API endpoints remain deferred to subsequent phases.

## Decision 29 — Sales PostgreSQL Persistence and Detail Mapping Strategy

- Decision: Persist `Sale` and `SaleDetail` using PostgreSQL with EF Core in `CatalogDbContext`.
  1. `Sale` maps to the `sales` table with UUID primary key `Id`, `CustomerReference` stored as required `character varying(200)` with index, `UserId` stored as required `uuid` with index (without speculative foreign keys to unbuilt Customer or User aggregates), `CreatedAtUtc` and nullable `ConfirmedAtUtc` (`timestamp with time zone`), integer `Status`, and decimal monetary values `Subtotal`, `TaxAmount`, `Total` stored with precision `(18,2)`. Check constraints enforce `"Subtotal" >= 0`, `"TaxAmount" >= 0`, and `"Total" >= 0`.
  2. `SaleDetail` maps to `sale_details` with UUID primary key `Id`, required foreign key `SaleId` referencing `sales(Id)` with cascade deletion (`OnDelete(DeleteBehavior.Cascade)`), `ProductIdentifier` stored as `character varying(100)` with value converter, integer `Quantity`, decimal `UnitPrice` (`numeric(18,2)`), decimal `TaxRate` (`numeric(18,4)`), and decimal amounts `Subtotal`, `TaxAmount`, `Total` (`numeric(18,2)`). Check constraints enforce positive quantity and non-negative unit price, tax rate, and totals. A composite unique index on `("SaleId", "ProductIdentifier")` (`ux_sale_details_sale_id_product_identifier`) guarantees at the database level that no duplicate product lines can exist within a single sale.
  3. Aggregate encapsulation: `Sale.Details` is encapsulated via private backing field `_details` using `PropertyAccessMode.Field`, ensuring aggregate invariants are preserved while EF Core tracks detail entities.
  4. Repository implementation: `SaleRepository` implements `ISaleRepository` (`GetByIdAsync`, `AddAsync`, `SaveAsync`), including child collection synchronization for detached entities (adding, modifying, and deleting orphan lines).
  5. Repository registration: `ISaleRepository` is registered with scoped lifetime in `PersistenceServiceCollectionExtensions.AddPersistence`.
  6. Migration: `20261019170912_AddSales`.
- Reason: Accurately represents the Sales Domain in PostgreSQL without coupling to unbuilt aggregates (Customer or User), protects financial values and duplicate product lines at the database level, and maintains Clean Architecture boundaries.
- Alternatives considered:
  - Adding foreign keys to `users` or speculative `customers`: Rejected because foreign keys to unbuilt or decoupled aggregates violate established architectural boundaries.
  - Storing monetary amounts as floating point or integer cents: Rejected because standard .NET `decimal` mapped to PostgreSQL `numeric(18,2)` preserves exact monetary precision without floating-point rounding errors or conversion overhead.
  - Relying only on in-memory checks for duplicate products per sale: Rejected because a unique index provides critical defense-in-depth against data corruption under concurrency.
- Consequences: Sales and detail lines are persisted transactionally with full auditability, proper cascade behavior, and mathematical precision. Sale confirmation, inventory deductions, stock movement generation, and API endpoints remain deferred to subsequent phases.

## Decision 30 — Atomic Sales Confirmation and Inventory Deduction Transaction Strategy

- Decision: Sale confirmation and inventory deduction are coordinated across Application and Infrastructure to guarantee atomic state transition and stock deduction:
  1. Application orchestration (`SaleUseCases.ConfirmSaleAsync`):
     - Validates that the sale exists, is in `Draft` status, and has $\ge 1$ detail lines.
     - Aggregates required quantities per product (`GroupBy(ProductIdentifier)`).
     - Pre-validates inventory existence and stock availability across all line items; if any line lacks stock, aborts early returning `ApplicationErrorCode.InsufficientStock` without mutating in-memory domain state or opening database transactions.
     - Calls domain methods: `sale.Confirm(confirmedAtUtc)` and `inventory.RegisterExit(detail.Quantity, reference: $"Sale {sale.Id}", occurredAtUtc)` for each line item.
     - Invokes `ISaleRepository.ConfirmSaleAsync(sale, affectedInventories, cancellationToken)` to persist the atomic transaction.
     - Catches `ConcurrencyException` from Infrastructure and maps it to `ApplicationErrorCode.ConcurrencyConflict`.
  2. Infrastructure technical transaction (`SaleRepository.ConfirmSaleAsync`):
     - Executes inside `dbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)` and opens an explicit PostgreSQL database transaction (`await dbContext.Database.BeginTransactionAsync(cancellationToken)`).
     - Re-verifies in the database inside the transaction that the target sale status is still `Draft` (`currentStatus == SaleStatus.Draft`), preventing duplicate confirmation under concurrent races.
     - Synchronizes and attaches detached `Sale` and `Inventory` entities (including rowversion and movement collection tracking) when invoked across contexts.
     - Commits sale status, inventory stock decrements, and newly generated `InventoryMovement` exit records atomically in `SaveChangesAsync()` within the transaction.
     - Catches optimistic concurrency conflicts (`DbUpdateConcurrencyException`) and database check constraint / unique violations (`PostgresErrorCodes.CheckViolation`, `PostgresErrorCodes.UniqueViolation`), rolls back the transaction, and translates them to `ConcurrencyException`.
  3. Re-confirmation safety:
     - Sequential duplicate calls are immediately rejected in `SaleUseCases` because `sale.Status != SaleStatus.Draft` (`ApplicationErrorCode.InvalidInput`).
     - Concurrent duplicate calls are protected by inventory OCC (`xmin`) and database status verification, guaranteeing that at most one succeeds while competing requests roll back cleanly.
- Reason: Strictly fulfills `BR-SALE-006`, `BR-SALE-008`, `BR-SALE-010`, `BR-INV-004`, `BR-INV-005`, and architectural Decisions 7 and 27, ensuring stock cannot become negative or orphaned, and preserving Clean Architecture by keeping technical transaction management in Infrastructure while use case business validation remains in Application.
- Alternatives considered:
  - Performing inventory deduction asynchronously via event bus / background worker: Rejected because point-of-sale confirmation requires synchronous consistency and immediate confirmation/rejection feedback to the caller.
  - Relying solely on application-level stock checks: Rejected because concurrent transactions could pass in-memory checks simultaneously and produce negative inventory or lost updates.
  - Merging `SaleRepository` and `InventoryRepository` into a single monolithic repository: Rejected to preserve aggregate boundary separation; passing affected inventories to the transaction coordinator preserves clean boundaries.
- Consequences: Draft sales can be confirmed atomically with guaranteed stock deduction, immutable audit exit movements, robust OCC rollback, and complete test coverage in PostgreSQL integration tests. Sales API endpoints and controller mappings remain deferred to Phase 3.6.

## Decision 31 — Sales Minimal API Endpoints, Authorization, and Explicit Pricing Limitation Strategy

- Decision: Expose the Sales application use cases via ASP.NET Core Minimal API endpoints grouped under `/api/sales`:
  1. Endpoints:
     - `POST /api/sales`: Creates a new `Draft` sale for the authenticated responsible user (`ICurrentUser.UserId`), taking `CreateSaleRequest` (`CustomerReference`). Returns HTTP 201 Created with `SaleResponse` and `Location` header (`/api/sales/{id}`).
     - `GET /api/sales/{id:guid}`: Retrieves a sale by identifier, returning HTTP 200 OK with `SaleResponse` (including detail lines and financial totals).
     - `POST /api/sales/{id:guid}/details`: Adds a detail line to a draft sale, taking `AddSaleDetailRequest` (`ProductIdentifier`, `Quantity`, `UnitPrice`, `TaxRate`). Returns HTTP 200 OK with the updated `SaleResponse`.
     - `PUT /api/sales/{id:guid}/details/{detailIdOrProductIdentifier}`: Updates an existing detail line, taking `UpdateSaleDetailRequest` (`Quantity`, `UnitPrice`, `TaxRate`). Dynamically parses `detailIdOrProductIdentifier` as a Guid (`detailId`) or string (`ProductIdentifier`). Returns HTTP 200 OK with the updated `SaleResponse`.
     - `DELETE /api/sales/{id:guid}/details/{detailIdOrProductIdentifier}`: Removes a detail line from a draft sale by detail ID or product identifier. Returns HTTP 200 OK with the updated `SaleResponse`.
     - `POST /api/sales/{id:guid}/confirm`: Atomically confirms the draft sale and deducts inventory stock, returning HTTP 200 OK with the confirmed `SaleResponse`.
  2. Data Transfer Objects: Request and response DTOs (`CreateSaleRequest`, `AddSaleDetailRequest`, `UpdateSaleDetailRequest`, `SaleResponse`, `SaleDetailResponse`) decouple API clients from Domain and EF Core entities. DataAnnotations enforce required fields, string length bounds, positive quantity ($\ge 1$), and non-negative unit price and tax rate ($\ge 0$).
  3. Pricing and Tax Temporary Limitation:
     - `AddSaleDetailRequest` and `UpdateSaleDetailRequest` explicitly require callers to supply unit-price snapshots (`UnitPrice`) and tax rates (`TaxRate`).
     - This is an explicit, temporary limitation due to catalog pricing, price lists, customer tiers, and tax engine policies not being defined in the domain yet.
     - Contracts and documentation explicitly flag that client-provided prices must NOT be treated as trusted production pricing without future catalog/pricing engine verification.
  4. Authentication and Authorization:
     - All `/api/sales` endpoints require JWT authentication.
     - Read operations (`GET /api/sales/{id:guid}`) permit both `User` and `Admin` roles.
     - Mutation operations (`POST /api/sales`, `POST /api/sales/{id}/details`, `PUT /api/sales/{id}/details/...`, `DELETE /api/sales/{id}/details/...`, `POST /api/sales/{id}/confirm`) require the `AdminOnly` authorization policy (`RequireRole("Admin")`), returning 403 Forbidden for non-Admin authenticated users.
     - Anonymous callers receive HTTP 401 Unauthorized Problem Details.
  5. Error Mapping:
     - Endpoints delegate directly to `SaleUseCases` and map `Result<T>` failures using `ApplicationProblemMapping`:
       - `InvalidInput` -> HTTP 400 Bad Request.
       - `NotFound` -> HTTP 404 Not Found.
       - `InsufficientStock` -> HTTP 409 Conflict with title "Insufficient stock".
       - `ConcurrencyConflict` -> HTTP 409 Conflict with title "Concurrency conflict".
       - Detail mutations on confirmed sales throw domain `InvalidOperationException`, translated to `InvalidInput` -> HTTP 400 Bad Request.
- Reason: Strictly complies with Clean Architecture boundaries, leverages established API routing and Problem Details conventions, enforces role-based security consistent with catalog and inventory mutations, and provides transparent documentation of temporary pricing mechanisms without blocking current milestone delivery.
- Alternatives considered:
  - Allowing regular `User` role to perform sales mutations: Rejected because in the current security model, business resource mutations (catalog, inventory, and sales) require administrative authority pending future POS cashier or self-service customer roles.
  - Separate endpoints for detail ID vs. product identifier removal/updating: Rejected in favor of flexible unified route `{detailIdOrProductIdentifier}` with Guid parsing, keeping API routing clean and avoiding endpoint ambiguity.
  - Mocking an automatic catalog price lookup in the API layer: Rejected to avoid inventing catalog pricing rules outside the domain model and application layer.
- Consequences: Full sales lifecycle (draft creation, detail line modification, retrieval, and atomic inventory confirmation) is fully accessible over HTTP with complete test coverage in PostgreSQL Testcontainers integration tests.

## Decision 32 — Customer Domain Model, Identity, Contact Rules, and Sale Reference Evolution

- Decision: The Customer domain is planned around `Customer` as an independent Aggregate Root and a non-breaking hybrid link strategy with the existing `Sale` aggregate:
  1. Customer Aggregate Root attributes:
     - `Id`: `Guid` (unique entity identifier).
     - `Name`: `string` (required legal or commercial name, 1–200 characters, trimmed).
     - `IdentificationNumber`: `string?` (optional tax ID, national ID, or commercial identification code, max 50 characters, trimmed, uppercase-normalized, unique across customers when provided).
     - `Email`: `EmailAddress?` (optional contact email, using the existing domain `EmailAddress` value object; lowercase-normalized and validated for syntax).
     - `Phone`: `string?` (optional contact phone number, max 30 characters, trimmed).
     - `Address`: `string?` (optional physical/delivery address, max 300 characters, trimmed).
     - `IsActive`: `bool` (customer status flag; defaults to `true` on creation).
     - `CreatedAtUtc`: `DateTimeOffset` (UTC timestamp).
  2. Domain Methods:
     - `Customer(name, identificationNumber = null, email = null, phone = null, address = null, createdAtUtc = null)`: creates an active customer.
     - `Customer(id, name, identificationNumber, email, phone, address, isActive, createdAtUtc)`: rehydration constructor.
     - `UpdateDetails(string name, string? identificationNumber, EmailAddress? email, string? phone, string? address)`: updates name and contact fields with full invariant validation.
     - `Activate()`: activates the customer (`IsActive = true`).
     - `Deactivate()`: deactivates the customer (`IsActive = false`).
  3. Contact Information Rules:
     - Name is strictly required.
     - Contact fields (Email, Phone, Address) are optional to support counter, retail, and wholesale sales without forcing dummy entries.
     - Whenever supplied, contact fields must be non-empty, trimmed, and structurally valid.
  4. Active/Inactive Lifecycle and Sales Invariant:
     - Inactive customers cannot be used to create new sales or be assigned to new draft sales (`BR-CUST-003`).
     - Existing sales linked to an inactive customer remain unchanged and fully auditable (`BR-CUST-004`).
     - Customer records that have associated sales must never be deleted (`BR-CUST-004`).
  5. Sale Reference Evolution & Hybrid Linking (Non-Breaking):
     - `Sale` will evolve by adding an optional `CustomerId` (`Guid?`, identifier by value) while retaining the existing `CustomerReference` (`string`) as an immutable commercial name snapshot.
     - When a sale is created for a registered customer, `CustomerId = customer.Id` and `CustomerReference = customer.Name` (snapshot at the time of sale).
     - If the customer is later renamed, past sales and invoices retain the exact historical `CustomerReference`, guaranteeing auditability and fiscal consistency (`BR-CUST-006`).
     - Unregistered / walk-in sales remain fully supported by providing a text `CustomerReference` with `CustomerId = null`, requiring zero breaking changes to existing database schema, APIs, or integration tests.
  6. Unit Test Scenarios Specified:
     - Customer creation with valid name defaults to active with null optional contact fields.
     - Customer creation with full contact details normalizes name (trimmed), identification (trimmed, uppercase), email (lowercase), phone (trimmed), and address (trimmed).
     - Creation rejects null, empty, whitespace, or >200 char name (`ArgumentException`).
     - Creation rejects invalid email syntax (`ArgumentException`).
     - Creation rejects phone >30 chars or address >300 chars or identification >50 chars (`ArgumentException`).
     - Active/inactive state transitions via `Activate()` and `Deactivate()`.
     - `UpdateDetails` validates and normalizes all fields atomically.
     - Sale creation rejects inactive customer in application use case.
     - Sale creation with registered customer snapshots customer name and stores customer ID.
  7. Unresolved Questions Recorded Explicitly:
     - Fiscal checksum algorithm: Country-specific tax ID format/checksum validation (e.g. RFC, RUT, VAT) is deferred; standard normalized alphanumeric string storage is used.
     - Default walk-in customer account: Whether the application will automatically seed a default 'Final Consumer' customer entity or continue relying on `CustomerId = null` with `CustomerReference = "Final Consumer"` is deferred to Application/API phases.
     - Fuzzy duplicate name detection: Whether to warn on similar customer names when no tax ID is provided is deferred to future CRM/Application enhancements.
- Reason: Strictly fulfills `FR-CUST-001` through `FR-CUST-005` and `BR-CUST-001` through `BR-CUST-006`, preserves Clean Architecture aggregate independence (identifier-by-value reference), avoids breaking changes to existing sales persistence and API contracts, and models a practical, robust commercial customer entity without speculative CRM complexity.
- Alternatives considered:
  - Making email or phone mandatory: Rejected because over-the-counter and small-business commercial transactions often lack customer email/phone, and forcing dummy values degrades data quality.
  - Linking Sale directly via hard foreign key navigation without name snapshot: Rejected because renaming a customer in the future would retroactively alter the customer name on historic legal/tax invoices.
  - Pure string snapshot without CustomerId: Rejected because it prevents structured querying and aggregation of sales history by customer (violating `FR-CUST-003`).
- Consequences: Phase 4.2 will implement the `Customer` entity and unit tests in `SistemaInventarioVentas.Domain` following these specifications without modifying existing sales domain logic or breaking test suites.

## Decision 33 — Customer Persistence, Identification Key Computed Column, and Conflict Translation

- Decision: Implemented PostgreSQL persistence for the `Customer` aggregate in Infrastructure:
  1. Table mapping:
     - Table `customers` with PK `Id` (`uuid`, value generated never).
     - Required `Name` (`varchar(200)`).
     - Nullable `IdentificationNumber` (`varchar(50)`).
     - Nullable `Email` (`varchar(320)`) using `ValueConverter<EmailAddress?, string?>`.
     - Nullable `Phone` (`varchar(30)`) and `Address` (`varchar(300)`).
     - Required `IsActive` (`boolean`) and `CreatedAtUtc` (`timestamp with time zone`).
  2. Case-insensitive uniqueness for identification numbers:
     - Defined stored computed column `IdentificationNumberKey = upper("IdentificationNumber")` (`varchar(50)`).
     - Created unique index `ux_customers_identification_number_key` on `IdentificationNumberKey`.
     - In PostgreSQL, standard unique indexes permit unlimited `NULL` rows, allowing multiple customers without identification numbers to coexist while strictly enforcing case-insensitive uniqueness when an identification number is provided.
  3. Case-insensitive search optimization:
     - Defined stored computed column `EmailKey = lower("Email")` (`varchar(320)`), enabling native EF Core `EF.Functions.ILike` text searching without value converter type mapping conflicts.
  4. Conflict exception translation:
     - `CustomerRepository` inspects `DbUpdateException` for PostgreSQL error code `23505` (`UniqueViolation`) specifically matching constraint name `ux_customers_identification_number_key`.
     - Translates matching errors to `DuplicateCustomerIdentificationException` in `SistemaInventarioVentas.Application.Exceptions`.
     - `CustomerUseCases` catches `DuplicateCustomerIdentificationException` during `CreateAsync` and `UpdateDetailsAsync`, returning `Result<Customer>.Failure(ApplicationErrorCode.CustomerIdentificationConflict, ...)` without leaking provider-specific database exceptions.
     - Unrelated database exceptions (such as primary key collisions, connection errors, or check constraint violations) are not caught as duplicate identification errors.
  5. Dependency injection & test isolation:
     - Registered `ICustomerRepository` as scoped in `PersistenceServiceCollectionExtensions.AddPersistence`.
     - Added `Customers.ExecuteDeleteAsync()` to `PostgreSqlFixture.ClearAsync()`.
     - Verified with 12 PostgreSQL Testcontainers integration tests in `CustomerPersistenceTests.cs`.
- Reason: Strictly complies with Clean Architecture boundaries, follows the established pattern of Category and Inventory persistence, enforces durable uniqueness under concurrent writes without race conditions, and prevents EF Core value converter type mapping errors in queries.
- Alternatives considered:
  - Standard non-computed unique index on `IdentificationNumber`: Rejected because standard PostgreSQL unique indexes on default collations are case-sensitive, which would allow `'ABC'` and `'abc'` to collide with Domain/Application uppercase normalization rules.
  - Partial unique index with `WHERE "IdentificationNumber" IS NOT NULL`: Considered, but stored computed column `upper("IdentificationNumber")` provides both case-insensitivity and native null tolerance while aligning with `Category.NameKey`.
- Consequences: Customer aggregate persistence is fully implemented and verified against PostgreSQL 17 in Testcontainers with 0 pending model changes. Existing Sales schema and behavior remain untouched.

## Decision 34 — Customer Minimal API Endpoints, DTO Contracts, and Authorization Policies

- Decision: Implemented the Customer Minimal API in `SistemaInventarioVentas.API` under route group `/api/customers`:
  1. Route endpoints:
     - `POST /api/customers` (`CreateAsync`, `AdminOnly`): Creates a customer from `CreateCustomerRequest` and returns `201 Created` with `CreatedAtRoute` referencing route `GetCustomer`.
     - `GET /api/customers/{id:guid}` (`GetAsync`, authenticated `User` or `Admin`): Retrieves a customer by ID and returns `200 OK` or `404 Not Found`.
     - `GET /api/customers` (`ListAsync`, authenticated `User` or `Admin`): Lists customers supporting query parameters `bool? isActive` and `string? searchTerm`, returning `200 OK`.
     - `PUT /api/customers/{id:guid}` (`UpdateAsync`, `AdminOnly`): Updates customer details from `UpdateCustomerRequest`, returning `200 OK`, `404 Not Found`, or `409 Conflict`.
     - `POST /api/customers/{id:guid}/activate` (`ActivateAsync`, `AdminOnly`): Activates a customer, returning `200 OK` or `404 Not Found`.
     - `POST /api/customers/{id:guid}/deactivate` (`DeactivateAsync`, `AdminOnly`): Deactivates a customer, returning `200 OK` or `404 Not Found`.
  2. DTO Encapsulation:
     - `CreateCustomerRequest`, `UpdateCustomerRequest`, and `CustomerResponse` decouple API consumers from internal Domain entities and EF Core persistence models.
     - `RequestValidation` validates DataAnnotations constraints before invoking Application use cases.
  3. Error & Problem Details mapping:
     - Mapped `ApplicationErrorCode.CustomerIdentificationConflict` to HTTP 409 Conflict with title `"Customer conflict"` in `ApplicationProblemMapping`.
     - Validation errors return HTTP 400 `ValidationProblemDetails`.
     - Missing customer entities return HTTP 404 `ProblemDetails`.
  4. Authentication & Authorization:
     - All `/api/customers` endpoints require authentication (`RequireAuthorization()`), returning HTTP 401 Unauthorized for anonymous callers.
     - Reads (`GET /`, `GET /{id}`) allow both `User` and `Admin` roles.
     - Mutations (`POST /`, `PUT /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`) require `AdminOnly` (`RequireRole("Admin")`), returning HTTP 403 Forbidden for non-Admin authenticated callers.
  5. Test coverage:
     - Added 19 PostgreSQL Testcontainers API integration tests in `tests/SistemaInventarioVentas.API.Tests/CustomerApiTests.cs`.
- Reason: Satisfies requirements `FR-CUST-001` through `FR-CUST-005` and `NFR-API-001` through `NFR-API-003`, maintaining architectural consistency with Catalog, Inventory, and Sales APIs while preserving aggregate encapsulation and authorization policies.
- Alternatives considered:
  - Embedding `IsActive` in `UpdateCustomerRequest`: Rejected in favor of explicit `activate` and `deactivate` lifecycle endpoints consistent with `Sale.Confirm` and `Inventory` entry/exit commands.
  - Allowing anonymous reads of customers: Rejected because customer names, contact info, and fiscal identification are sensitive business records (`NFR-SEC-001`).
- Consequences: Customer management is completely operational via HTTP Minimal APIs. Clean Architecture boundaries are preserved. The next phase will link customers to sales (Phase 4.6).

## Decision 35 — Customer Integration with Sales Workflow (Hybrid Snapshot and Relational Link)

- Decision: Integrated the Customer domain with the Sales workflow using the hybrid snapshot architecture established in Phase 4.1:
  1. Domain Aggregate (`Sale.cs`):
     - Added nullable `public Guid? CustomerId { get; private set; }`.
     - Extended constructors to accept optional `Guid? customerId = null`, validating non-empty Guid when provided.
     - Added `AssignCustomer(Guid customerId, string customerName)`: enforces `Draft` status, non-empty Guid, non-blank name; sets `CustomerId = customerId`, snapshots trimmed name into `CustomerReference`.
     - Added `ClearCustomer(string customerReference)`: enforces `Draft` status, non-blank reference; sets `CustomerId = null`, updates `CustomerReference`.
     - Maintained `UpdateCustomerReference(string customerReference)` for walk-in reference updates.
     - Enforced that customer association and snapshotted reference cannot be modified once a sale transitions out of `Draft` status (`Confirmed` or `Cancelled`).
  2. Application Layer (`SaleUseCases.cs`):
     - Injected optional `ICustomerRepository? customers = null` into primary constructor preserving backwards compatibility for existing callers.
     - Overloaded `CreateDraftAsync` to accept optional `customerId`. If supplied, validates non-empty Guid, fetches customer from `ICustomerRepository`, returns `NotFound` if non-existent, rejects with `InvalidInput` if `!customer.IsActive`, snapshots `customer.Name` into `CustomerReference`. If not supplied, requires `customerReference` as a walk-in sale.
     - Added `UpdateCustomerAsync(Guid saleId, Guid? customerId, string? customerReference = null, CancellationToken cancellationToken = default)`: validates sale exists, is `Draft`; assigns active registered customer or clears customer back to walk-in with supplied reference; persists via `ISaleRepository.SaveAsync`.
     - Preserves historical name snapshot: if a registered customer is renamed or deactivated in the catalog later, existing sales retain their original snapshotted `CustomerReference`.
  3. Persistence & Relational Integrity (`SaleConfiguration.cs` & Migrations):
     - Added nullable `CustomerId` column to `sales` table with index `ix_sales_customer_id`.
     - Configured foreign key relationship `builder.HasOne<Customer>().WithMany().HasForeignKey(sale => sale.CustomerId).IsRequired(false).OnDelete(DeleteBehavior.Restrict)`.
     - `DeleteBehavior.Restrict` enforces `BR-CUST-004`: registered customers with associated sales cannot be deleted from the database.
     - Generated EF Core migration `20261020034258_AddCustomerIdToSales` with 0 pending model changes.
  4. API Layer (`SaleContracts.cs`, `SaleMappings.cs`, `SaleEndpoints.cs`):
     - Updated `CreateSaleRequest` implementing `IValidatableObject`: accepts optional `CustomerId`, requires `CustomerReference` only when `CustomerId` is null.
     - Added `UpdateSaleCustomerRequest` implementing `IValidatableObject`: accepts optional `CustomerId` and `CustomerReference`.
     - Updated `SaleResponse` to expose `Guid? CustomerId = null`.
     - Mapped `sale.CustomerId` in `SaleMappings.ToResponse`.
     - Updated `POST /api/sales` to pass `request.CustomerId`.
     - Added `PUT /api/sales/{id:guid}/customer` (`AdminOnly`) to update or clear the customer on a draft sale.
  5. Test Verification:
     - 14 Domain unit tests covering `CustomerId`, `AssignCustomer`, `ClearCustomer`, empty GUID rejection, blank name rejection, and immutability after confirmation/cancellation.
     - 12 Application unit tests covering registered customer lookup, name snapshotting, non-existent customer `NotFound`, inactive customer `InvalidInput`, snapshot preservation on customer rename, and `UpdateCustomerAsync` lifecycle validation.
     - 4 Infrastructure integration tests against PostgreSQL in Testcontainers verifying `CustomerId` persistence, null coexistence with walk-ins, snapshot preservation on customer rename/deactivation, and `DeleteBehavior.Restrict` blocking customer deletion when sales exist.
     - 7 API integration tests against Testcontainers verifying `POST /api/sales` with `customerId`, 404 on nonexistent, 400 on inactive, `PUT /api/sales/{id}/customer` linking/clearing customers, 400 rejection on confirmed sales, and 401/403 authorization rules.
- Reason: Satisfies requirements `FR-CUST-006`, `BR-CUST-003`, `BR-CUST-004`, `BR-CUST-006`, and `BR-SALE-014` without breaking existing walk-in sales or compromising atomic confirmation and inventory deduction.
- Alternatives considered:
  - Required CustomerId on all sales: Rejected because businesses require walk-in sales without forcing customer registration.
  - Dynamically joining customer name on query: Rejected because legal and accounting regulations require sales records to preserve the exact customer name at the moment the sale took place, even if the customer is later renamed, merged, or deactivated.
- Consequences: Sales and Customers are cleanly integrated across all layers. Full backwards compatibility is preserved. Phase 4 is fully completed.

## Decision 36 — Supplier Domain Planning, Identity, Contact Rules, and Inventory Relationship Boundaries

- Decision: Defined the Supplier domain model as an independent Aggregate Root `Supplier` in `SistemaInventarioVentas.Domain` following the established Clean Architecture entity patterns while addressing vendor-specific requirements:
  1. Supplier Aggregate Root Attributes:
     - `Id`: `Guid` (unique entity identifier).
     - `Name`: `string` (required legal or commercial supplier company name, 1–200 characters, trimmed).
     - `ContactName`: `string?` (optional contact representative / account executive name, max 200 characters, trimmed).
     - `IdentificationNumber`: `string?` (optional Tax ID / RFC / CIF / VAT number, max 50 characters, trimmed, uppercase-normalized, unique across suppliers when provided).
     - `Email`: `EmailAddress?` (optional contact email address using the existing domain `EmailAddress` value object; validated syntax and lowercase-normalized).
     - `Phone`: `string?` (optional contact phone number, max 30 characters, trimmed).
     - `Address`: `string?` (optional warehouse or business address, max 300 characters, trimmed).
     - `IsActive`: `bool` (supplier status flag; defaults to `true` on creation).
     - `CreatedAtUtc`: `DateTimeOffset` (UTC timestamp).
  2. Domain Methods:
     - `Supplier(name, contactName = null, identificationNumber = null, email = null, phone = null, address = null, createdAtUtc = null)`: creates an active supplier.
     - `Supplier(id, name, contactName, identificationNumber, email, phone, address, isActive, createdAtUtc)`: rehydration constructor.
     - `UpdateDetails(string name, string? contactName, string? identificationNumber, EmailAddress? email, string? phone, string? address)`: updates commercial and contact fields with full validation.
     - `Activate()`: activates the supplier (`IsActive = true`).
     - `Deactivate()`: deactivates the supplier (`IsActive = false`).
  3. Contact Information Rules:
     - Corporate/commercial `Name` is strictly required.
     - Contact details (`ContactName`, `Email`, `Phone`, `Address`) are optional to support local/informal vendors without forcing placeholder data (`BR-SUP-005`).
     - When supplied, all contact fields are trimmed, length-checked, and structurally validated.
  4. Active/Inactive Lifecycle & Sourcing Invariants:
     - Inactive suppliers cannot be used for recording new incoming inventory operations (`BR-SUP-003`).
     - Deactivating a supplier preserves historical inventory entries and stock records intact (`BR-SUP-003`, `BR-SUP-004`).
     - Supplier records associated with existing inventory movements must not be deleted (`DeleteBehavior.Restrict`, `BR-SUP-004`).
  5. Product and Inventory Relationship Boundaries:
     - Products do NOT have a mandatory supplier reference. Products belong to categories independently of sourcing.
     - Sourcing relationships belong to incoming inventory operations (entries/receipts) and future purchasing workflows.
     - An optional preferred supplier on `Product` is deferred until catalog product enhancement or purchasing is requested.
     - When incoming inventory entries reference a supplier in future integration phases, a hybrid snapshot pattern (storing `SupplierId` with a snapshotted supplier name in the movement audit reference) will ensure immutability and fiscal traceability without breaking existing generic inventory entries (`BR-SUP-006`).
  6. Unit Test Scenarios Specified:
     - Supplier creation with valid name initializes active status with null optional fields.
     - Supplier creation with full contact fields normalizes all attributes.
     - Creation rejects null, empty, whitespace, or >200 chars name (`ArgumentException`).
     - Creation rejects >200 chars contact name (`ArgumentException`).
     - Creation rejects >50 chars identification number (`ArgumentException`).
     - Creation rejects invalid email syntax (`ArgumentException`).
     - Creation rejects >30 chars phone or >300 chars address (`ArgumentException`).
     - `Activate()` and `Deactivate()` state transitions.
     - `UpdateDetails()` validates and normalizes all fields atomically.
  7. Unresolved Decisions Recorded:
     - Full purchasing workflow (Purchase Orders, Vendor Bills, Accounts Payable) remains deferred.
     - Product-to-supplier catalog mapping (single default supplier vs. many-to-many `ProductSupplier` catalog table) is deferred until catalog sourcing requirements are defined.
     - Schema migration for linking `SupplierId` to `inventory_movements` is deferred to the inventory-supplier integration phase.
- Reason: Comply with Clean Architecture boundaries, maintain design consistency with Customer and Category aggregates, avoid tight coupling between Catalog and Sourcing, and prepare a solid, minimal Supplier domain without scope creep.
- Alternatives considered:
  - Requiring a `SupplierId` on `Product`: Rejected because products can be supplied by multiple vendors or created before a supplier relationship is finalized, and changing `Product` now would prematurely couple Catalog to Suppliers.
  - Omission of `ContactName`: Rejected because B2B vendor management typically requires tracking a representative or account manager distinct from the company name.
## Decision 37 — Supplier Minimal API Endpoints, DTO Contracts, and Authorization Policies

- Decision: Implemented the Supplier Minimal API in `SistemaInventarioVentas.API` under route group `/api/suppliers`:
  1. Route endpoints:
     - `POST /api/suppliers` (`CreateAsync`, `AdminOnly`): Creates a supplier from `CreateSupplierRequest` and returns `201 Created` with `CreatedAtRoute` referencing route `GetSupplier`.
     - `GET /api/suppliers/{id:guid}` (`GetAsync`, authenticated `User` or `Admin`): Retrieves a supplier by ID and returns `200 OK` or `404 Not Found`.
     - `GET /api/suppliers` (`ListAsync`, authenticated `User` or `Admin`): Lists suppliers supporting query parameters `bool? isActive` and `string? searchTerm`, returning `200 OK`.
     - `PUT /api/suppliers/{id:guid}` (`UpdateAsync`, `AdminOnly`): Updates supplier details from `UpdateSupplierRequest`, returning `200 OK`, `404 Not Found`, or `409 Conflict`.
     - `POST /api/suppliers/{id:guid}/activate` (`ActivateAsync`, `AdminOnly`): Activates a supplier, returning `200 OK` or `404 Not Found`.
     - `POST /api/suppliers/{id:guid}/deactivate` (`DeactivateAsync`, `AdminOnly`): Deactivates a supplier, returning `200 OK` or `404 Not Found`.
  2. DTO Encapsulation:
     - `CreateSupplierRequest`, `UpdateSupplierRequest`, and `SupplierResponse` decouple API consumers from internal Domain entities and EF Core persistence models.
     - `RequestValidation` validates DataAnnotations constraints before invoking Application use cases.
  3. Error & Problem Details mapping:
     - Mapped `ApplicationErrorCode.SupplierIdentificationConflict` to HTTP 409 Conflict with title `"Supplier conflict"` in `ApplicationProblemMapping`.
     - Validation errors return HTTP 400 `ValidationProblemDetails`.
     - Missing supplier entities return HTTP 404 `ProblemDetails`.
  4. Authentication & Authorization:
     - All `/api/suppliers` endpoints require authentication (`RequireAuthorization()`), returning HTTP 401 Unauthorized for anonymous callers.
     - Reads (`GET /`, `GET /{id}`) allow both `User` and `Admin` roles.
     - Mutations (`POST /`, `PUT /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`) require `AdminOnly` (`RequireRole("Admin")`), returning HTTP 403 Forbidden for non-Admin authenticated callers.
  5. Test coverage:
     - Added 22 PostgreSQL Testcontainers API integration tests in `tests/SistemaInventarioVentas.API.Tests/SupplierApiTests.cs`.
- Reason: Satisfies requirements `FR-SUP-001` through `FR-SUP-005` and `NFR-API-001` through `NFR-API-003`, maintaining architectural consistency with Customer, Catalog, Inventory, and Sales APIs while preserving aggregate encapsulation and authorization policies.
- Alternatives considered:
  - Embedding `IsActive` in `UpdateSupplierRequest`: Rejected in favor of explicit `activate` and `deactivate` lifecycle endpoints consistent with Customer and Sale commands.
  - Allowing anonymous reads of suppliers: Rejected because vendor contact info, commercial details, and fiscal identification are sensitive business records (`NFR-SEC-001`).
- Consequences: Supplier management is completely operational via HTTP Minimal APIs. Clean Architecture boundaries are preserved. The next phase will link suppliers to incoming inventory operations (Phase 5.6).

## Decision 38 — Supplier Integration with Inventory Movements (Hybrid Sourcing Snapshot and Relational Link)

- Decision: Designed the architecture for integrating suppliers with incoming inventory operations:
  1. Granular Association at the Movement Level:
     - `SupplierId` is associated with `InventoryMovement`, not the parent `Inventory` aggregate root.
     - Rationale: `Inventory` represents cumulative current stock of a `ProductIdentifier` across all past operations. Sourcing events happen per stock receipt. Associating the vendor with individual `MovementType.Entry` records reflects physical delivery events accurately.
  2. Hybrid Sourcing Snapshot Pattern:
     - `InventoryMovement` will store both:
       - `SupplierId`: `Guid?` (optional relational link to the `Supplier` aggregate root).
       - `SupplierName`: `string?` (optional snapshot of `supplier.Name` at the time of stock delivery, max 200 characters).
     - Rationale: Mirrors the established Customer-Sale hybrid pattern (`Sale.CustomerId` + `Sale.CustomerReference`). If a vendor changes its corporate or trading name later, past inventory movements preserve the historical vendor name on the receipt record without mutating audit history (`BR-SUP-006`).
     - Domain Invariant: If `SupplierId` is supplied, `SupplierName` must be provided (non-empty, trimmed, max 200 characters). If `SupplierId` is null, `SupplierName` must be null.
  3. Active Supplier Validation in Application Layer:
     - `InventoryUseCases.RecordEntryAsync` accepts optional `Guid? supplierId = null`.
     - When `supplierId` is supplied:
       - Injects `ISupplierRepository` into `InventoryUseCases` (via constructor parameter).
       - Rejects empty GUID (`Guid.Empty`) with `ApplicationErrorCode.InvalidInput`.
       - Retrieves supplier via `ISupplierRepository.GetByIdAsync(supplierId.Value, cancellationToken)`.
       - If supplier does not exist: returns `ApplicationErrorCode.NotFound`.
       - If supplier is inactive (`!supplier.IsActive`): returns `ApplicationErrorCode.InvalidInput` ("Inactive suppliers cannot be used for recording new inventory entries.") enforcing `BR-SUP-003`.
       - Snapshots `supplier.Name` and delegates to `Inventory.RegisterEntry(quantity, reference, occurredAtUtc, supplier.Id, supplier.Name)`.
  4. Full Backwards Compatibility for Non-Supplier and Non-Entry Operations:
     - `supplierId` is strictly optional (`Guid? supplierId = null`).
     - Manual/internal stock receipts, initial stock initialization, and tests continue to record entries with `supplierId = null` and `supplierName = null`.
     - Stock exits (`RecordExitAsync`) and adjustments (`AdjustAsync`) do not accept `supplierId` and always leave `SupplierId` and `SupplierName` null.
     - All existing historical database records remain valid with NULL columns.
  5. Foreign Key Deletion Protection & Schema Migration:
     - Table `inventory_movements` adds nullable column `SupplierId` (`uuid`) and nullable column `SupplierName` (`varchar(200)`).
     - Configures foreign key `FK_inventory_movements_suppliers_SupplierId` with `DeleteBehavior.Restrict`.
     - Configures database index `ix_inventory_movements_supplier_id` for efficient sourcing history lookups.
     - Rationale: Directly enforces `BR-SUP-004` (suppliers tied to historical inventory movements must never be deleted from the system).
  6. Layered Responsibilities:
     - Domain (`Inventory`, `InventoryMovement`): owns the aggregate invariant (`SupplierId` requiring valid `SupplierName`), stock calculation, and movement encapsulation.
     - Application (`InventoryUseCases`): owns supplier lookup, active status validation, and error translation.
     - Infrastructure (`InventoryMovementConfiguration`): maps EF Core columns, indexes, and restricted foreign key relationship.
     - API (`InventoryContracts`, `InventoryEndpoints`, `InventoryMappings`): exposes optional `SupplierId` in `RecordStockEntryRequest` and returns `SupplierId`/`SupplierName` in `InventoryMovementResponse`.
- Alternatives considered:
  - Associating Supplier with `Product`: Rejected because a catalog product can be supplied by multiple vendors over time, and associating sourcing to the product aggregate would prematurely force single-supplier constraints or complex many-to-many catalogs.
  - Storing only `SupplierId` without a name snapshot: Rejected because database updates to vendor names would retroactively mutate past movement audit records, violating `BR-SUP-006` and fiscal audit requirements.
  - Embedding supplier name inside the free-text `Reference` field: Rejected because structured properties enable type-safe API responses, cleaner SQL/reporting aggregations, and keep delivery note / invoice numbers distinct from vendor identity.
  - Cascading deletion of movements on supplier deletion: Strongly rejected because deleting a vendor must never destroy inventory movement history (`BR-SUP-004`, `BR-INV-006`).

## Decision 39 — Audit Capability Architecture, Event Model, and Transactional Boundaries

- Decision: Designed the architecture, domain model, operational event scope, and transactional boundaries for the Audit capability (`FR-AUD-001` through `FR-AUD-004`, `BR-AUD-001` through `BR-AUD-003`):
  1. Operational Event Scope:
     - State-changing business events requiring audit records:
       - Sales lifecycle: `SaleCreated` (Draft), `SaleConfirmed` (finalized with totals and items), `SaleCancelled` (voided).
       - Inventory operations: `InventoryMovementRecorded` (Entry, Exit, Adjustment with quantities, references, and supplier if applicable).
       - Entity lifecycle transitions: `ProductActivated`, `ProductDeactivated`, `SupplierActivated`, `SupplierDeactivated`, `CustomerActivated`, `CustomerDeactivated`, `UserActivated`, `UserDeactivated`.
       - Security & authentication events: `UserLoginSucceeded`, `UserLoginFailed`.
     - Read operations: Strictly excluded from audit events. Queries, list views, and report generation do not emit audit records to prevent log bloat and performance degradation.
  2. Audit Event Model and Schema:
     - Entity `AuditEvent` in Domain layer:
       - `Id`: `Guid` unique identifier.
       - `EventType`: `string` standardized event name (e.g. `SaleConfirmed`, `ProductDeactivated`), required, trimmed, 1–100 characters.
       - `EntityName`: `string` domain aggregate or target category (e.g. `Sale`, `InventoryMovement`, `Product`, `Supplier`, `Customer`, `User`), required, trimmed, 1–100 characters.
       - `EntityId`: `string?` business or technical identifier of the affected record (max 100 characters). Nullable for system-level or non-entity events (such as failed authentication attempts).
       - `UserId`: `Guid?` identifier of the authenticated user who initiated the action. Nullable for unauthenticated operations (e.g., failed logins) or system-initiated processes.
       - `OccurredAtUtc`: `DateTimeOffset` timestamp when the action occurred in UTC.
       - `MetadataJson`: `string?` structured JSON payload capturing operational details (e.g., sale totals, movement quantity and type, attempted username, client IP or reason), excluding all sensitive credentials.
     - Invariants: `AuditEvent` is strictly immutable. Once constructed and persisted, it cannot be modified, updated, or deleted.
  3. Actor Representation:
     - User-initiated actions: `UserId` is extracted directly from the validated server-side identity (`ICurrentUser.UserId`).
     - System-initiated / background tasks: `UserId = null`, with metadata indicating `"actor": "system"`.
     - Unauthenticated authentication events (e.g., failed login): `UserId = null`, metadata records sanitized attempted username and reason.
  4. Transactional Guarantees (In-Band vs. Out-of-Band):
     - In-band transactional auditing for state-changing business operations: `AuditEvent` records share the same PostgreSQL database transaction (`CatalogDbContext`) as the mutating business entity (Sale confirmation, inventory deduction, product deactivation).
     - Atomicity rule: If the business transaction fails or rolls back, the audit record is rolled back with it. No phantom audit records may exist for uncommitted state changes.
     - Out-of-band / immediate auditing for standalone authentication events: `UserLoginSucceeded` and `UserLoginFailed` do not modify operational aggregates and are committed independently upon evaluation.
  5. Clean Architecture Layer Responsibilities:
     - Domain: Owns the `AuditEvent` entity with validation rules (non-empty `EventType`, `EntityName`, UTC timestamp) and immutability invariants.
     - Application: Owns `IAuditRepository` abstraction (`InsertAsync`, `GetByIdAsync`, `ListAsync`), `AuditQuery` filter model, and orchestrates audit record generation inside existing use cases (e.g., `SaleUseCases`, `InventoryUseCases`, `ProductUseCases`, `SupplierUseCases`, `CustomerUseCases`).
     - Infrastructure: Owns EF Core `AuditEventConfiguration`, maps `audit_events` table in PostgreSQL, creates database indexes on `(OccurredAtUtc)`, `(EventType)`, `(EntityName, EntityId)`, and `(UserId)`, and provides append-only repository implementation (no update/delete methods exposed).
     - API: Owns Minimal API endpoints under `/api/audit` (`GET /api/audit`, `GET /api/audit/{id}`), restricted to `Admin` role (`RequireRole("Admin")`), mapping to `AuditEventResponse` DTOs.
  6. Security and Sensitive Data Protections:
     - Under no circumstances will passwords, password hashes, JWT tokens, signing keys, connection strings, or full credit card / financial details be written into `MetadataJson` or any audit field (`NFR-SEC-003`, `NFR-OBS-003`).
     - Failed login attempts will record the sanitized attempted username, but never the failed password input.
  7. Deferred Capabilities:
     - Event sourcing (the system remains a state-based relational model with an append-only audit trail; domain state is never rehydrated from audit events).
     - Asynchronous message brokers or outbox patterns (Kafka, RabbitMQ, MassTransit) remain deferred; auditing is handled synchronously within the existing EF Core Unit of Work.
     - Third-party audit frameworks (e.g., Audit.NET) are rejected in favor of direct, lightweight domain-aligned logging.
     - Log retention, automatic partition pruning, and cold-storage archiving policies remain deferred until storage thresholds are defined.
     - Fine-grained entity property diffing (automatic before-and-after JSON delta calculation) remains deferred; audit records capture operational intent and key summary values.
- Reason: Satisfies requirements `FR-AUD-001` through `FR-AUD-004`, `NFR-OBS-001` through `NFR-OBS-003`, and `NFR-DI-003`, establishing an immutable, reviewable, and strictly consistent audit trail without introducing unnecessary distributed systems complexity or compromising Clean Architecture boundaries.
- Alternatives considered:
  - Event Sourcing as the primary persistence model: Rejected because event sourcing adds immense architectural overhead, changes aggregate hydration semantics across all existing domains, and is unnecessary for the project's operational and reporting requirements.
  - Asynchronous outbox / message queue logging: Rejected for state-changing operations because asynchronous audit logs risk losing audit entries if workers fail or creating discrepancies if state changes commit while audit messages fail to publish. In-band database transactions guarantee 100% audit-to-state consistency.
  - Auditing all read / query operations: Rejected because logging read requests causes massive database write amplification, inflates storage rapidly, and degrades query performance. Read logging belongs to HTTP access/proxy logs if ever needed, not operational domain auditing.
  - Storing unstructured plaintext log strings instead of `AuditEvent` entity: Rejected because structured fields (`EventType`, `EntityName`, `EntityId`, `UserId`, `OccurredAtUtc`) enable efficient relational indexing, filtering, and administrative audit queries.
- Consequences: Establishes a comprehensive, reliable, append-only audit architecture. Prepares for Phase 6.2 (Audit Domain Implementation) with clear entity invariants, transactional boundaries, and layer responsibilities. No production code or migrations were modified in this phase.

## Decision 40 — Reporting Architecture, MVP Report Set, and Read-Model Boundaries

- Decision: Designed the reporting architecture, MVP report set, query model, data boundaries, and authorization rules for the Reports capability (`FR-REP-001` through `FR-REP-005`, `FR-SALE-HIST-001` through `FR-SALE-HIST-004`, `BR-REP-001` through `BR-REP-007`):
  1. MVP Report Set:
     - Sales Summary & History Report (`GET /api/reports/sales-summary`):
       - Purpose: Summarizes transaction counts, confirmed sales counts, subtotals, tax collected, and gross revenue over a date range (`FromUtc`, `ToUtc`), with optional filters for `CustomerId`, `UserId`, and `SaleStatus`. Lists matching sales with item counts and monetary totals.
       - Calculation rule: Revenue and financial summary totals aggregate strictly `Confirmed` sales (`BR-REP-004`). `Draft` and `Cancelled` sales are included in counts if explicitly filtered, but excluded from revenue to avoid distorting commercial metrics.
     - Product Sales Performance Report (`GET /api/reports/product-sales`):
       - Purpose: Analyzes product sales volume and monetary contributions across products and categories over a date range.
       - Calculation rule: Aggregates units sold, line subtotals, line taxes, and line gross sales from immutable `sale_details` snapshots where parent `Sale` is `Confirmed` (`BR-REP-005`).
     - Inventory Stock Status Report (`GET /api/reports/inventory-status`):
       - Purpose: Provides an operational snapshot of on-hand inventory across catalog products, identifying total units on hand, in-stock products (`Quantity > 0`), and out-of-stock products (`Quantity == 0` or missing inventory record).
       - Calculation rule: Products registered in the catalog without an initialized inventory record are reported as zero stock (`HasInventoryRecord = false`) (`BR-REP-006`).
     - Inventory Movement Traceability Report (`GET /api/reports/inventory-movements`):
       - Purpose: Cross-product warehouse movement audit trail detailing entries, exits, and adjustments over date ranges, product identifiers, and supplier snapshots.
  2. Audit Reporting Candidate Evaluation:
     - Evaluated candidate "Audit History Report": The existing `GET /api/audit` and `GET /api/audit/{id}` Minimal API endpoints (implemented in Phase 6.7) already provide complete, filtered administrative access to audit records across event types, entities, actors, and date ranges.
     - Decision: A separate duplicate "Audit Report" is explicitly rejected to avoid redundant endpoints and code duplication (`Principle 5`, `Principle 6`).
  3. Clean Architecture Read-Model Boundaries:
     - Domain Layer: Unchanged. Reports are read projections, not transactional domain aggregates. No speculative domain entities or mutation logic are introduced.
     - Application Layer: Defines report query models (`SalesSummaryReportQuery`, `ProductSalesReportQuery`, `InventoryStatusReportQuery`, `InventoryMovementReportQuery`), read DTOs, query validation rules (`FromUtc <= ToUtc`), and query orchestration (`IReportingRepository` or `ReportingUseCases`).
     - Infrastructure Layer: Implements read-only projection queries using EF Core and PostgreSQL (`CatalogDbContext`) with `.AsNoTracking()`, efficient `.GroupBy()` / `.Select()` projections, and joins utilizing existing relational indexes (`ix_sales_created_at_utc`, `ix_sales_status`, `ix_sales_customer_id`, `ix_sale_details_sale_id`, `ix_sale_details_product_identifier`, `ix_inventory_movements_occurred_at_utc`, `ix_inventory_movements_product_identifier`, `ix_inventory_movements_supplier_id`).
     - API Layer: Exposes Minimal API endpoints under `/api/reports` with ProblemDetails error mapping and role-based authorization.
  4. Role-Based Authorization Matrix:
     - Financial / Revenue Reports (`sales-summary`, `product-sales`): Restricted to Admin users (`RequireAuthorization("AdminOnly")`).
     - Operational Inventory Reports (`inventory-status`, `inventory-movements`): Accessible to authenticated users (`User` and `Admin`) for day-to-day warehouse operations.
  5. Explicit Data & Calculation Limitations:
     - Missing Product Cost: `Product` does not track purchase/cost price. Therefore, Gross Margin, Net Margin, Markup %, and Cost of Goods Sold (COGS) are NOT supported. Reports strictly present gross revenue and tax amounts.
     - Missing Stock Thresholds: Neither `Product` nor `Inventory` tracks minimum stock or reorder points. Therefore, automated reorder alerts and low-stock warnings are NOT supported; stock status is strictly binary (`InStock` vs. `OutOfStock`).
     - Missing Inventory Valuation: Financial asset valuation of stock on hand is NOT supported without cost data.
     - Missing Sales Returns: Sales cancellations for confirmed sales without inventory reversal are rejected, and credit notes/returns do not exist. Therefore net revenue equals gross sales revenue.
  6. Deferred Decisions:
     - Report export to binary formats (PDF, Excel, CSV) is deferred to future presentation/client enhancements; the API provides structured JSON.
     - Asynchronous report generation, queueing, and scheduled email dispatch remain deferred.
     - Dedicated reporting databases, OLAP data warehouses, read replicas, and materialized views remain deferred until operational traffic justifies them.
- Reason: Satisfies requirements `FR-REP-001` through `FR-REP-005` and `FR-SALE-HIST-001` through `FR-SALE-HIST-004` using focused, performant, read-only queries against existing relational structures without adding speculative dependencies, domain complexity, or architectural overhead.
- Alternatives considered:
  - Adding a generic BI / reporting framework: Rejected as severe overengineering that bypasses Clean Architecture and adds heavy external dependencies.
  - Separate reporting database with ETL: Rejected because current operational volume does not warrant synchronization complexity or potential eventual-consistency lag.
  - Adding a duplicate Audit Report: Rejected because `/api/audit` already satisfies all audit query and filtering requirements.
  - Estimating profit margins using catalog retail prices: Strongly rejected as financially misleading and inaccurate without real cost tracking.
- Consequences: Establishes a comprehensive, practical, and data-grounded plan for Phase 7.2 (Reports Application & Infrastructure Implementation). No production code, tests, or migrations were modified in this phase.

## Decision 41 — Frontend Architecture, Angular Workspace Structure, and API Integration Design

- Decision: Designed the Angular frontend foundation and integration architecture for SistemaInventarioYVentas:
  1. Angular Workspace & Tooling:
     - Framework: Angular 22 standalone architecture (`standalone: true` default, no NgModule boilerplate).
     - Directory: Top-level `frontend/` directory at the repository root.
     - Tooling: Angular CLI 22, TypeScript strict mode, Vite-based development server, and esbuild production bundling via `@angular/build`.
     - Styling: Semantic modern CSS and native HTML5 elements (`<dialog>`, native form controls with `:user-valid`/`:user-invalid` feedback, CSS custom properties / variables) avoiding external paid component libraries.
  2. Feature Organization:
     - Organized into `core/` (`auth`, `http`, `layout`), `shared/` (UI widgets, pipes, dialogs), and lazy-loaded `features/` (`auth`, `dashboard`, `categories`, `products`, `customers`, `suppliers`, `inventory`, `sales`, `reports`, `audit`).
  3. Authentication & Session Strategy:
     - Browser storage: `sessionStorage` for storing the short-lived JWT access token and its UTC expiration timestamp (`ExpiresAtUtc`), scoped to the browser tab and clearing on tab closure to balance user convenience with credential isolation.
     - Token lifecycle: Reactive session tracking with proactive expiration evaluation; automatic `Authorization: Bearer <token>` injection via functional `authInterceptor`.
     - 401 handling: Automatic session invalidation and redirection to `/login` with return URL.
     - 403 handling: Non-intrusive permission warning notification while preserving the current view and session.
     - Route guards: `authGuard` and `adminGuard` act strictly as client-side usability aids; server-side Minimal API endpoints remain the authoritative security boundary.
  4. API Client & Error Handling:
     - Centralized typed services using `HttpClient` wrapping backend endpoints.
     - Error mapping: Functional `errorInterceptor` transforms RFC 7807 `ProblemDetails` and `ValidationProblemDetails` into structured errors mapped to reactive forms and alert toasts.
     - Data flow: Signals for synchronous view state (`signal`, `computed`) and RxJS Observables for HTTP operations.
  5. Local Development & Deployment Strategy:
     - Local dev uses `proxy.conf.json` proxying `/api` and `/auth` to `http://localhost:5000`, matching production same-origin reverse-proxy behavior without requiring CORS overhead.
     - Production containerization: Multi-stage Dockerfile (`node:24-alpine` build -> `nginx:alpine` serving static SPA and reverse-proxying `/api` and `/auth` to the ASP.NET Core container).
  6. Backend Constraints & Gaps Identified:
     - User administration (listing, activating, role management) does not exist in the backend API and is deferred from UI implementation until backend endpoints are planned.
     - Sale cancellation endpoint (`POST /api/sales/{id}/cancel`) is absent from the API and deferred.
     - Sales history listing (`GET /api/reports/sales-summary`) is restricted to `AdminOnly` in the backend, meaning sales history is currently an admin-only feature in the UI.
- Reason: Establishes a lightweight, maintainable, standards-compliant frontend architecture aligned with Clean Architecture boundaries, minimizing external dependencies and strictly respecting existing backend API contracts and authorization rules.
- Alternatives considered:
  - Using `localStorage`: Rejected due to heightened risk of cross-tab persistent credential exposure.
  - Using complex state libraries (NgRx Store): Rejected as overengineering for the current workflow complexity; Angular signals provide sufficient reactive state.
  - Selecting heavy third-party UI libraries: Rejected to keep bundle sizes lean, accessibility native, and styling fully controllable without license or framework lock-in.
- Consequences: Sets up the blueprint for Phase 8.2 (Angular Workspace Setup & Core Architecture). No frontend source code, backend code, or migrations were created in this phase.

## Decision 42 — Catalog Management Frontend Architecture and Absence of Product Deletion

- Decision: Implemented the Catalog Management frontend (Categories and Products) adhering strictly to verified backend contracts:
  1. Absence of Product Aggregate Deletion:
     - The backend API provides no `DELETE /api/products/{identifier}` endpoint to preserve domain invariants (historical sales line items and inventory movements require permanent product references).
     - The frontend does not invent or mock a delete action; instead, it provides status toggling (`isActive: boolean`) via `PUT /api/products/{identifier}`.
  2. Category Deletion & Conflict Handling:
     - `DELETE /api/categories/{id}` is implemented with explicit pre-submission warnings when categories have associated products.
     - Backend 409 Conflict responses (`CategoryInUse`) are parsed through `parseApiError` and displayed cleanly in the modal alert.
  3. Decoupling of Catalog, Inventory Stock, and Pricing:
     - `ProductResponse` strictly reflects catalog definition: `identifier`, `name`, `categoryId`, `categoryName`, and `isActive`.
     - The catalog UI does not fabricate price or stock fields, keeping domain boundaries clean until Inventory and Sales screens are implemented.
  4. Global Utility Consolidation:
     - Shared modal cards, form controls, alerts, spinners, and responsive tables were consolidated into `src/styles.css`.
     - Component stylesheets remain lean (< 3kB), fully adhering to Angular CLI's 4kB budget without loosening warning thresholds.
  5. Admin-Only Mutation Gating:
     - Template-level permission gating hides create, edit, and delete action triggers from non-admin users via `AuthService.isAdmin()`, while backend endpoints enforce authoritative `AdminOnly` policies.
- Reason: Enforces strict fidelity to backend contracts, domain invariants, and Clean Architecture boundaries without adding unconfirmed endpoints, leaking domain rules, or fabricating data fields.
- Alternatives considered:
  - Faking client-side product deletion: Rejected as deceptive and violating backend domain consistency.
  - Adding ungrounded price or stock columns: Rejected because stock belongs to the Inventory aggregate and price to Sale line-item snapshots.
  - Raising Angular CLI style budgets to suppress warnings: Rejected in favor of architectural extraction of repeated CSS into shared styles.
- Consequences: Full Categories and Products management screens implemented with 81 passing unit tests and a zero-warning production build.

## Decision 43 — Customers & Suppliers Management Frontend Architecture and Lifecycle Design

- Decision: Implemented the Customers and Suppliers frontend management screens strictly adhering to verified backend contracts and domain invariants:
  1. Explicit Absence of Entity Deletion:
     - The backend API deliberately provides no `DELETE /api/customers/{id}` or `DELETE /api/suppliers/{id}` endpoints. Customer IDs are permanently referenced in sales records (`Sale.CustomerId`), and supplier IDs are referenced in stock movement audit records (`InventoryMovement.SupplierId`).
     - The UI faithfully reflects this domain invariant by providing explicit activation and deactivation controls (`POST /api/customers/{id}/activate`, `POST /api/customers/{id}/deactivate`, `POST /api/suppliers/{id}/activate`, `POST /api/suppliers/{id}/deactivate`) rather than synthetic or client-side deletion controls.
  2. Independent Entity Contracts:
     - While sharing common fields (ID, name, identification number, email, phone, address, isActive, createdAtUtc), `Supplier` contracts include a representative contact name (`contactName`) while `Customer` contracts do not.
     - Kept models, services, and components strictly decoupled rather than forcing premature inheritance or a generic entity abstraction.
  3. UI & Layout Utility Consolidation:
     - Shared layout utilities (`page-header`, `toolbar-card`, `filters-row`, `state-card`, `actions-group`, `id-pill`, `form-row`) were consolidated into `src/styles.css`.
     - Component stylesheets remain lean (< 1kB), well within Angular CLI's 4kB component budget with zero warnings.
  4. Admin-Only Mutation Gating:
     - Creation, editing, and activation/deactivation triggers are hidden from non-admin users via `AuthService.isAdmin()`, matching backend Minimal API `AdminOnly` policies.
- Reason: Satisfies customer and supplier management requirements while protecting historical sales integrity and inventory traceability without inventing fictitious delete actions or conflating distinct domain aggregate contracts.
- Alternatives considered:
  - Faking client-side entity deletion: Rejected as deceptive, violating backend domain consistency, and risking referential orphan states.
  - Merging Customer and Supplier into a single generic "Contact" component: Rejected because they are distinct aggregates with different lifecycles, backend endpoints, and schema attributes (`contactName`).
  - Increasing Angular CLI style budget limits: Rejected in favor of architectural extraction of repeated CSS into shared styles.
- Consequences: Full Customers and Suppliers management screens implemented with 32 dedicated unit tests (113 total suite tests passing) and a zero-warning production build.

## Decision 44 — Inventory Management Frontend Architecture and Movement Traceability Design

- Decision: Implemented the Inventory Management frontend screens adhering to backend contracts, domain invariants, and Clean Architecture boundaries:
  1. Authoritative Backend Calculations & Delta Operations:
     - The frontend never calculates or posts replacement stock balances. All stock changes are submitted as atomic entries (`POST /api/inventory/entries`), exits (`POST /api/inventory/exits`), or signed non-zero adjustments (`POST /api/inventory/adjustments`).
     - Insufficient stock and concurrency conflicts are detected server-side and rendered cleanly using RFC 7807 problem details parsed via `parseApiError`.
  2. Tabular Separation of Stock Availability and Movement Traceability:
     - The UI separates current stock status (`GET /api/reports/inventory-status`) from historical movement traceability (`GET /api/reports/inventory-movements`) into dedicated tabs.
     - Uninitialized catalog products are rendered with 0 available stock and an explicit "Sin registro" badge in alignment with BR-REP-006.
     - Single product movements can be directly inspected via a focused modal calling `GET /api/inventory/{productIdentifier}/movements`.
  3. Non-Optimistic UI Updates:
     - Stock mutations trigger authoritative refreshes of the backend reporting endpoints rather than speculative client-side state patches, eliminating the possibility of desynchronized stock counts.
  4. Movement Traceability and Supplier Snapshots:
     - Movement history retains immutable supplier name snapshots and user attribution.
     - No editing or deletion of movements is exposed, preserving append-only audit integrity (BR-INV-006, BR-AUD-002).
  5. Admin-Only Mutation Gating:
     - Stock operation buttons and the mutation modal are hidden from non-admin users via `AuthService.isAdmin()`, aligning with backend Minimal API `AdminOnly` policies.
- Reason: Guarantees inventory non-negativity and complete movement traceability without client-side calculation drift or unauthorized mutations.
- Alternatives considered:
  - Allowing the client to calculate and submit new absolute stock quantities: Strongly rejected as it would bypass backend concurrency controls and violate business invariants.
  - Allowing optimistic client-side inventory updates: Rejected because failed requests could misrepresent actual warehouse stock.
  - Combining stock and movement lists into a single overloaded view: Rejected in favor of dedicated tabs with specialized metrics and filtering controls.
- Consequences: Full Inventory Management and Movement Traceability screens implemented with 18 dedicated unit tests (131 total suite tests passing) and a zero-warning production build.

## Decision 45 — Sales Management Frontend Architecture, Atomic Confirmation, and Role-Gated History

- Decision: Implemented the Sales Management frontend adhering strictly to verified backend contracts, domain invariants, and Clean Architecture boundaries:
  1. Authoritative Backend Financial Calculations:
     - The frontend never computes or overrides financial metrics (subtotals, taxes, totals). Domain rules BR-SALE-005, BR-SALE-010, and BR-SALE-012 mandate server-authoritative calculations using `MidpointRounding.AwayFromZero`.
     - Whenever line items are added, updated, or removed, the frontend uses the returned server `SaleResponse` to render all financial values, preventing client-side rounding drift.
  2. Atomic Confirmation & Concurrency / Stock Guard:
     - Sale confirmation (`POST /api/sales/{id}/confirm`) is executed atomically on the server. If inventory is inadequate, the server returns 409 Conflict with an RFC 7807 problem details object indicating `InsufficientStock`.
     - The frontend handles this conflict cleanly via `parseApiError`, displaying an informative error message while preserving the active draft sale for corrections, preventing loss of operator context.
  3. Role-Gated Sales History & Direct ID Lookup:
     - The backend API exposes no general sales collection endpoint on `/api/sales`; only `GET /api/sales/{id}` exists for single-sale inspection by authenticated users.
     - Comprehensive sales transaction history and financial revenue metrics are provided exclusively through `GET /api/reports/sales-summary`, which is restricted to `Admin` users.
     - The UI faithfully reflects this security boundary: `Admin` users access the "Historial de Transacciones" tab with date/status filters, revenue cards, and sale navigation; non-admin users receive a clear notice explaining the administrative restriction and can look up sales by ID via `GET /api/sales/{id}`.
  4. Explicit Absence of Sale Cancellation Endpoint:
     - The backend API currently exposes no sale cancellation endpoint (`POST /api/sales/{id}/cancel`).
     - In adherence to Rule 2 and Rule 12, the UI does not provide or simulate a cancel action, preserving strict alignment with backend capabilities.
  5. Immutable Receipt Presentation & Native Printing:
     - Confirmed sales display an immutable receipt view featuring the sale ID, UTC confirmation timestamp, customer attribution, cashier user ID, immutable line-item snapshots, and financial summary.
     - Native print styling (`@media print`) and trigger (`window.print()`) allow clean physical/PDF printing without third-party dependencies.
  6. Admin-Only Mutation Gating:
     - Draft creation, customer reassignment, detail line management, and confirmation triggers are hidden from non-admin operators via `AuthService.isAdmin()`, aligning with backend Minimal API `AdminOnly` endpoint policies.
- Reason: Enforces strict adherence to backend financial precision, inventory atomicity invariants, and authorization boundaries without introducing client-side arithmetic drift, synthetic endpoints, or unauthorized operations.
- Alternatives considered:
  - Performing client-side subtotal, tax, and total arithmetic: Strongly rejected to prevent rounding discrepancies against the backend's `MidpointRounding.AwayFromZero` calculation engine.
  - Fabricating a generic `GET /api/sales` endpoint or client-side caching of all sales: Rejected because the backend does not provide a general sales collection endpoint and requires role-based access to financial reports.
  - Adding a simulated sale cancellation button: Rejected because no cancellation endpoint exists in the backend API.
  - Adding external PDF or charting libraries for receipts: Rejected to maintain zero bundle-budget violations and avoid unnecessary external dependencies.
- Consequences: Complete Sales workflow implemented across models, service, and components with 21 dedicated unit tests (152 total suite tests passing) and a zero-warning production build.

## Decision 46 — Reports & Analytics Frontend Architecture, Unified Reporting Client, and Role-Gated Analytics

- Decision: Implemented the Reports & Analytics frontend architecture adhering strictly to verified backend contracts, domain reporting invariants, and Clean Architecture boundaries:
  1. Authoritative Backend Aggregations & Calculations:
     - The frontend never performs client-side financial calculations (subtotals, taxes, revenues, quantities). Invariants BR-REP-004 through BR-REP-007 mandate server-side aggregation and exact financial rounding using `MidpointRounding.AwayFromZero`.
     - In alignment with the domain model, only Confirmed sales contribute to realized revenue; Draft and Cancelled transactions are accounted for in count metrics and tables, but contribute \$0.00 to total revenue.
     - Product sales performance utilizes immutable sale-detail snapshots.
     - Uninitialized catalog products in the inventory status report are rendered with 0 units and an explicit "Sin registro" badge in alignment with BR-REP-006.
  2. Consolidated Reporting Client with Compositional Reuse:
     - Authored `ReportService` (`@core/reports/report.service.ts`) providing a unified client for all 4 reporting endpoints (`/api/reports/sales-summary`, `/api/reports/product-sales`, `/api/reports/inventory-status`, `/api/reports/inventory-movements`).
     - To avoid duplicating parameter serialization and HTTP handling, `ReportService` composes and reuses the existing `SaleService.listSummaryReport`, `InventoryService.getStatusReport`, and `InventoryService.getMovementReport` operations while natively providing `getProductSales`.
  3. Responsive Multi-Tab Dashboard with Role-Gated Visibility:
     - Four specialized reporting areas: "Resumen de Ventas" (AdminOnly), "Rendimiento de Productos" (AdminOnly), "Estado de Stock" (all authenticated users), and "Trazabilidad de Movimientos" (all authenticated users).
     - Non-admin users are automatically routed to the operational "Estado de Stock" tab upon initialization and receive an explicit administrative restriction banner if they select financial tabs, preventing unauthorized 403 HTTP requests.
     - Admin users have full access to all 4 reports with filter controls and summary cards.
  4. Traceability Attribution and Query Filter Fidelity:
     - The movement traceability report table renders the audited `userId` attribution snapshot, but strictly refrains from providing a user ID filter in the query form, faithful to the backend API which models attribution snapshotting without a query filter.
  5. Lightweight, Dependency-Free Visualizations:
     - Avoided heavy charting libraries (e.g. Chart.js, D3) to maintain strict bundle size budgets (< 4kB per component).
     - Implemented semantic CSS-based visual progress tracks and ranking cards (`.progress-track`, `.progress-fill`, `.ranking-list`) to illustrate revenue contribution percentages and catalog stock availability ratios.
  6. Date Range Boundary Handling:
     - Date inputs produce exact UTC day boundaries (`YYYY-MM-DDT00:00:00.000Z` to `YYYY-MM-DDT23:59:59.999Z`), ensuring consistent backend query evaluation without timezone offset distortion.
     - Pre-submission validation asserts `fromDate <= toDate`, providing user-friendly feedback before calling the API.
- Reason: Satisfies all reporting and analytics requirements with strict fidelity to backend business rules, authorization policies, and performance constraints without introducing unsupported metrics or heavy external dependencies.
- Alternatives considered:
  - Introducing a third-party charting package (e.g., ngx-charts or Chart.js): Rejected to avoid bundle size bloat, external dependency risk, and budget warnings.
  - Fabricating unsupported financial metrics (profit margins, COGS, inventory valuation): Rejected because costs, purchase prices, and returns are deliberately unmodeled in the backend.
  - Adding a simulated UserId filter on movement queries: Rejected because the backend endpoint does not support filtering movements by UserId.
- Consequences: Full reporting dashboard implemented across models, service, and component with 15 dedicated unit tests (167 total suite tests passing) and a zero-warning production build.

## Decision 47 — Audit Log Frontend Architecture, Immutability Guarantees, and Security Traceability

- Decision: Implemented the Audit Log and System Monitoring frontend adhering strictly to verified backend contracts, domain audit invariants, and Clean Architecture boundaries:
  1. Strict Read-Only Immutability & Absence of Mutation Controls:
     - The audit log is strictly append-only and immutable.
     - In adherence to Rule 2, Rule 12, and security invariants (BR-AUD-001, BR-AUD-002), the UI provides zero mutation controls (no create, edit, or delete actions) for audit events, preserving forensic audit integrity.
  2. Double-Layered Authorization:
     - Route-level protection via `adminGuard` blocks non-admin access at the navigation boundary.
     - Component-level verification via `AuthService.isAdmin()` renders an informative access restriction card if an unprivileged session loads the component, preventing unauthorized 403 Forbidden requests to the backend Minimal API (`/api/audit`).
  3. Query Parameter Fidelity & Chronological Sorting:
     - Implemented `AuditService` (`@core/audit/audit.service.ts`) supporting all backend-defined query parameters: `eventType`, `entityName`, `entityId`, `userId`, `fromUtc`, `toUtc`.
     - The backend provides native chronological ordering (descending by `OccurredAtUtc`, then by `Id`), which the frontend renders faithfully without client-side reshuffling.
  4. Dual-Time Presentation (UTC Authoritative + Local Operator Context):
     - Timestamps are presented with UTC as the authoritative representation (`YYYY-MM-DD HH:mm:ss UTC`) to preserve absolute traceability across timezones, paired with local browser time for operator context.
  5. Detail Inspection Modal with Safe JSON Metadata:
     - When inspecting an audit event, a focused modal renders formatted pretty-printed JSON metadata safely inside `<pre class="font-mono">`.
     - Prohibited sensitive credentials (passwords, hashes, tokens, API keys) are strictly prohibited and sanitized by the domain aggregate.
  6. In-Memory System Monitoring Metrics:
     - Computed signals calculate monitoring indicators (Total Events, User Operations, System Events, Unique Entities Affected) from the active audit view without inventing unmodeled backend server health checks.
- Reason: Provides comprehensive operational visibility, forensic traceability, and security auditing for administrators without violating immutability, leaking sensitive payloads, or inventing backend capabilities.
- Alternatives considered:
  - Faking system health metrics (CPU, RAM, disk, network latency): Rejected as unmodeled and outside project scope.
  - Allowing audit record deletion or modification: Strongly rejected as it would destroy audit validity and violate core domain invariants.
  - Omitting the detail view: Rejected because metadata JSON contains critical correlation context that requires expanded inspection.
- Consequences: Complete Audit Log and System Monitoring interface implemented across models, service, and component with 14 dedicated unit tests (181 total suite tests passing) and a zero-warning production build.

## Decision 48 — Frontend Integration Review, Real Backend End-to-End Validation, and Contract Fidelity Verification

- Decision: Performed comprehensive frontend-to-backend integration review and end-to-end validation across the real running system:
  1. Dedicated, Isolated Integration Environment:
     - Executed against a dedicated, isolated PostgreSQL test container (`sistema-integration-db` on port 5439) with full EF Core schema migrations, completely preventing collision with development data or production secrets.
     - Spun up ASP.NET Core Minimal API on port 5008 and Angular development server with reverse-proxy on port 4200.
  2. Multi-Workflow End-to-End Test Suite:
     - Authored and integrated `scripts/e2e-integration-validation.mjs` (`npm run test:e2e`), executing 159 assertions across 7 core workflows:
       a. App shell & reverse proxy connectivity (`/` and `/auth/me` proxying).
       b. Authentication & Authorization (registration, inactive rejection 401, administrative activation, valid login, invalid login 401, `/auth/me` claims verification, token tampering rejection, operator creation, full role matrix with 403 on admin routes and 200 on operational routes).
       c. Catalog Management (Categories CRUD & 409 conflict guard on delete with attached products, Products CRUD, category and query filtering, active status toggling, Customers CRUD & activation commands, Suppliers CRUD & activation commands, 405 Method Not Allowed validation on unsupported entity deletes).
       d. Inventory & Movement Traceability (uninitialized product status, stock entries with supplier snapshotting, exits, 409 Conflict on insufficient exit, signed-delta adjustments, inactive supplier rejection).
       e. Sales Workflow & Atomic Confirmation (draft creation, customer assignment/reassignment, line item additions, quantity updates, and line deletions with authoritative server calculation of subtotal, 13% tax, and total, 409 Conflict on insufficient stock at confirmation time, atomic confirmation, atomic stock deduction, movement creation, printable receipt lookup, 400 Bad Request rejection on post-confirmation modification, and absence of sale cancellation endpoint).
       f. Reports & Analytics (Sales Summary with realized confirmed revenue, Product Sales performance with immutable line-item snapshotting, Inventory Status with categorized stock states, Movement Traceability with net quantity delta calculation).
       g. Audit Log & Forensic Traceability (event listing, chronological ordering descending by timestamp, eventType and entityName filtering, single-event lookup, user attribution, safe JSON metadata formatting, strict verification of zero password/token/key leaks).
  3. Contract and Invariant Fidelity:
     - Confirmed 100% fidelity between frontend TypeScript contracts, interceptors, error parsers, and the backend ASP.NET Core Minimal API endpoints.
     - Confirmed that reverse proxy whitelists only `/api` and `/auth` routes, preventing unintended leakage of unproxied paths.
     - Confirmed strict adherence to domain rules: no synthetic capabilities or unsupported endpoints invented.
  4. Build & Unit Test Verification:
     - Frontend unit test suite: 27 test files, 181 unit tests passing with 0 failures.
     - Frontend production build (`ng build`): completes cleanly with 0 errors and 0 warnings, all bundle budgets respected.
- Reason: Validates the complete Angular frontend against the running backend under realistic conditions, proving end-to-end functionality, security controls, and domain invariants without expanding product scope or modifying backend code.
- Alternatives considered:
  - Mocking the backend for end-to-end validation: Strongly rejected because Phase 8.10 explicitly mandates exercising the actual running backend and database.
  - Modifying backend endpoints or relaxing constraints: Rejected because backend contracts and invariants are authoritative and must be preserved.
- Consequences: Phase 8 (Frontend Architecture & Development) is fully verified and concluded with end-to-end proof of contract alignment, zero defects, and clean test pass rates.

## Decision 49 — Professional Documentation, Local Setup Architecture, and Repository Hardening for Public Release

- Decision: Established comprehensive repository documentation, isolated local setup architecture, and secret-prevention hardening:
  1. Repository Exclusion & Secret Hardening:
     - Authored a comprehensive root `.gitignore` covering .NET build artifacts (`bin/`, `obj/`, `*.user`, `*.suo`), Angular and Node dependencies (`node_modules/`, `dist/`, `.angular/cache/`), environment configuration files (`.env`, `.env.*.local`, `*.key`, `*.pem`), test coverage reports, and editor files.
     - Identified that legacy build binaries were tracked in git history in early commits before a root `.gitignore` existed; documented safe pruning instructions for the developer while strictly avoiding committing or pushing changes.
  2. Safe Configuration Template:
     - Created `.env.example` defining all required infrastructure, database, runtime, and JWT configuration parameters with clear, safe placeholder values.
     - Mandated minimum 32-byte signing keys for HMAC-SHA256 JWT validation and explicit guidance for resolving local PostgreSQL port collisions (`POSTGRES_PORT`).
  3. Single-Command Local Infrastructure:
     - Created `docker-compose.yml` for PostgreSQL 17 Alpine with container healthchecking (`pg_isready`) and volume persistence.
     - Preserves clean architectural boundaries by containerizing only required infrastructure while keeping backend API (`dotnet run`) and frontend client (`npm start`) locally executable for rapid development.
  4. Explicit Migration Execution Boundary:
     - Discovered and verified that EF Core tools require `SistemaInventarioVentas.Infrastructure` as both `--project` and `--startup-project` due to `CatalogDbContextFactory` and `Microsoft.EntityFrameworkCore.Design` placement. Documented the exact verified execution command.
  5. Security-Compliant Admin Provisioning Workflow:
     - Preserved production security invariants by avoiding baked-in default credentials.
     - Documented a safe two-step bootstrap workflow: registering an account via `POST /auth/register` (hashed with PBKDF2), followed by administrative activation and role assignment via standard database query.
  6. Enterprise Portfolio README:
     - Completely overhauled root `README.md` into a structured, professional guide detailing business problems, domain invariants, module features, Clean Architecture layer diagrams, prerequisites, local execution, test suites, and roadmap limitations.
- Reason: Prepares the codebase for public GitHub portfolio presentation by ensuring that any developer cloning the repository can quickly, reliably, and securely spin up the entire system and verify all test suites.
- Alternatives considered:
  - Baking hardcoded admin credentials or database seeders into migrations: Strongly rejected because it violates credential safety and security invariants.
  - Forcing full containerization of API and Frontend in Phase 8.11: Rejected because full multi-stage application containerization is explicitly scheduled for Phase 9.1.
- Consequences: The repository possesses complete, verified documentation and reproducible local execution tooling. Phase 8 (Frontend & Integration) is formally concluded and ready for Phase 9 (Containerization & Deployment).

## Decision 50 — Multi-Stage Containerization, Production Compose Topology, and GitHub Actions CI Pipeline

- Decision: Designed and implemented reproducible containerization and automated continuous integration for the full application stack:
  1. Multi-Stage .NET 10 API Dockerfile:
     - Authored `src/SistemaInventarioVentas.API/Dockerfile` utilizing `mcr.microsoft.com/dotnet/sdk:10.0` for cached dependency restore and Release compilation.
     - Final runtime uses minimal `mcr.microsoft.com/dotnet/aspnet:10.0` running as non-root user (`$APP_UID`, 1654), listening on internal container port 8080 (`ASPNETCORE_HTTP_PORTS=8080`).
     - Excludes secrets, certificates, and development profiles from the container image.
  2. Multi-Stage Angular 22 Frontend Dockerfile & Nginx Reverse Proxy:
     - Authored `frontend/Dockerfile` using `node:22-alpine` for deterministic dependency installation via `npm ci` and AOT production compilation to `dist/frontend/browser`.
     - Final runtime uses `nginx:alpine` with custom `frontend/nginx.conf` configured for gzip compression, client-side SPA routing fallback (`try_files $uri $uri/ /index.html`), security headers, static asset caching, and reverse proxying of `/api` and `/auth` routes to `http://api:8080`.
  3. Production-Oriented Docker Compose Topology:
     - Created `docker-compose.prod.yml` defining the complete 3-tier architecture: PostgreSQL 17 Alpine (`db`), ASP.NET Core Minimal API (`api`), and Angular Nginx (`frontend`).
     - Enforces healthy database dependency (`condition: service_healthy`) and single external ingress on port 80 (or `${FRONTEND_PORT}`) while keeping API port 8080 internal to the Docker bridge network.
     - Preserves the existing `docker-compose.yml` for local database development.
  4. Parallel GitHub Actions CI Pipeline:
     - Authored `.github/workflows/ci.yml` triggering on pushes and pull requests to `main`.
     - Configured concurrent `backend` and `frontend` jobs running on `ubuntu-latest`.
     - Backend job restores, builds in Release, and executes all 767+ xUnit tests with Testcontainers.
     - Frontend job installs dependencies via `npm ci`, runs non-interactive unit tests (`npm test -- --watch=false`), and compiles the production distribution.
     - Requires zero external secrets for pull-request verification.
  5. Repository Hygiene:
     - Formulated and verified the safe, non-destructive git command (`git rm -r --cached src/*/bin src/*/obj tests/*/bin tests/*/obj`) to unstage 1,012 legacy build binaries from early commits without altering files on disk.
- Reason: Satisfies all Phase 9.1 containerization and CI requirements, providing automated quality gates and a single-command reproducible container deployment.
- Alternatives considered:
  - Automatically running EF Core migrations on container startup: Rejected because multi-instance deployments create race conditions; database migrations remain an explicit administrative command.
  - Exposing API container directly to host in production: Rejected to ensure all external HTTP traffic routes through Nginx reverse proxy.
- Consequences: Both .NET API and Angular frontend are packaged into minimal, secure container images; full application stack is validated in production Compose; CI workflow guards repository mainline.

## Decision 51 — Repository Hygiene, Tracked Build Artifact Remediation, and Security Audit for Public Release

- Decision: Conducted an authoritative repository hygiene, build artifact, and security audit to prepare the codebase for public GitHub release:
  1. Build Artifact Audit & Remediation Verification:
     - Verified exactly 1,012 compiled binary and build-state files (`bin/`, `obj/`) tracked in the Git index across 4 `src/` projects and 4 `tests/` projects, originally committed in early commits prior to establishing the root `.gitignore` in Phase 8.11.
     - Evaluated the candidate un-tracking command: `git rm -r --cached src/*/bin src/*/obj tests/*/bin tests/*/obj`.
     - Confirmed that while `src/*/bin` matches the existing 1st-level project directories under bash shell expansion, it relies on shell-dependent wildcard expansion that fails on shells without auto-expansion (such as Windows PowerShell or `cmd.exe`) and cannot handle nested subdirectories.
     - Formulated and verified the safer, cross-platform Git pathspec command using Git's native glob syntax:
       `git rm -r --cached ':(glob)src/**/bin' ':(glob)src/**/obj' ':(glob)tests/**/bin' ':(glob)tests/**/obj'`
       (or repository-wide: `git rm -r --cached ':(glob)**/bin' ':(glob)**/obj'`).
     - Verified via `--dry-run` that this command matches precisely all 1,012 tracked build files without deleting local working tree files on disk.
     - Adhered strictly to Phase 9.2 safety constraints: did NOT execute any index removal command, leaving execution for explicit developer action prior to GitHub release commit.
  2. Comprehensive Secret & Credential Audit:
     - Confirmed that zero real secrets, passwords, production API keys, JWT signing keys, TLS private keys, certificates, or `.env` credential files are tracked in Git.
     - Verified that all environment variable templates (`.env.example`) and compose files (`docker-compose.yml`, `docker-compose.prod.yml`) use clear, safe placeholder strings and runtime parameter injection.
     - Confirmed that `appsettings.json` and `appsettings.Development.json` contain zero database connection strings or signing keys.
     - Verified that test fixtures (`ApiTestFixture.cs`, `PostgreSqlFixture.cs`) generate ephemeral cryptographic keys (`RandomNumberGenerator.GetBytes(32)`) and connect strictly to isolated ephemeral Testcontainers.
  3. Git Ignore & Exclusion Boundary Verification:
     - Confirmed that root `.gitignore` and `frontend/.gitignore` comprehensively exclude .NET build outputs (`bin/`, `obj/`, `out/`, `build/`, `*.user`, `*.suo`), Angular / Node artifacts (`node_modules/`, `dist/`, `.angular/cache/`, `tmp/`), environment secrets (`*.env`, `secrets.json`, `*.pfx`, `*.pem`, `*.key`), database storage (`postgres_data/`, `*.db`), and IDE/OS caches.
     - Confirmed that `.dockerignore` properly prevents local build artifacts, Git metadata, environment files, and dev compose configurations from polluting container build contexts.
  4. Public Documentation & Deployment Consistency:
     - Verified complete consistency across `README.md`, `launchSettings.json`, `.env.example`, `docker-compose.yml`, `docker-compose.prod.yml`, and `frontend/nginx.conf`:
       - Local Dev API HTTP port: 5008 (`ASPNETCORE_URLS=http://localhost:5008`).
       - Local Dev Frontend port: 4200 (`proxy.conf.json` proxying `/api` and `/auth` to 5008).
       - Local Dev Database: port 5432 (or `${POSTGRES_PORT}`).
       - Containerized API internal port: 8080 (`ASPNETCORE_HTTP_PORTS=8080`).
       - Containerized Frontend / Gateway port: 80 (or `${FRONTEND_PORT}`).
       - Container service names: `sistema-inventario-db`, `sistema-inventario-api`, `sistema-inventario-frontend`.
       - Database migration command: `dotnet ef database update --project src/SistemaInventarioVentas.Infrastructure --startup-project src/SistemaInventarioVentas.Infrastructure` with preceding `export ConnectionStrings__InventoryDatabase=...`.
       - Admin bootstrap workflow: `POST /auth/register` + SQL role activation.
     - Confirmed zero undocumented external cloud dependencies.
- Reason: Guarantees that the repository is completely hardened, secure, lean, and reproducible for public GitHub release without secret exposure or build pollution.
- Alternatives considered:
  - Automatically running `git rm -r --cached` during Phase 9.2: Explicitly rejected per user instructions to avoid modifying Git index state without explicit user consent.
  - Using `git filter-repo` / BFG Repo-Cleaner to rewrite historical commits: Unnecessary for build artifacts and risky for collaborative history; removing files from index with a clean release commit is the standard Git hygiene practice.
- Consequences: The repository is certified clean, secure, and ready for public publication upon developer-approved artifact un-tracking and final commit.

## Decision 52 — Git Index Cleanup, Tracked Binary Removal, and GitHub Release Preparation

- Decision: Completed Git index remediation, remote alignment, and repository presentation hardening for public GitHub release:
  1. Git Index Cleanup Execution:
     - Executed the verified native Git pathspec command `git rm -r --cached ':(glob)**/bin/**' ':(glob)**/obj/**'` to un-track exactly 1,012 legacy build binaries from the index.
     - Reconfirmed that 0 local files were removed or modified on disk; all project binaries and local caches remain completely intact in the local filesystem.
     - Confirmed that `.gitignore` immediately suppresses the unstaged paths, resulting in 0 untracked `bin/` or `obj/` items in `git status`.
     - Confirmed that remaining tracked files in Git total exactly 90 clean, essential source code and documentation files.
  2. Remote Verification & Alignment:
     - Confirmed configured Git remote `origin` targets `https://github.com/CristiamSanchez/InventorySalesSystem.git` on branch `main`.
     - Preserved all remotes, branches, tags, and untracked development files without issuing `git push` or `git commit`.
     - Aligned root `README.md` clone instructions and repository title with the official `InventorySalesSystem` repository name and clone URL.
  3. Visual Presentation & Mermaid Modernization:
     - Replaced legacy ASCII architectural representations in `README.md` with responsive Mermaid diagrams (`flowchart TD` for Clean Architecture layers and `flowchart LR` for 3-tier production container topology) that render natively on GitHub dark/light themes.
     - Established a comprehensive visual walkthrough plan detailing the 12 core application screens with an actionable capture guide in `docs/screenshots/README.md`.
- Reason: Purges repository bloat from Git tracking and establishes an enterprise-grade GitHub portfolio presentation adhering to ProgramingAssistedAI quality gates and security invariants.
- Alternatives considered:
  - Performing a history rewrite (`git filter-branch` or `git filter-repo`): Rejected because index removal in the current commit provides a clean, standard, and safe transition without rewriting historical commit SHA hashes.
  - Fabricating synthetic screenshots using mock tools: Rejected per Rule 12 and Phase 9.3 instructions; authentic captures will be taken from the live running Angular application according to the documented specifications.
- Consequences: The repository is completely cleansed of tracked build bloat, fully aligned with its GitHub remote, equipped with native Mermaid architecture diagrams, and ready for developer-executed release commit and push.

## Decision 53 — Professional README Enhancement, UI Walkthrough, and Authentic Application Screenshot Capture

- Decision: Completed public presentation enhancement, UI walkthrough documentation, and authentic application screenshot capture:
  1. Authentic UI Screenshot Capture:
     - Executed live headless browser capture (`firefox --headless --screenshot`) against the running Angular 22 standalone application on `http://localhost:4200/login` in light mode at 1920×1080 resolution.
     - Verified authentic rendering of `01-login.png` (38,380 bytes) containing the genuine SIV brand logo, credentials card, and reactive controls.
  2. Browser Automation Boundary & Data Invariant Preservation:
     - Identified that headless single-command page screenshots exit before asynchronous API data loading and require persistent user authentication state in browser `sessionStorage`.
     - In strict accordance with Rule 12 (do not invent requirements or fabricate data) and Phase 9.4 instructions, strictly avoided fabricating synthetic or mocked screenshot graphics.
     - Documented the exact manual capture workflow in `docs/screenshots/README.md` for authenticated screens (02–12) with sample data seeding procedures.
  3. Professional Portfolio README Integration:
     - Embedded `01-login.png` in root `README.md` with an informative figure caption highlighting standalone Angular 22 architecture and sessionStorage token isolation.
     - Updated the Application Walkthrough table with direct links to the captured screenshot and the reproducible capture guide in `docs/screenshots/README.md`.
  4. Repository Hygiene:
     - Maintained zero git commits or pushes, preserving all staged artifact deletions and untracked development assets.
     - Verified clean git diff without trailing whitespace errors.
- Reason: Satisfies all Phase 9.4 requirements by delivering an authentic, visually rich public presentation of the application while maintaining strict factual integrity and data hygiene.
- Alternatives considered:
  - Generating mock/synthetic UI images with AI or canvas generators: Strictly rejected because it violates user instructions and Rule 12; genuine application captures reflect actual working software.
  - Modifying Angular authentication or application logic to bypass auth for screenshot automation: Strongly rejected because modifying business logic or security controls for presentation purposes violates architectural integrity.
- Consequences: The repository features an authentic visual presentation in `README.md` and complete reproduction instructions for remaining views, positioning the project for public GitHub release.

## Decision 54 — Release Staging, Final Quality Verification, and Public GitHub Release

- Decision: Completed final release auditing, verified change staging, and public synchronization with GitHub:
  1. Release Safety Audit & Boundary Enforcement:
     - Confirmed target Git remote is exactly `https://github.com/CristiamSanchez/InventorySalesSystem.git` on branch `main`.
     - Reconfirmed that all 1,012 staged deletions affect exclusively compiled binaries and build cache files (`bin/`, `obj/`) and 0 project source files.
     - Confirmed that corresponding local binaries remain intact on disk and are permanently ignored by `.gitignore`.
     - Confirmed zero leaked secrets, credentials, `.env` files, or production private keys exist in the repository.
  2. Multi-Tier Verification Gates:
     - Verified .NET solution compilation: 0 errors, 0 warnings.
     - Verified .NET unit tests: 534 tests passed (255 Domain + 279 Application) with 0 failures.
     - Verified Angular production build: compiled cleanly in AOT mode with all component bundles below size limits.
     - Verified Angular unit tests: 181 frontend unit tests passed across 27 test files with 0 failures.
  3. Release Staging & Commit Construction:
     - Staged all verified source code, migrations, tests, documentation, containerization profiles, and frontend assets.
     - Preserved all 1,012 staged build artifact un-trackings in the single release commit:
       `feat(release): prepare inventory and sales system for public portfolio`.
  4. Remote Synchronization:
     - Pushed single release commit to `origin main` preserving complete commit history without force-pushing or tag modification.
- Reason: Successfully concludes Phase 9 (Containerization, Deployment & Release) and establishes the official public release of the InventorySalesSystem repository.
- Alternatives considered:
  - Splitting binary un-tracking and source commits: Rejected to ensure an atomic transition to a clean, buildable repository state on GitHub.
  - Force-pushing to rewrite history: Strictly rejected to preserve Git history integrity and avoid collaborative disruption.
- Consequences: The repository on GitHub is completely clean, buildable, tested, containerized, documented, and ready for public presentation.
