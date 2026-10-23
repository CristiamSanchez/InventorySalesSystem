# Project State

## Current Phase

Phase 9.5 — Final Release, Commit, and GitHub Publication (Completed)

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
- Phase 2.1 — Inventory Domain
- Phase 2.2 — Inventory Application
- Phase 2.3 — Inventory Persistence
- Phase 2.4 — Inventory API
- Phase 2.5 — Inventory Concurrency and Data Consistency
- Phase 3.1 — Sales Domain Planning
- Phase 3.2 — Sales Domain Implementation
- Phase 3.3 — Sales Application Foundation
- Phase 3.4 — Sales Persistence
- Phase 3.5 — Atomic Sales Confirmation and Inventory Deduction
- Phase 3.6 — Sales API
- Phase 4.1 — Customer Domain Planning
- Phase 4.2 — Customer Domain Implementation
- Phase 4.3 — Customer Application Layer
- Phase 4.4 — Customer Persistence
- Phase 4.5 — Customer API
- Phase 4.6 — Integrate Customers with Sales
- Phase 5.1 — Supplier Domain Planning
- Phase 5.2 — Supplier Domain Implementation
- Phase 5.3 — Supplier Application Layer
- Phase 5.4 — Supplier Persistence
- Phase 5.5 — Supplier API
- Phase 5.6 — Plan Supplier Integration with Inventory
- Phase 5.7 — Implement Supplier Integration with Inventory
- Phase 5.8 — Supplier Module Closure Review
- Phase 6.1 — Audit Domain Planning
- Phase 6.2 — Audit Domain Implementation
- Phase 6.3 — Audit Application Layer
- Phase 6.4 — Audit Infrastructure Persistence
- Phase 6.5 — Integrate Audit with Sales and Inventory
- Phase 6.6 — Audit Integration Closure Review
- Phase 6.7 — Audit Read-Only API
- Phase 6.8 — Audit Module Closure Review
- Phase 7.1 — Reports Domain Planning
- Phase 7.2 — Reports Application & Infrastructure Implementation
- Phase 7.3 — Reports API Endpoints & Authorization
- Phase 7.4 — Reports Module Closure Review
- Phase 8.1 — Frontend Foundation Planning
- Phase 8.2 — Angular Workspace Setup & Core Architecture
- Phase 8.3 — Frontend Authentication & API Client Foundation
- Phase 8.4 — Shared API Client & Catalog Management
- Phase 8.5 — Customers & Suppliers Management
- Phase 8.6 — Inventory Management & Movement Traceability
- Phase 8.7 — Sales Management Frontend
- Phase 8.8 — Reports & Analytics Frontend
- Phase 8.9 — Audit Log Frontend & System Monitoring
- Phase 8.10 — Frontend Integration Review & End-to-End Validation
- Phase 8.11 — Professional Documentation & Local Setup
- Phase 9.1 — Containerization & CI Foundation
- Phase 9.2 — Repository Hygiene and Security Review
- Phase 9.3 — Git Cleanup and GitHub Release Preparation
- Phase 9.4 — Professional README and Real Application Screenshots
- Phase 9.5 — Final Release, Commit, and GitHub Publication

## Current Work

Completed Phase 9.5 — Final Release, Commit, and GitHub Publication:
- Release Boundary & Safety Auditing:
  - Verified target remote matches `https://github.com/CristiamSanchez/InventorySalesSystem.git` on branch `main`.
  - Confirmed exactly 1,012 staged build artifact deletions affect exclusively `bin/` and `obj/` items, with local files intact and excluded via `.gitignore`.
  - Confirmed zero secret exposure, credentials, or `.env` credential files in the repository.
- Multi-Tier Quality Verification:
  - .NET solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS (0 warnings, 0 errors).
  - Domain and Application unit tests: PASS (534 total passed: 255 Domain + 279 Application, 0 failed).
  - Angular production build (`npm run build`): PASS (0 errors, all chunks within budget).
  - Angular unit tests (`npm test -- --watch=false`): PASS (181 passed across 27 test files, 0 failed).
  - Git diff formatting (`git diff --check` and `git diff --cached --check`): PASS (0 whitespace/formatting errors).
- Staging & Release Commit:
  - Staged all verified source code, migrations, tests, documentation, containerization profiles, and frontend assets while preserving the 1,012 staged build-artifact deletions.
  - Authored official release commit: `feat(release): prepare inventory and sales system for public portfolio`.
  - Pushed to `origin main` synchronizing local branch with GitHub remote.
- Documentation & State Record:
  - Appended Decision 54 to `docs/decision-log.md`.
  - Updated `docs/project-state.md` to mark Phase 9.5 Completed.

## Next Phase

Continuous Maintenance & Future Roadmap Enhancements (e.g. manual screenshots of authenticated screens as optional future enhancement).

## Phase 9.5 Final Release and GitHub Publication Decisions

- Verified Release Integrity: All 534 backend unit tests, 181 frontend unit tests, .NET 10 compilation, and Angular AOT production build passed with zero errors prior to release staging.
- Atomic Public Synchronization: Preserved 1,012 legacy build artifact deletions while staging all verified domain modules, minimal APIs, Angular frontend, container definitions, and documentation into a single atomic release commit.
- Strict Non-Destructive Publishing: Committed and pushed to `origin main` without history rewriting, force-pushing, or tag disruption.

## Phase 9.4 Professional README and Screenshots Decisions

- Authentic Live UI Capture: Captured `01-login.png` directly from the live Angular application in a headless browser at 1920×1080 in light mode, preserving true application aesthetics without mock tools.
- Strict Zero-Mock Policy: Refrained from generating synthetic screenshot mockups for authenticated screens (02–12); instead, documented reproducible manual capture procedures and test data guidelines in `docs/screenshots/README.md`.
- Portfolio Visual Walkthrough: Integrated visual screenshot preview and feature mapping in root `README.md` to showcase the standalone Angular 22 frontend and Clean Architecture design.

## Phase 9.3 Git Cleanup and Release Preparation Decisions

- Verified Pathspec Cleanup: Purged exactly 1,012 tracked build outputs using `git rm -r --cached ':(glob)**/bin/**' ':(glob)**/obj/**'` with zero local filesystem deletion.
- Remote & Clone URL Alignment: Synchronized documentation with official repository remote `https://github.com/CristiamSanchez/InventorySalesSystem.git`.
- Native Mermaid Visualization: Upgraded architecture diagrams to responsive Mermaid format rendering cleanly on GitHub dark and light modes.
- Screenshot Specification Standard: Authored `docs/screenshots/README.md` cataloging the 12 production views with exact resolutions and file naming conventions.

## Phase 9.2 Repository Hygiene and Security Review Decisions

- Authoritative Artifact Remediation Strategy: Identified and audited exactly 1,012 legacy build binaries tracked in the Git index; formulated and verified the safer, cross-platform Git native pathspec command (`git rm -r --cached ':(glob)**/bin' ':(glob)**/obj'`) while strictly refraining from running index mutations without explicit developer consent.
- Zero-Secret Certification: Formally verified that no credentials, tokens, certificates, private keys, or `.env` files are tracked in the Git index or leaked into Docker build contexts.
- Configuration & Port Contract Alignment: Verified 100% agreement between documented ports, compose service definitions, Nginx reverse proxy routes, and .NET launch profiles.

## Phase 9.1 Containerization & CI Decisions

- Multi-Stage Minimal Images: Packaged API and Frontend into optimized multi-stage containers; backend runs as non-root user (`$APP_UID`) on port 8080, and frontend is served via lightweight Nginx on port 80.
- Single-Ingress Reverse Proxy: Nginx serves as the single public HTTP ingress, reverse proxying `/api` and `/auth` internally to the API container without exposing internal ports to the host network in production.
- Decoupled Database Migrations: Migration execution remains an explicit administrative step rather than an automatic container entrypoint command, preventing migration race conditions in multi-replica deployments.
- Parallelized CI Workflows: GitHub Actions pipeline separates backend (.NET 10) and frontend (Node 22 / Angular 22) into concurrent jobs, reducing CI feedback latency without requiring external secrets.
- Non-Destructive Artifact Pruning: Preserved repository history while providing a verified command to unstage tracked legacy binaries.

## Phase 8.11 Professional Documentation & Local Setup Decisions

- Root Git Exclusion Hardening: Created a comprehensive root `.gitignore` to prevent future commits of build binaries, node_modules, secrets, certificates, or local settings.
- Safe Configuration Separation: Decoupled environment secrets from source control via `.env.example`; runtime variables are passed through environment injection without hardcoded credentials.
- Infrastructure-Only Containerization for Local Dev: Containerized PostgreSQL 17 Alpine via Docker Compose while keeping the backend API and Angular client locally runnable for rapid developer feedback.
- Deterministic EF Core Migration Execution: Grounded the database migration procedure in `SistemaInventarioVentas.Infrastructure` as both project and startup-project to align with `CatalogDbContextFactory` and package dependencies.
- Zero-Credential Administrative Setup: Preserved security invariants by defining an initial admin onboarding workflow combining standard registration (`POST /auth/register`) with direct database role activation.

## Phase 8.10 Frontend Integration Review Decisions

- Test Isolation and Safe Execution: End-to-end integration was executed against a dedicated, isolated PostgreSQL test container (`sistema-integration-db` on port 5439) with full EF Core schema migrations, completely preventing collision with development data or production secrets.
- Automated Real-Backend E2E Validation: Integrated a multi-workflow test runner (`scripts/e2e-integration-validation.mjs`, invoked via `npm run test:e2e`) verifying 159 assertions against the live backend and reverse proxy without mocking.
- Contract Fidelity and Zero-Drift: Confirmed 100% agreement between Angular frontend services, DTOs, interceptors, error parsers, and backend ASP.NET Core Minimal API contracts.
- Respect for Domain Invariants: Confirmed that unsupported operations (product deletion, customer deletion, supplier deletion, sale cancellation, client-side financial arithmetic) are strictly absent in the frontend, preserving domain invariants.
- Clean Architecture Preservation: Backend source code, migrations, and domain rules remained untouched; all integration validation was conducted strictly at the external HTTP/reverse-proxy boundary.

## Phase 8.9 Audit Log Frontend Decisions

- Strict Read-Only Immutability: The audit log interface provides zero mutation controls (no create, edit, or delete actions) for audit events, preserving forensic audit integrity and adhering strictly to `BR-AUD-001` and `BR-AUD-002`.
- Double-Layered Authorization: Protected by `adminGuard` at the routing boundary and `AuthService.isAdmin()` in the component; non-admin users receive a clear restriction view without triggering unauthorized 403 HTTP requests.
- Query Parameter and Sorting Fidelity: Full support for all 6 backend query parameters (`eventType`, `entityName`, `entityId`, `userId`, `fromUtc`, `toUtc`); respects backend chronological sorting (`OccurredAtUtc` desc, `Id` asc).
- Dual-Time Presentation: UTC is rendered as the primary authoritative representation (`yyyy-MM-dd HH:mm:ss UTC`) for forensic auditing across timezones, with local browser time provided as secondary context.
- Safe JSON Metadata Inspection: Formatted metadata inspection in modal; credentials excluded by domain design and safely pretty-printed.
- Dependency-Free Operational Metrics: Calculated in-memory from the active audit dataset without inventing synthetic OS health checks.

## Phase 8.8 Reports & Analytics Frontend Decisions

- Authoritative Backend Calculations & Invariant Compliance: The frontend performs zero financial arithmetic or aggregation. Revenue metrics strictly follow backend semantics where only Confirmed sales generate realized revenue. Product sales calculations use immutable line-item snapshots.
- Consolidated Reporting Client with Composition: `ReportService` provides a unified entry point for all 4 reporting endpoints, reusing established `SaleService` and `InventoryService` parameter serialization and HTTP handling while introducing dedicated typing for `/api/reports/product-sales`.
- Role-Gated Access & Graceful Non-Admin Experience: Financial reports are restricted to Administrators. Non-admin users default to operational inventory reports and see an informative restriction message without triggering unauthorized 403 HTTP requests.
- Query Filter Fidelity: The movement traceability report table renders the audited `userId` attribution snapshot, but refrains from providing a user ID filter in the query form because the backend API does not support filtering movements by UserId.
- Dependency-Free Visualizations: Lightweight CSS progress bars and ranking lists illustrate revenue share and stock availability without the overhead, bundle penalties, or licensing issues of third-party charting libraries.
- Accurate Date Boundaries: Date inputs are formatted to exact UTC day boundaries (`T00:00:00.000Z` and `T23:59:59.999Z`) with client-side validation asserting `fromDate <= toDate`.

## Phase 8.6 Inventory Management Frontend Decisions

- Authoritative Server Calculations: The frontend never calculates or submits replacement stock balances; all stock transitions occur through atomic entry, exit, and signed-delta adjustment API endpoints.
- Separation of Stock Availability and Historical Traceability: Stock status (`/api/reports/inventory-status`) and movement traceability (`/api/reports/inventory-movements`) are presented in dedicated tabs with distinct metrics, preventing confusion between current inventory levels and cumulative transaction history.
- Non-Optimistic UI Updates: Upon successful operation submission, the frontend does not manually alter client-side stock counts. Instead, it triggers authoritative reloads of the report and movement endpoints to preserve absolute consistency.
- Admin-Only Mutation Gating: Creation triggers and the operation modal are restricted to admin users (`authService.isAdmin()`), adhering to backend Minimal API `AdminOnly` policies, while read-only stock and movement tracking remain accessible to all authenticated operators.

## Phase 8.5 Customers & Suppliers Management Decisions

- Explicit Absence of Entity Deletion: Neither Customers nor Suppliers support HTTP DELETE operations in the backend. Sales history links to customer IDs and inventory movement traceability links to supplier IDs. The UI faithfully reflects this invariant by providing activation/deactivation toggles (`POST /{id}/activate`, `POST /{id}/deactivate`) instead of destructive deletion controls.
- Independent Entity Contracts: Distinct TypeScript models and services were authored for Customers and Suppliers. Supplier contracts include `contactName`, whereas Customer contracts do not.
- Global UI Layout Utility Promotion: Repeated layout styles (`page-header`, `toolbar-card`, `filters-row`, `state-card`, `actions-group`, `id-pill`, `form-row`) were consolidated into `styles.css`. This keeps feature component styles lightweight (< 1kB) and well within the Angular CLI 4kB budget.
- Admin-Only Mutation Gating: Template-level permission checks hide create, edit, and toggle triggers from non-admin users via `authService.isAdmin()`, aligning with backend Minimal API `AdminOnly` endpoint policies.

## Phase 8.4 Shared API Client & Catalog Management Decisions

- No Product Aggregate Deletion: In alignment with backend domain rules (preserving historical inventory movements and sales line items), products cannot be deleted. The UI provides quick status toggling (`isActive: boolean`) via `PUT /api/products/{identifier}` instead of a destructive delete button.
- Category Deletion Guard: Category deletion via `DELETE /api/categories/{id}` returns 409 Conflict if products are attached. The UI warns the user before submission and surfaces domain conflicts safely through `parseApiError`.
- Clean Separation of Stock and Price: `ProductResponse` contains catalog identity attributes (`identifier`, `name`, `categoryId`, `categoryName`, `isActive`). Real-time stock levels and sales pricing are decoupled from catalog definition, avoiding fabricated fields in the catalog UI.
- Scoped Global Utilities: Modal cards, form controls, alerts, and spinner keyframes were consolidated into `src/styles.css`, keeping component-specific stylesheets lightweight (< 3kB) and well within Angular CLI's 4kB bundle budget without loosening warning thresholds.
- Admin-Only Mutation Gating: Creation, modification, and deletion buttons are strictly hidden from non-admin users in the template, while backend endpoints enforce `AdminOnly` authorization policies.

## Phase 8.3 Frontend Authentication & API Client Decisions

- Session Storage Strategy: Preserved strict sessionStorage isolation for JWT access tokens and UTC expiration timestamps per Decision 41; no persistent localStorage or refresh token mechanisms.
- Target URL Whitelisting: `authInterceptor` explicitly tests URL paths against `/api` and `/auth` relative endpoints and same-origin absolute URLs, guaranteeing tokens are never leaked to external origins or static asset requests.
- 401 Loop Prevention: `errorInterceptor` checks `req.url.includes('/auth/login')` and current router state, preventing redirect loops during invalid credential submissions.
- 403 Non-Destructive Handling: `errorInterceptor` preserves user session and avoids logout when 403 Forbidden is returned, allowing caller components to surface permission denial gracefully.
- Startup Validation: `provideAppInitializer` restores session from sessionStorage and re-validates the token through `/auth/me`; invalid or expired tokens are wiped silently without breaking bootstrap.
- Registration UI Exclusion: Excluded registration UI from Phase 8.3 because backend `/auth/register` creates inactive user accounts pending administrative activation, which requires a separate administrative product decision.

- Backend Port Alignment: Configured `proxy.conf.json` to target `http://localhost:5008` based on verified `launchSettings.json` HTTP launch profile.
- TypeScript Path Resolution: Modernized `tsconfig.json` path aliases (`@core`, `@shared`, `@features`, `@env`) to use relative `./src/...` mappings without deprecated `baseUrl` to ensure forward compatibility with TypeScript 6.0/7.0.
- Native Template Icons: Used native template SVG rendering with `@switch` in `SidebarComponent` instead of `[innerHTML]` to eliminate Angular DOM sanitization warnings and prevent attribute stripping.
- Modular Lazy Loading: Structured `app.routes.ts` with route-level `loadComponent` dynamic imports, resulting in isolated lazy chunks for layout and all 10 feature placeholder routes.
- Component Design System: Built purely dependency-free CSS design system using native variables and semantic HTML, ensuring zero third-party UI framework bloat.

## Phase 8.1 Frontend Foundation Planning Decisions

- Framework & version: Angular 22 standalone architecture (`standalone: true` default, no NgModule boilerplate), strict TypeScript, Vite dev server, esbuild bundler.
- Workspace location: `frontend/` directory at repository root, cleanly separating client code from .NET solution.
- Token storage & security: `sessionStorage` for short-lived JWT access tokens and `ExpiresAtUtc`, scoped to browser tabs, balancing convenience with credential isolation. No refresh token mechanism (backend does not support it).
- Authoritative boundary: Server-side Minimal API endpoints remain the sole security authority; client route guards and UI directives are usability and navigation aids.
- Error handling: RFC 7807 `ProblemDetails` and `ValidationProblemDetails` parsed and surfaced via reactive form errors and alert toasts.
- UI & Styling: Semantic modern HTML5 (`<dialog>` modal, form controls with `:user-valid`/`:user-invalid` feedback, CSS custom properties) avoiding heavy external paid component libraries.
- Local dev networking: Angular CLI `proxy.conf.json` proxying `/api` and `/auth` to ASP.NET Core API on port 5000, eliminating CORS issues and matching production reverse-proxy semantics.
- Backend gaps identified: User administration is unsupported in backend (deferred from UI); sales history is currently `AdminOnly`; sale cancellation API endpoint is absent.

## Phase 7.1 Reports Domain Planning Decisions

- MVP report selection: Sales Summary & History, Product Sales Performance, Inventory Stock Status, and Inventory Movement Traceability.
- Audit report rejection: Duplicate audit reporting endpoint is rejected; existing `/api/audit` endpoints already fulfill administrative audit needs.
- Clean Architecture read-model boundaries: Domain remains untouched. Application defines queries/DTOs. Infrastructure executes `.AsNoTracking()` EF Core projections against PostgreSQL. API exposes read-only Minimal API endpoints.
- Authorization matrix: Financial revenue reports restricted to `AdminOnly`; operational inventory reports accessible to both `User` and `Admin`.
- Revenue calculation rule: Financial metrics aggregate strictly `Confirmed` sales (`BR-REP-004`).
- Historical pricing integrity: Detail calculations use immutable snapshot prices on `sale_details` (`BR-REP-005`).
- Uninitialized stock handling: Products without inventory records report stock zero (`HasInventoryRecord = false`) (`BR-REP-006`).
- Side-effect and audit exemption: Reporting queries never acquire persistent locks, mutate state, or produce audit log entries (`BR-AUD-004`, `BR-REP-007`).
- Unsupported metrics: COGS, profit margins, inventory asset valuation, and dynamic reorder alerts are unsupported due to missing product cost and min-stock data.
- Deferred capabilities: PDF/Excel/CSV exports, async generation/queues, and OLAP data warehouses/read replicas remain deferred.

## Phase 6.1 Audit Domain Planning Decisions

- Operational event scope: State-changing operational actions across Sales, Inventory, Product/Supplier/Customer/User lifecycles, and Authentication are audited. Read queries are strictly excluded.
- Append-only immutability: `AuditEvent` records are permanent and immutable. No updates or deletions are permitted.
- In-band transactional guarantees: Audit events for state-modifying business operations share the primary entity's database transaction, preventing phantom audit entries if transactions abort.
- Zero secret disclosure: Passwords, password hashes, JWT access tokens, and cryptographic keys are strictly excluded from audit payloads.
- Architecture boundaries: Domain owns `AuditEvent`; Application orchestrates logging via `IAuditRepository`; Infrastructure handles EF Core mapping to `audit_events` table in PostgreSQL; API exposes read-only endpoints under `/api/audit` for Admins.
- Deferred items: Event sourcing, asynchronous message queues (Kafka/RabbitMQ/Outbox), third-party auditing libraries, and log retention/archiving policies remain deferred.



## Phase 5.6 Supplier-Inventory Integration Decisions

- Granular movement-level association: `SupplierId` belongs on `InventoryMovement` (specifically incoming entries), not on the `Inventory` aggregate root.
- Hybrid sourcing snapshot: `InventoryMovement` stores nullable `Guid? SupplierId` and nullable `string? SupplierName` (`varchar(200)`), preserving historical delivery records permanently (`BR-SUP-006`).
- Active supplier governance: `InventoryUseCases` validates that `supplierId` exists and is active (`BR-SUP-003`).
- Backwards compatibility: Non-supplier entries, stock exits, adjustments, and historical records remain fully valid with null supplier properties.
- Deletion protection: `DeleteBehavior.Restrict` blocks deleting supplier records that have associated movement history (`BR-SUP-004`).
- No purchasing/catalog creep: Purchase orders, accounts payable, and product-supplier catalog links remain cleanly deferred.

## Phase 5.5 Supplier API Decisions

- Minimal API route group `/api/suppliers` exposes supplier CRUD and lifecycle transitions.
- DTO encapsulation: `CreateSupplierRequest`, `UpdateSupplierRequest`, and `SupplierResponse` decouple API consumers from internal domain and persistence representations.
- Authorization: All supplier endpoints require authentication (`RequireAuthorization()`), returning HTTP 401 Unauthorized for anonymous callers. Reads (`GET /`, `GET /{id}`) allow `User` and `Admin` roles. Mutations (`POST /`, `PUT /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`) require `AdminOnly` (`RequireRole("Admin")`), returning HTTP 403 Forbidden for non-Admin authenticated callers.
- Error handling: `SupplierIdentificationConflict` maps to HTTP 409 Conflict ("Supplier conflict") via `ApplicationProblemMapping`. Validation errors return HTTP 400 `ValidationProblemDetails`. Missing supplier entities return HTTP 404 `ProblemDetails`.
- Explicit lifecycle commands: Supplier activation and deactivation are exposed via dedicated `POST /api/suppliers/{id:guid}/activate` and `POST /api/suppliers/{id:guid}/deactivate` endpoints, maintaining consistency with existing action command conventions.

## Phase 5.1 Supplier Domain Planning Decisions

- Supplier aggregate: `Supplier` is modeled as an independent Aggregate Root (`Id`, `Name`, `ContactName`, `IdentificationNumber`, `Email`, `Phone`, `Address`, `IsActive`, `CreatedAtUtc`).
- Contact details: `ContactName`, `Email`, `Phone`, and `Address` are optional to accommodate informal/local suppliers, but strictly normalized and validated when provided (`BR-SUP-005`).
- Identification uniqueness: Optional `IdentificationNumber` must be unique across all suppliers case-insensitively (`BR-SUP-002`). Multiple nulls coexist.
- Active/inactive status: Inactive suppliers cannot be used for recording new inventory entries (`BR-SUP-003`).
- Sourcing boundaries: Products do not require a mandatory supplier link; supplier relationships belong to incoming inventory entries and future purchasing workflows.
- Immutability and deletion protection: Suppliers tied to historical inventory movements cannot be deleted (`BR-SUP-004`).
- Deferred items: Complex purchasing workflows (POs, vendor bills, AP) and catalog product-supplier mappings remain explicitly deferred.

## Phase 4.6 Customer-Sales Integration Decisions

- Hybrid snapshot and relational link: Sale stores nullable `CustomerId` for relational integrity, reporting, and customer history, while snapshotting the customer name into `CustomerReference` at creation or assignment time (`BR-CUST-006`).
- Historical immutability: Subsequent changes to customer records in the catalog (renaming, deactivating) never alter past sales' snapshotted names.
- Walk-in compatibility: `CustomerId` is optional; walk-in sales without registered customers remain fully supported by providing `CustomerReference` directly.
- Inactive customer rejection: New sales and customer reassignment strictly reject inactive customers (`BR-CUST-003`).
- Lifecycle restriction: Confirmed or cancelled sales cannot have their customer association or snapshotted name altered (`BR-SALE-014`).
- Deletion restriction: `DeleteBehavior.Restrict` prevents deleting customer records from the database if associated sales exist (`BR-CUST-004`).

- Minimal API route group `/api/customers` exposes customer CRUD and lifecycle transitions.
- DTO encapsulation: `CreateCustomerRequest`, `UpdateCustomerRequest`, and `CustomerResponse` decouple API consumers from internal domain and persistence representations.
- Authorization: All customer endpoints require authentication (`RequireAuthorization()`), returning HTTP 401 Unauthorized for anonymous callers. Reads (`GET /`, `GET /{id}`) allow `User` and `Admin` roles. Mutations (`POST /`, `PUT /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`) require `AdminOnly` (`RequireRole("Admin")`), returning HTTP 403 Forbidden for non-Admin authenticated callers.
- Error handling: `CustomerIdentificationConflict` maps to HTTP 409 Conflict via `ApplicationProblemMapping`. Validation errors return HTTP 400 `ValidationProblemDetails`. Missing customer entities return HTTP 404 `ProblemDetails`.
- Explicit lifecycle commands: Customer activation and deactivation are exposed via dedicated `POST /api/customers/{id:guid}/activate` and `POST /api/customers/{id:guid}/deactivate` endpoints, maintaining consistency with existing action command conventions.

## Phase 4.4 Customer Persistence Decisions

- Schema mapping: Customer entity maps to `customers` table (`Id` uuid PK, `Name` varchar(200), `IdentificationNumber` varchar(50), `Email` varchar(320), `Phone` varchar(30), `Address` varchar(300), `IsActive` boolean, `CreatedAtUtc` timestamp with time zone).
- Case-insensitive identification uniqueness: Computed column `IdentificationNumberKey = upper("IdentificationNumber")` stored in PostgreSQL with unique index `ux_customers_identification_number_key`. PostgreSQL treats nulls as distinct, allowing multiple customers without identification numbers while enforcing strict uniqueness for provided numbers.
- Value converter ILike query pattern: `Email` uses `ValueConverter<EmailAddress?, string?>`. Direct `EF.Functions.ILike(customer.Email, pattern)` attempts to convert string pattern to `EmailAddress`, throwing `InvalidCastException`. Added stored computed column `EmailKey = lower("Email")` (CLR type `string`) to allow native EF ILike queries on `EmailKey`.
- Exception translation and boundary preservation: Infrastructure catches `DbUpdateException` specifically where `SqlState == PostgresErrorCodes.UniqueViolation` and constraint name matches `ux_customers_identification_number_key`, throwing `DuplicateCustomerIdentificationException`. `CustomerUseCases` catches this exception and returns `ApplicationErrorCode.CustomerIdentificationConflict`, preventing PostgreSQL provider exceptions from escaping to outer layers.
- Sales isolation preserved: `Sales` table and `Sale` aggregate were not altered. Linking customer to sales remains deferred to Phase 4.6.

## Phase 4.1 Customer Domain Planning Decisions

- Customer aggregate: `Customer` is modeled as an independent Aggregate Root (`Id`, `Name`, `IdentificationNumber`, `Email`, `Phone`, `Address`, `IsActive`, `CreatedAtUtc`).
- Mandatory vs. Optional fields: `Name` is required (1–200 characters, trimmed). Contact fields (`Email`, `Phone`, `Address`) and `IdentificationNumber` are optional to support walk-in, counter, and wholesale commerce without forcing dummy data. Whenever supplied, fields are trimmed and validated.
- Uniqueness & duplicate prevention: `IdentificationNumber`, when provided, must be unique across customers (case-insensitively).
- Active status and sales invariant: Inactive customers cannot be used for new sales or new draft sale associations (`BR-CUST-003`). Deactivation does not affect past sales. Customers with sales must never be deleted (`BR-CUST-004`).
- Hybrid Sale linking: `Sale` references `CustomerId` (`Guid?`, identifier by value) and maintains `CustomerReference` (`string`) as an immutable commercial name snapshot, preserving historical accuracy when a customer is renamed and supporting unregistered/walk-in sales without breaking changes to existing database schema, APIs, or tests.

## Phase 3.6 Sales API Decisions

- Minimal API route group `/api/sales` exposes draft creation, retrieval, detail modifications, and atomic confirmation.
- DTO encapsulation: `CreateSaleRequest`, `AddSaleDetailRequest`, `UpdateSaleDetailRequest`, `SaleResponse`, and `SaleDetailResponse` decouple API consumers from internal domain and persistence representations.
- Flexible detail identification: `PUT` and `DELETE` endpoints on `/api/sales/{id:guid}/details/{detailIdOrProductIdentifier}` dynamically parse the path parameter as a Guid (detail ID) or string (product identifier), maintaining a unified route pattern.
- Pricing limitation: Callers explicitly pass `UnitPrice` snapshots and `TaxRate` because catalog pricing and tax engine rules are not yet defined. Documentation and XML contracts explicitly mark this as an untrusted client limitation pending future catalog/pricing engine integration.
- Authentication & Authorization: Endpoints require JWT authentication. Reads allow `User` and `Admin` roles; mutations (`POST`, `PUT`, `DELETE`) require `AdminOnly` (`RequireRole("Admin")`), returning 403 Forbidden for non-Admin authenticated callers and 401 Unauthorized for anonymous callers.
- Error handling: Domain invariants and use-case outcomes map cleanly via `ApplicationProblemMapping` to HTTP 400 Bad Request (`InvalidInput`), HTTP 404 Not Found (`NotFound`), and HTTP 409 Conflict (`InsufficientStock`, `ConcurrencyConflict`). Detail mutations on confirmed sales throw domain `InvalidOperationException` and return HTTP 400 Bad Request.

## Phase 3.5 Atomic Sales Confirmation and Inventory Deduction Decisions

- Application orchestration: `SaleUseCases.ConfirmSaleAsync` pre-validates that the target sale exists, is in `Draft` status, and has $\ge 1$ detail lines. It aggregates required product quantities, checks every inventory record, and immediately returns `ApplicationErrorCode.InsufficientStock` if any product lacks stock, without mutating in-memory domain state or opening database transactions.
- Domain delegation: Invokes `Sale.Confirm(effectiveConfirmedAtUtc)` and `Inventory.RegisterExit(detail.Quantity, reference: $"Sale {sale.Id}", effectiveConfirmedAtUtc)` to ensure business rules and audit references are enforced in the domain layer.
- Transaction management: `ISaleRepository.ConfirmSaleAsync` coordinates the technical transaction in Infrastructure using `strategy.ExecuteAsync(...)` and `await dbContext.Database.BeginTransactionAsync(cancellationToken)`.
- Concurrency and Invariant Protection: Re-verifies in the database inside the transaction that the sale status is still `Draft` (`currentStatus == SaleStatus.Draft`). Reuses the existing optimistic concurrency token (`xmin` on `Inventory`) and check constraint (`ck_inventories_quantity_non_negative`).
- Rollback and Error Translation: `DbUpdateConcurrencyException`, check constraint violations (`PostgresErrorCodes.CheckViolation`), and unique index conflicts roll back the transaction and throw `ConcurrencyException`, which `SaleUseCases` translates to `ApplicationErrorCode.ConcurrencyConflict`.
- Re-confirmation safety: Sequential re-confirmation attempts are rejected with `ApplicationErrorCode.InvalidInput` because `sale.Status == SaleStatus.Confirmed`. Concurrent re-confirmation attempts fail on inventory `xmin` OCC or database status check and roll back cleanly, ensuring stock is never deducted twice.
- Test coverage: 13 focused unit tests in `SaleUseCasesTests` (validating validation errors, insufficient stock, missing repository dependencies, cancellation tokens, and concurrency translations) and 7 PostgreSQL Testcontainers integration tests in `SaleConfirmationPersistenceTests` (validating atomic commits, multi-item exits, atomic rollbacks on partial insufficient stock, concurrent race isolation, and repeated confirmation safety).

## Phase 3.4 Sales Persistence Decisions

- `Sale` maps to `sales` table: primary key `Id` (uuid, value generated never), `CustomerReference` (varchar(200)), `UserId` (uuid, indexed, without speculative foreign keys to unbuilt Customer/User aggregates), `CreatedAtUtc` and `ConfirmedAtUtc` (timestamp with time zone), integer `Status` (indexed), and decimal monetary values `Subtotal`, `TaxAmount`, `Total` with precision (18,2). Check constraints enforce non-negative monetary amounts (`ck_sales_subtotal_non_negative`, `ck_sales_tax_amount_non_negative`, `ck_sales_total_non_negative`).
- `SaleDetail` maps to `sale_details` table: primary key `Id` (uuid, value generated never), `SaleId` (uuid, FK to sales(Id) with cascade delete), `ProductIdentifier` (varchar(100) via value converter, indexed), `Quantity` (integer), `UnitPrice` (numeric(18,2)), `TaxRate` (numeric(18,4)), `Subtotal` (numeric(18,2)), `TaxAmount` (numeric(18,2)), `Total` (numeric(18,2)). Check constraints enforce positive quantity and non-negative unit price, tax rate, and amounts.
- Composite unique index `ux_sale_details_sale_id_product_identifier` on `(SaleId, ProductIdentifier)` guarantees at the database level that duplicate product lines cannot exist within a single sale.
- Encapsulation: `Sale.Details` is mapped via private backing field `_details` using `PropertyAccessMode.Field`.
- `SaleRepository` implements `ISaleRepository`: `GetByIdAsync` includes detail lines; `AddAsync` persists new sales and detail lines; `SaveAsync` supports tracked modifications as well as detached entity synchronization (adding, updating, and deleting orphan lines).
- Scoped DI registration: `ISaleRepository` is registered in `PersistenceServiceCollectionExtensions.AddPersistence`.
- Migration: `20261019170912_AddSales` verified against PostgreSQL 17 in Testcontainers with 0 pending model changes.

## Phase 3.3 Sales Application Foundation Decisions

- `ISaleRepository` defines the minimum persistence abstraction: `GetByIdAsync(Guid id, CancellationToken cancellationToken = default)`, `AddAsync(Sale sale, CancellationToken cancellationToken = default)`, and `SaveAsync(Sale sale, CancellationToken cancellationToken = default)`.
- `SaleUseCases` coordinates draft sale workflows: `CreateDraftAsync`, `GetByIdAsync`, `AddDetailAsync`, `UpdateDetailAsync`, `RemoveDetailAsync`, and `UpdateCustomerReferenceAsync`.
- Business rules and financial math (positive quantity, non-negative unit price snapshot, tax rate, draft-only mutations, duplicate line incrementing, decimal rounding via `MidpointRounding.AwayFromZero`) are strictly delegated to `Sale` domain methods.
- Domain exception translation: `ArgumentException` and `ArgumentOutOfRangeException` map to `ApplicationErrorCode.InvalidInput`; `InvalidOperationException` (mutations on non-draft sales) maps to `ApplicationErrorCode.InvalidInput` or not-found as appropriate.
- Catalog validation: `SaleUseCases` accepts an optional `IProductRepository`. When present, `AddDetailAsync` validates that the product exists in the catalog before creating or updating the line, returning `ApplicationErrorCode.NotFound` if missing.
- Price snapshots: Unit prices are explicitly supplied by callers when adding/updating details, preserving the immutable snapshot principle.
- Cancellation token propagation: All async use cases take and propagate `CancellationToken` to repository operations.
- Confirmation, inventory exits, persistence implementations, and API endpoints remain deferred to subsequent phases.

## Phase 3.2 Sales Domain Implementation Decisions

- `SaleStatus` enum defines `Draft = 1`, `Confirmed = 2`, `Cancelled = 3`.
- `Sale` aggregate root encapsulates details via private `_details` list and `ReadOnlyCollection<SaleDetail>` view.
- Mutating operations (`AddDetail`, `UpdateDetail`, `UpdateDetailQuantity`, `RemoveDetail`, `UpdateCustomerReference`) strictly require `Status == SaleStatus.Draft`.
- Adding an existing product identifier increments the existing detail line's quantity using checked arithmetic and recalculates line and sale totals.
- Financial arithmetic in `SaleDetail` and `Sale` consistently uses `MidpointRounding.AwayFromZero` rounded to two decimal places (`Subtotal`, `TaxAmount`, `Total`).
- Price snapshot: `UnitPrice` is captured explicitly when adding or updating details; catalog price retrieval remains deferred to the Application phase.
- Lifecycle transitions: `Confirm()` validates `Draft` status and non-empty details before transitioning to `Confirmed` and setting `ConfirmedAtUtc`. `Cancel()` transitions `Draft` or `Confirmed` to `Cancelled`. All invalid transitions throw `InvalidOperationException`.
- Inventory operations, stock reversals, persistence, and API endpoints remain deferred to later phases.

## Phase 3.1 Sales Domain Planning Decisions

- Entity model: `Sale` aggregate root (`Id`, `CustomerReference`, `UserId`, `CreatedAtUtc`, `ConfirmedAtUtc`, `Status`, `Subtotal`, `TaxAmount`, `Total`, `_details`) and `SaleDetail` child entity (`Id`, `SaleId`, `ProductIdentifier`, `Quantity`, `UnitPrice`, `TaxRate`, `Subtotal`, `TaxAmount`, `Total`).
- Lifecycle: `SaleStatus` enum (`Draft = 1`, `Confirmed = 2`, `Cancelled = 3`). Draft allows detail line modifications without stock consumption. Confirmation is an explicit state transition that locks lines and triggers stock deduction. Cancelled is a terminal void state.
- Financial precision and snapshots: `UnitPrice` is an immutable snapshot taken at line creation; financial totals use standard decimal arithmetic and rounding (`MidpointRounding.AwayFromZero` to 2 decimal places).
- Inventory integration: Confirmed sales invoke `Inventory.RegisterExit` per detail line, recording `MovementType.Exit` referencing the sale identifier (`$"Sale {sale.Id}"`).
- Atomicity: The sale confirmation workflow commits `Sale`, all `SaleDetail`s, all affected `Inventory` stock decrements, and all `InventoryMovement` records in a single atomic database transaction. Any failure rolls back the entire operation.
- Concurrency protection: Protected by the existing inventory OCC (PostgreSQL `xmin` row versioning, Decision 26). Concurrent sales attempting to purchase the same inventory will trigger a concurrency conflict on commit, aborting conflicting sales cleanly without allowing negative stock or orphaned records.
- Explicit unresolved business choices:
  - Customer aggregate decoupling: `Sale` uses `CustomerReference` (string / value object) while Customer aggregate is deferred to Phase 5.
  - Product catalog price: `Product` does not currently store a price; `SaleDetail` captures `UnitPrice` as an immutable snapshot.
  - Multi-tax and jurisdiction rules: Kept flexible with line-level `TaxRate`.
  - Human-readable numbering: Commercial invoice/receipt sequence formats remain deferred.
  - Reversal workflow: Reversal of confirmed sales remains deferred beyond initial creation and confirmation.

## Phase 2.5 Inventory Concurrency Decisions

- Optimistic Concurrency Token: Configured shadow property `builder.Property<uint>("Version").IsRowVersion()` on `Inventory` in `InventoryConfiguration`, mapping to PostgreSQL's native `xmin` system column.
- Database Invariant Check: Added check constraint `ck_inventories_quantity_non_negative` enforcing `"Quantity" >= 0` directly on the `inventories` PostgreSQL table.
- Concurrency Exception Translation: `InventoryRepository` translates `DbUpdateConcurrencyException` and `DbUpdateException` (unique violation on `ux_inventories_product_identifier`) to `ConcurrencyException` in `SistemaInventarioVentas.Application.Exceptions`.
- Detached Entity Concurrency Handling: In `InventoryRepository.SaveAsync`, when saving detached entities, the current database `Version` is fetched and set as `OriginalValue` before attaching, keeping detached tests consistent while EF-tracked instances in production use their loaded version.
- Application Error Code: Added `ApplicationErrorCode.ConcurrencyConflict` and handled `ConcurrencyException` in `InventoryUseCases`, returning `Result<Inventory>.Failure(ApplicationErrorCode.ConcurrencyConflict, ...)`.
- API Error Mapping: Mapped `ApplicationErrorCode.ConcurrencyConflict` to HTTP 409 Conflict with title "Concurrency conflict" in `ApplicationProblemMapping`.
- Movement Integrity: Movement records are added and saved atomically with the `Inventory` aggregate root inside EF Core transactions; a rejected concurrent operation rolls back and does not persist orphaned movement records.
- PostgreSQL Migration: Scaffolding generated migration `20261019160519_AddInventoryConcurrencyAndCheckConstraint` (with `AddColumn("xmin")` / `DropColumn("xmin")` omitted since `xmin` is a native system column).
- Concurrency Integration Tests: Added `InventoryConcurrencyPersistenceTests` (5 integration tests covering concurrent over-withdrawal, simultaneous valid updates, movement history reconciliation, failure isolation, and concurrent product initialization races) and `InventoryConcurrencyApiTests` (2 HTTP API tests verifying 409 Conflict responses for concurrent stock exits and initializations) using PostgreSQL 17 Testcontainers.

## Phase 2.4 Inventory API Decisions

- Routes grouped under `/api/inventory`: `GET /{productIdentifier}`, `GET /{productIdentifier}/movements`, `POST /{productIdentifier}/entries` (also `POST /entries`), `POST /{productIdentifier}/exits` (also `POST /exits`), and `POST /{productIdentifier}/adjustments` (also `POST /{productIdentifier}/adjust` and `POST /adjustments`).
- All inventory endpoints require JWT authentication.
- Read operations (`GET /{productIdentifier}`, `GET /{productIdentifier}/movements`) permit both `User` and `Admin` roles.
- Mutation operations (`POST` entries, exits, adjustments) require the `AdminOnly` authorization policy (`RequireRole("Admin")`), returning 403 Forbidden for non-Admin authenticated users.
- Anonymous callers to any inventory endpoint receive 401 Unauthorized Problem Details.
- Input validation failures return 400 Bad Request Problem Details with validation errors.
- Missing product or inventory records return 404 Not Found Problem Details.
- Insufficient stock invariant violations map to 409 Conflict Problem Details with title "Insufficient stock".
- Request and response DTOs (`RecordStockEntryRequest`, `RecordStockExitRequest`, `AdjustStockRequest`, `InventoryResponse`, `InventoryMovementResponse`) decouple API contracts from Domain and EF Core entities.
- Endpoints delegate directly to `InventoryUseCases`, preserving Clean Architecture layer boundaries.
- No database migrations or concurrency locking were introduced; existing persistence and domain logic remain unchanged.

## Phase 2.3 Inventory Persistence Decisions

- `Inventory` maps to the `inventories` table with UUID primary key `Id`, current stock integer `Quantity`, and `ProductIdentifier` stored as `character varying(100)` with unique index `ux_inventories_product_identifier`.
- `InventoryMovement` maps to `inventory_movements` with UUID primary key `Id`, `ProductIdentifier` (`varchar(100)`), `MovementType` (`integer`), `Quantity` (`integer`), `Reference` (`varchar(500)`), and `OccurredAtUtc` (`timestamp with time zone`).
- `InventoryMovement` has foreign key `InventoryId` referencing `inventories(Id)` with cascade delete, and indexes on `InventoryId`, `ProductIdentifier`, and `OccurredAtUtc`.
- Movements collection is encapsulated via the domain entity's private backing field `_movements` using `PropertyAccessMode.Field`.
- `InventoryRepository` implements `IInventoryRepository` (`GetByProductIdentifierAsync`, `AddAsync`, `SaveAsync`), supporting both tracked entities and detached entities with atomic synchronization of newly added movements.
- `IInventoryRepository` is registered with scoped lifetime in `PersistenceServiceCollectionExtensions.AddPersistence`.
- Migration: `20261019013317_AddInventory`. Existing tables and migrations are unchanged.
- Integration tests in `tests/SistemaInventarioVentas.Infrastructure.Tests/InventoryPersistenceTests.cs` reuse the existing PostgreSQL 17 Testcontainers fixture.

## Phase 2.2 Inventory Application Decisions

- `IInventoryRepository` defines `GetByProductIdentifierAsync`, `AddAsync`, and `SaveAsync`.
- `InventoryUseCases` coordinates inventory workflows and allows optional `IProductRepository` injection to verify catalog product existence.
- Initial entry automatically creates an `Inventory` aggregate root and persists it via `AddAsync` if none exists yet for the product; subsequent entries, exits, and adjustments persist updates via `SaveAsync`.
- Missing inventory on exit, adjustment, or retrieval returns `ApplicationErrorCode.NotFound`.
- Domain invariant violations (such as negative stock resulting from an exit or adjustment) are captured and mapped to `ApplicationErrorCode.InsufficientStock`.
- Input validation failures (blank product identifiers, non-positive entry/exit quantities, zero adjustment delta, and blank references/reasons) map to `ApplicationErrorCode.InvalidInput`.
- Clean Architecture boundaries are preserved: no ASP.NET Core, EF Core, or database dependencies are referenced in Application.

## Phase 2.1 Inventory Domain Decisions

- `Inventory` aggregate root manages current stock for a product referenced by `ProductIdentifier`, avoiding unnecessary coupling to the `Product` entity.
- Initial stock defaults to 0; constructor with initial stock requires a positive quantity and reference, recording an initial `Entry` movement to maintain 100% movement traceability.
- Encapsulation: `Quantity` has a private setter, and `Movements` is exposed via `IReadOnlyCollection<InventoryMovement>` backed by a `ReadOnlyCollection` to prevent external collection tampering.
- `MovementType` is an enum with `Entry = 1`, `Exit = 2`, and `Adjustment = 3`.
- Stock operations:
  - `RegisterEntry` / `RecordEntry` increases stock with positive quantities and requires a non-blank reference.
  - `RegisterExit` / `RecordExit` decreases stock with positive quantities, requires a non-blank reference, and rejects attempts to decrement past available stock with `InvalidOperationException`.
  - `Adjust` / `RegisterAdjustment` / `RecordAdjustment` models stock corrections via a signed non-zero quantity change (`quantityDelta`) and mandatory reason, strictly enforcing the non-negative invariant (`Quantity + quantityDelta >= 0`) and recording `MovementType.Adjustment` with the signed delta.
- Invariant & overflow protection: stock additions and adjustments use `checked` arithmetic to guard against integer overflow, and violations throw `InvalidOperationException`.


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

Phase 3.5 — Sales Confirmation and Inventory Atomic Deduction (or next approved phase). Await human approval before starting.

## Known Issues

- No concrete Angular project structure has been created yet.
- No detailed frontend feature layout has been defined yet.
- Detailed API contracts beyond the current catalog, authentication, and inventory endpoints have not been selected.
- API JWT authentication is implemented; production signing-key provisioning, secure storage, and key rotation remain deployment decisions.
- Dockerfiles, local orchestration files, GitHub Actions workflows, and deployment resources have not been created.
- Database schema outside Category, Product, User, and Inventory and detailed persistence, deployment, and runtime configurations remain to be designed.
- The Application pre-checks remain for user-friendly conflicts; PostgreSQL unique indexes are authoritative under concurrent writes.
- Category uniqueness is case-insensitive; the exact Unicode/collation equivalence between .NET `OrdinalIgnoreCase` and PostgreSQL `lower()` has not been formalized.
- Category removal is blocked by the database while any product (including inactive products) references the category. Reassign inactive products before deletion; the business's finer inactive-product deletion policy remains unresolved.
- Category name (200 characters) and ProductIdentifier (100 characters) database limits are persistence constraints; corresponding business-level maximum-length policy is not otherwise specified.
- Migration deployment/rollback process, database seeding, transaction/concurrency strategy, and production database operations remain deferred.
- Product listing supports category, active status, name, and identifier filters. Supplier filtering remains unavailable until supplier relationships are implemented.
- Product supplier relationships, pricing/cost, tax context, and sales integration remain outside this phase.
- Authorization for sales, supplier, customer, and user-management endpoints, plus frontend authorization, remains unimplemented.
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

### Phase 5.8

- Closure review scope: PASS — Read-only verification of Supplier domain, use cases, persistence configuration, API endpoints, and inventory movements integration against business rules `BR-SUP-001` through `BR-SUP-006` and Decisions 36–38.
- Supplier validation and normalization (`BR-SUP-001`, `BR-SUP-005`): PASS — Domain entity `Supplier` enforces required non-empty Name (1–200 chars), optional ContactName (max 200 chars), uppercase-normalized IdentificationNumber (max 50 chars), lowercase-normalized EmailAddress, Phone (max 30 chars), and Address (max 300 chars).
- Case-insensitive identification uniqueness (`BR-SUP-002`): PASS — Unique index on uppercase-computed column in PostgreSQL (`ux_suppliers_identification_number_key`) with `DuplicateSupplierIdentificationException` mapping to `SupplierIdentificationConflict` (HTTP 409 Conflict). Multiple null identification numbers coexist cleanly.
- Supplier CRUD, lifecycle, and authorization: PASS — Minimal API `/api/suppliers` enforces authentication on all routes, read access for authenticated users, and `AdminOnly` for mutations (`POST /`, `PUT /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`).
- Inactive supplier rejection (`BR-SUP-003`): PASS — `InventoryUseCases.RecordEntryAsync` rejects inactive suppliers with `ApplicationErrorCode.InvalidInput`, mapped to HTTP 400 Bad Request ProblemDetails.
- Supplier name snapshots on movements (`BR-SUP-006`): PASS — `InventoryMovement` captures historical `SupplierName` (`varchar(200)`) at creation time, preserving past movement records permanently regardless of subsequent supplier updates or deactivations.
- Restricted deletion (`BR-SUP-004`): PASS — Foreign key `FK_inventory_movements_suppliers_SupplierId` is configured with `DeleteBehavior.Restrict`, blocking deletion of suppliers with associated movements.
- Backwards compatibility: PASS — Generic inventory entries, stock exits, and adjustments remain fully supported with null supplier fields.
- Production code and migrations: PASS — Unchanged during this review phase.
- Solution build & tests: PASS — 603 passed, 0 failed, 0 skipped across all test projects; 0 pending EF model changes.

### Phase 5.7

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests`): PASS — 198 passed, 0 failed, 0 skipped (including 7 new supplier-inventory domain tests).
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests`): PASS — 221 passed, 0 failed, 0 skipped (including 7 new supplier-inventory application use case tests).
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests`): PASS — 77 passed, 0 failed, 0 skipped (including 3 new supplier-inventory persistence integration tests).
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests`): PASS — 107 passed, 0 failed, 0 skipped (including 5 new supplier-inventory API integration tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx`): PASS — 603 passed, 0 failed, 0 skipped across all test projects.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Supplier-Inventory integration verified against PostgreSQL 17 in Testcontainers:
  - Entry movements with supplier details persist `SupplierId` and snapshot `SupplierName` accurately.
  - Historical snapshot preservation: renaming or deactivating a supplier in the database does not modify historical `SupplierName` on existing movements.
  - Foreign key restriction: deleting a supplier referenced by inventory movements is blocked by PostgreSQL foreign key violation (`DeleteBehavior.Restrict` / `PostgresErrorCodes.ForeignKeyViolation`).
  - Inactive supplier rejection: recording an entry with an inactive supplier returns HTTP 400 Bad Request ("Inactive suppliers cannot be used for recording new inventory entries.").
  - Nonexistent supplier rejection: recording an entry with an unknown supplier ID returns HTTP 404 Not Found ("Supplier was not found.").
  - Backwards compatibility: recording entries without a supplier continues to succeed, persisting null `SupplierId` and null `SupplierName`.
  - Route-based and body-based entry endpoints both correctly accept and process `supplierId`.
- Clean Architecture boundaries preserved: Domain defines movement invariants, Application orchestrates supplier repository validation, Infrastructure owns EF Core mapping and constraints, API acts as thin DTO adapter.

### Phase 5.6

- Documentation verification: PASS — Reviewed and updated `docs/domain-model.md`, `docs/business-rules.md`, `docs/decision-log.md` (Decision 38), and `docs/project-state.md`.
- Architectural boundary alignment: PASS — Clean Architecture boundaries preserved: Domain encapsulates movement invariants; Application orchestrates supplier validation via `ISupplierRepository`; Infrastructure owns EF Core foreign key mapping with `DeleteBehavior.Restrict`; API decouples contracts via DTOs.
- Invariant & rule coverage: PASS — Rules `BR-SUP-003` (inactive supplier rejection), `BR-SUP-004` (deletion protection for suppliers with movement history), and `BR-SUP-006` (historical supplier name snapshot) are completely resolved in the plan.
- Backwards compatibility verification: PASS — Manual/internal receipts, initial stock initialization, stock exits, inventory adjustments, and existing movement records remain fully supported with null supplier properties.
- Production code unchanged: PASS — No C# production source files, tests, or database migrations were created or modified.
- Full solution build: PASS — `dotnet build SistemaInventarioVentas.slnx` compiles with 0 warnings, 0 errors.
- Full test suite: PASS — `dotnet test SistemaInventarioVentas.slnx` passes 579 tests, 0 failed, 0 skipped.

### Phase 5.5

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests`): PASS — 189 passed, 0 failed, 0 skipped.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests`): PASS — 214 passed, 0 failed, 0 skipped.
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests`): PASS — 74 passed, 0 failed, 0 skipped.
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests`): PASS — 102 passed, 0 failed, 0 skipped (including 22 new supplier API integration tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx`): PASS — 579 passed, 0 failed, 0 skipped across all test projects.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Supplier Minimal API endpoints verified against PostgreSQL 17 in Testcontainers:
  - Supplier creation (`POST /api/suppliers`): 201 Created with Location header and `SupplierResponse` payload, verified with full details and minimal details (Name only).
  - Supplier retrieval by ID (`GET /api/suppliers/{id:guid}`): 200 OK for authenticated callers (tested with `User` role to verify read access), and 404 Not Found for non-existent IDs.
  - Supplier listing & search (`GET /api/suppliers`): 200 OK without filters, filtered by `isActive` (true/false), and searched by `searchTerm` matching across Name, ContactName, IdentificationNumber, EmailKey, and Phone.
  - Supplier updating (`PUT /api/suppliers/{id:guid}`): 200 OK for valid updates, self-identification preservation without false conflict, 404 Not Found for missing suppliers, and 409 Conflict for conflicting identification numbers.
  - Lifecycle state transitions: `POST /api/suppliers/{id:guid}/activate` and `POST /api/suppliers/{id:guid}/deactivate` successfully activate/deactivate suppliers and persist status changes (verified in DB), and return 404 Not Found for missing suppliers.
  - Error handling & validation: DataAnnotations and Domain validation failures return 400 ValidationProblem or 400 BadRequest. Duplicate identification conflicts return 409 Conflict with `"Supplier conflict"` ProblemDetails.
  - Security & Authorization: 401 Unauthorized for anonymous callers across all endpoints; 403 Forbidden for non-Admin authenticated users attempting mutations (`POST /`, `PUT /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`).
- Clean Architecture boundaries preserved: API endpoints act as thin HTTP adapters delegating to `SupplierUseCases`; DTOs decouple external consumers from Domain entities and EF models.

### Phase 5.4

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests`): PASS — 189 passed, 0 failed, 0 skipped.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests`): PASS — 214 passed, 0 failed, 0 skipped.
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests`): PASS — 74 passed, 0 failed, 0 skipped (including 12 new supplier persistence integration tests).
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests`): PASS — 80 passed, 0 failed, 0 skipped.
- Full solution test suite: PASS — 557 passed, 0 failed, 0 skipped across all test projects.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.

### Phase 5.3

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests`): PASS — 189 passed, 0 failed, 0 skipped.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests`): PASS — 214 passed, 0 failed, 0 skipped (including 37 new supplier application unit tests).
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests`): PASS — 62 passed, 0 failed, 0 skipped.
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests`): PASS — 80 passed, 0 failed, 0 skipped.
- Full solution test suite: PASS — 545 passed, 0 failed, 0 skipped across all test projects.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.

### Phase 5.2

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests`): PASS — 189 passed, 0 failed, 0 skipped (including 27 new supplier domain unit tests).
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests`): PASS — 177 passed, 0 failed, 0 skipped.
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests`): PASS — 62 passed, 0 failed, 0 skipped.
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests`): PASS — 80 passed, 0 failed, 0 skipped.
- Full solution test suite: PASS — 508 passed, 0 failed, 0 skipped across all test projects.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.

### Phase 5.1

- Documentation verification: PASS — Reviewed documentation changes across `docs/domain-model.md`, `docs/business-rules.md`, `docs/decision-log.md` (Decision 36), and `docs/project-state.md`.
- Consistency review: PASS — Supplier domain model adheres to Clean Architecture aggregate patterns established in Category and Customer domains.
- Invariant alignment: PASS — Rules `BR-SUP-001` through `BR-SUP-006` align with existing project principles (valid data, audit traceability, active-status governance, and deletion restriction for records with history).
- Non-breaking boundary review: PASS — Catalog (`Product`), Inventory (`Inventory`, `InventoryMovement`), and Sales (`Sale`) are decoupled from Supplier in this phase; no production code, tests, or database migrations were modified.
- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx`): PASS — 481 passed, 0 failed, 0 skipped: Domain 162, Application 177, Infrastructure 62, API 80.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.

### Phase 4.6

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests`): PASS — 162 passed, 0 failed, 0 skipped (including 14 new customer-sale domain tests).
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests`): PASS — 177 passed, 0 failed, 0 skipped (including 12 new customer-sale application tests).
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests`): PASS — 62 passed, 0 failed, 0 skipped (including 4 new customer-sale persistence tests).
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests`): PASS — 80 passed, 0 failed, 0 skipped (including 7 new customer-sale API tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx`): PASS — 481 passed, 0 failed, 0 skipped.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Customer-Sales integration verified:
  - Hybrid snapshot pattern: `Sale` aggregate stores optional `CustomerId` and immutable snapshotted `CustomerReference` (`BR-CUST-006`).
  - Catalog modification isolation: Renaming or deactivating a customer in the catalog does not mutate existing sales' `CustomerReference`.
  - Walk-in compatibility: Draft sales can be created and confirmed with `CustomerId = null` without requiring customer registration.
  - Inactive customer rejection: Attempting to create a sale or assign an inactive customer returns 400 Bad Request (`BR-CUST-003`).
  - Nonexistent customer rejection: Attempting to create a sale or assign a nonexistent customer returns 404 Not Found.
  - Immutability on confirmed sales: `PUT /api/sales/{id}/customer` rejects modifying the customer on confirmed sales with 400 Bad Request (`BR-SALE-014`).
  - Database deletion restriction: Foreign key `FK_sales_customers_CustomerId` with `DeleteBehavior.Restrict` prevents deleting customers with associated sales (`BR-CUST-004`).
  - Authorization: `PUT /api/sales/{id}/customer` requires `AdminOnly`, rejecting unauthorized users with 401 and ordinary users with 403.
  - Atomic confirmation preserved: Inventory stock deduction and sale confirmation remain atomic and consistent.

### Phase 4.5

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj`): PASS — 73 passed, 0 failed, 0 skipped (including 19 new customer API integration tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx`): PASS — 444 passed, 0 failed, 0 skipped: Domain 148, Application 165, Infrastructure 58, API 73.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Customer Minimal API endpoints verified against PostgreSQL 17 in Testcontainers:
  - Customer creation (`POST /api/customers`): 201 Created with Location header and `CustomerResponse` payload, verified with both full details and minimal details (Name only).
  - Customer retrieval by ID (`GET /api/customers/{id:guid}`): 200 OK for authenticated callers (tested with `User` role to verify read access), and 404 Not Found for non-existent IDs.
  - Customer listing & search (`GET /api/customers`): 200 OK without filters, filtered by `isActive` (true/false), and searched by `searchTerm` matching across Name, IdentificationNumber, EmailKey, and Phone.
  - Customer updating (`PUT /api/customers/{id:guid}`): 200 OK for valid updates, self-identification preservation without false conflict, 404 Not Found for missing customers, and 409 Conflict for conflicting identification numbers.
  - Lifecycle state transitions: `POST /api/customers/{id:guid}/activate` and `POST /api/customers/{id:guid}/deactivate` successfully activate/deactivate customers and persist status changes (verified in DB), and return 404 Not Found for missing customers.
  - Error handling & validation: DataAnnotations and Domain validation failures return 400 ValidationProblem or 400 BadRequest. Duplicate identification conflicts return 409 Conflict with `"Customer conflict"` ProblemDetails.
  - Security & Authorization: 401 Unauthorized for anonymous callers across all endpoints; 403 Forbidden for non-Admin authenticated users attempting mutations (`POST /`, `PUT /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`).
- Clean Architecture boundaries preserved: API endpoints act as thin HTTP adapters delegating to `CustomerUseCases`; DTOs decouple external consumers from Domain entities and EF models.

### Phase 4.4

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests/SistemaInventarioVentas.Infrastructure.Tests.csproj`): PASS — 58 passed, 0 failed, 0 skipped (including 12 new customer persistence integration tests).
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj`): PASS — 165 passed, 0 failed, 0 skipped (including 2 new duplicate identification exception translation tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 425 passed, 0 failed, 0 skipped: Domain 148, Application 165, Infrastructure 58, API 54.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Customer persistence verified against PostgreSQL 17 in Testcontainers:
  - Table mapping and primary key: `customers` table with non-generated UUID primary key.
  - Case-insensitive identification uniqueness: `IdentificationNumberKey` computed column with `ux_customers_identification_number_key` enforces unique constraint across case variations.
  - Coexistence of null identification numbers: multiple customers without identification numbers persist without conflict.
  - Query filtering: `CustomerQuery` filters by `IsActive` (active, inactive, all) and substring search across `Name`, `IdentificationNumber`, `EmailKey`, and `Phone`.
  - Email value conversion and query compatibility: `EmailAddress` mapped via value converter; `EmailKey` stored computed column allows native PostgreSQL ILike queries without cast failures.
  - Change tracking and detached updates: customers updated correctly whether tracked in EF change tracker or detached.
  - Exception translation: `DbUpdateException` on unique violation translated to `DuplicateCustomerIdentificationException`, mapped to `CustomerIdentificationConflict` by `CustomerUseCases` without leaking provider-specific exceptions.
  - Cancellation token: asynchronous operations respect cancellation tokens.
  - Dependency injection: `ICustomerRepository` resolves to `CustomerRepository` from container.
- Clean Architecture boundaries preserved: Infrastructure implements `ICustomerRepository`; Application and Domain have no dependencies on EF Core, Npgsql, or PostgreSQL.

### Phase 4.3

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj`): PASS — 163 passed, 0 failed, 0 skipped (including 37 new customer application use case tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 411 passed, 0 failed, 0 skipped: Domain 148, Application 163, Infrastructure 46, API 54.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Customer application layer verified:
  - Repository interface `ICustomerRepository` defines asynchronous contracts for ID retrieval, active/search listing via `CustomerQuery`, case-insensitive unique identification number checks with excluded entity IDs, and persistence (`AddAsync`, `SaveAsync`).
  - Use cases coordinate customer lifecycle (`CreateAsync`, `GetByIdAsync`, `ListAsync`, `UpdateDetailsAsync`, `ActivateAsync`, `DeactivateAsync`).
  - Duplicate identification numbers mapped to `ApplicationErrorCode.CustomerIdentificationConflict`; absent/blank identification numbers bypass duplicate checks.
  - Updates allow preserving the current identification number for the same customer without false conflict.
  - Domain validation errors and syntax violations mapped to `ApplicationErrorCode.InvalidInput`.
  - Non-existent entities mapped to `ApplicationErrorCode.NotFound`.
  - Cancellation token properly propagated across all async methods.
- Clean Architecture boundaries preserved: Application depends only on Domain and does not reference EF Core, ASP.NET Core, or outer infrastructure.

### Phase 4.2

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj`): PASS — 148 passed, 0 failed, 0 skipped (including 26 new customer domain tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 374 passed, 0 failed, 0 skipped: Domain 148, Application 126, Infrastructure 46, API 54.
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Customer domain aggregate verified: non-empty name (1–200 characters, trimmed), optional uppercase-normalized identification number (max 50 chars), optional email/phone/address validation and trimming, active default state, activate/deactivate state transitions, and atomic update validation.
- Clean Architecture boundaries preserved: Domain has no dependencies on Application, Infrastructure, EF Core, or outer frameworks.

### Phase 4.1

- Documentation consistency check: PASS — `docs/domain-model.md`, `docs/business-rules.md`, `docs/decision-log.md`, and `docs/project-state.md` are aligned with requirements `FR-CUST-001` through `FR-CUST-005` and Clean Architecture principles.
- Production code unchanged: PASS — no C# source files, project files, migrations, or tests were created or modified.
- Backwards compatibility verification: PASS — `Sale.CustomerReference` is preserved as an immutable snapshot alongside optional `CustomerId`, ensuring existing sales, persistence mappings, and API endpoints remain intact without breaking changes.

### Phase 3.6

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 348 passed, 0 failed, 0 skipped: Domain 122, Application 126, Infrastructure 46, API 54.
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj`): PASS — 54 passed, 0 failed, 0 skipped (including 14 new sales API integration tests).
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Sales endpoints verified: draft sale creation (`POST /api/sales`), get by ID (`GET /api/sales/{id}`), add detail line (`POST /api/sales/{id}/details`), update detail line by detail ID or product identifier (`PUT /api/sales/{id}/details/{detailIdOrProductIdentifier}`), remove detail line (`DELETE /api/sales/{id}/details/{detailIdOrProductIdentifier}`), and atomic confirmation with stock deduction (`POST /api/sales/{id}/confirm`).
- Error handling and Problem Details verified: 400 Bad Request on invalid payloads and detail mutation attempts after confirmation; 404 Not Found on missing sales, products, or detail lines; 409 Conflict on insufficient stock with atomic rollback.
- Security and authorization policies verified: 401 Unauthorized for anonymous requests across all endpoints; 403 Forbidden for non-Admin authenticated users attempting mutations; 200 OK for User role reading sales.
- Clean Architecture boundaries preserved: Endpoints act as thin HTTP adapters delegating to `SaleUseCases`; no persistence context or domain logic duplicated in API layer.

### Phase 3.5

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj`): PASS — 126 passed, 0 failed, 0 skipped (including 13 new sales confirmation use case tests).
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests/SistemaInventarioVentas.Infrastructure.Tests.csproj`): PASS — 46 passed, 0 failed, 0 skipped (including 7 new atomic sales confirmation persistence integration tests).
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Atomic sales confirmation verified: Draft status and line items pre-validated, stock decremented across multiple products, `InventoryMovement` exit records generated with sale reference (`$"Sale {sale.Id}"`), database transaction rolled back on any insufficient stock item, optimistic concurrency conflicts caught and translated to `ConcurrencyConflict`, and duplicate confirmations prevented sequentially and under concurrency.

### Phase 3.4

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 314 passed, 0 failed, 0 skipped: Domain 122, Application 113, Infrastructure 39, API 40.
- Infrastructure PostgreSQL integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests/SistemaInventarioVentas.Infrastructure.Tests.csproj`): PASS — 39 passed, 0 failed, 0 skipped (including 10 new sales persistence integration tests).
- Migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Migration applied against PostgreSQL 17 in Testcontainers: PASS — `20261019170912_AddSales` applied without errors.
- Sales aggregate persistence verified: draft sale creation without details, sale creation with multiple details, monetary precision preservation (subtotal, tax amount, total with 2 decimal places), detail update and totals recalculation, detail deletion and orphan removal, detached entity updates, lifecycle transitions (`Confirmed`, `Cancelled`), database unique constraint on duplicate product per sale, and cancellation token propagation.
- Clean Architecture boundaries preserved: Domain and Application remain independent of EF Core, PostgreSQL, and Npgsql; Infrastructure implements `ISaleRepository`.

### Phase 3.3

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj`): PASS — 113 passed, 0 failed, 0 skipped (including 33 new sales application use case tests).
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj`): PASS — 122 passed, 0 failed, 0 skipped.
- Architecture verification: PASS — Application depends on Domain only and defines `ISaleRepository`; no dependencies on EF Core, PostgreSQL, ASP.NET Core, or outer layers.
- Invariant & rule coverage: PASS — Draft sale creation, detail addition/updating/removal, duplicate line aggregation, price snapshot preservation, customer reference update, catalog existence check, draft-only mutation enforcement, and cancellation token propagation verified.
- Confirmation, stock movements, and persistence deferred: PASS — No inventory modification, transaction coordination, database mapping, or API endpoints implemented.

### Phase 3.2

- Solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj --no-build`): PASS — 122 passed, 0 failed, 0 skipped (including 45 new sales domain tests).
- Invariant & rule coverage: PASS — positive quantities, non-negative unit price snapshot, non-negative tax rate, financial calculations with `MidpointRounding.AwayFromZero`, duplicate product aggregation by increasing quantity, draft-only mutations, valid/invalid lifecycle transitions, and collection immutability verified.
- Clean Architecture boundaries preserved: Domain has no dependencies on Application, Infrastructure, API, EF Core, or external libraries; aggregates reference catalog products by value via `ProductIdentifier`.
- No Application use cases, persistence, or API behavior implemented: PASS.

### Phase 3.1

- Planning and documentation consistency check: PASS — domain model (`docs/domain-model.md`), business rules (`docs/business-rules.md`), decision log (`docs/decision-log.md`), and project state (`docs/project-state.md`) are aligned with requirements and Clean Architecture.
- Production code unchanged: PASS — no C# production source files, tests, migrations, or project configurations were modified.
- Existing inventory and authentication contracts preserved: PASS — no redesign or alteration of existing inventory OCC or security infrastructure.

### Phase 2.5

- Full solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx`): PASS — 226 passed, 0 failed, 0 skipped: Domain 77, Application 80, Infrastructure 29, API 40.
- Concurrency persistence integration tests (`InventoryConcurrencyPersistenceTests`): PASS — 5 tests verifying concurrent over-withdrawal prevention (initial stock 10, two concurrent exits of 7: at most one succeeds, final stock is never negative), concurrent updates without silent data loss, movement history consistency with final stock, rejected operations persisting no phantom movements, and concurrent initializations handling unique identifier conflict gracefully.
- Concurrency API integration tests (`InventoryConcurrencyApiTests`): PASS — 2 tests verifying HTTP 409 Conflict Problem Details responses on concurrent withdrawals and concurrent product stock initializations.
- EF Core migration snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Invariant & rule coverage: PASS — non-negative stock invariant (`Quantity >= 0`) enforced at both domain and database levels, optimistic concurrency conflicts return clean application/API errors without leaking internal database exceptions.
- Sequential baseline tests: PASS — all existing entry, exit, adjustment, not-found, insufficient stock, authentication, and authorization tests continue to pass.

### Phase 2.4

- Full solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj`): PASS — 38 passed, 0 failed, 0 skipped (including 13 new inventory API tests).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx`): PASS — 219 passed, 0 failed, 0 skipped: Domain 77, Application 80, Infrastructure 24, API 38.
- Authentication & Authorization: PASS — anonymous requests to `/api/inventory` endpoints return 401 Unauthorized Problem Details; authenticated `User` role has read access (`GET /{productIdentifier}`, `GET /{productIdentifier}/movements`) and receives 403 Forbidden Problem Details on mutation operations; authenticated `Admin` role can record entries, exits, and adjustments.
- Error mapping & Problem Details: PASS — invalid request payloads map to 400 Bad Request; nonexistent product or inventory queries map to 404 Not Found; domain invariant violations (insufficient stock) map to 409 Conflict with title "Insufficient stock".
- Request/Response contracts: PASS — API DTOs cleanly represent stock queries and movement history with proper serialization and no exposure of internal entity types.
- Clean Architecture boundaries preserved: API endpoints act as thin HTTP adapters invoking `InventoryUseCases`; no DbContext or repository types are accessed in endpoints.

### Phase 2.3

- Full solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Infrastructure integration test suite (`dotnet test tests/SistemaInventarioVentas.Infrastructure.Tests/SistemaInventarioVentas.Infrastructure.Tests.csproj`): PASS — 24 passed, 0 failed, 0 skipped (including 7 new inventory persistence tests).
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj`): PASS — 77 passed, 0 failed, 0 skipped.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj`): PASS — 80 passed, 0 failed, 0 skipped.
- API integration test suite (`dotnet test tests/SistemaInventarioVentas.API.Tests/SistemaInventarioVentas.API.Tests.csproj`): PASS — 25 passed, 0 failed, 0 skipped.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 206 passed, 0 failed, 0 skipped: Domain 77, Application 80, Infrastructure 24, API 25.
- EF model snapshot check (`dotnet ef migrations has-pending-model-changes`): PASS — 0 pending model changes.
- Migration applied against PostgreSQL 17 in Testcontainers: PASS — `20261019013317_AddInventory` applied without errors.
- Inventory aggregate persistence verified: initial receipt, subsequent entries, exits, adjustments, cumulative stock balance reconciliation, detached-state synchronization, database-level unique constraint on `ProductIdentifier`, and cascade delete of movements.
- Clean Architecture boundaries preserved: Domain and Application remain independent of EF Core, PostgreSQL, and Npgsql; Infrastructure implements `IInventoryRepository`.

### Phase 2.2

- Full solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Application unit test suite (`dotnet test tests/SistemaInventarioVentas.Application.Tests/SistemaInventarioVentas.Application.Tests.csproj`): PASS — 80 passed, 0 failed, 0 skipped (including 49 new inventory use case test cases).
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj`): PASS — 77 passed, 0 failed, 0 skipped.
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 199 passed, 0 failed, 0 skipped: Domain 77, Application 80, Infrastructure 17, API 25.
- Architecture verification: PASS — Application depends on Domain only and defines `IInventoryRepository`; no infrastructure, database, or API dependencies.
- Error handling & invariants: PASS — Domain invariant failures map to `ApplicationErrorCode.InsufficientStock`, missing records map to `NotFound`, and malformed/invalid inputs map to `InvalidInput`. Cancellation tokens are propagated to repository operations.

### Phase 2.1

- Full solution build (`dotnet build SistemaInventarioVentas.slnx`): PASS — 0 warnings, 0 errors.
- Domain unit test suite (`dotnet test tests/SistemaInventarioVentas.Domain.Tests/SistemaInventarioVentas.Domain.Tests.csproj`): PASS — 77 passed, 0 failed, 0 skipped (including 37 inventory domain test cases).
- Full solution test suite (`dotnet test SistemaInventarioVentas.slnx --no-build`): PASS — 150 passed, 0 failed, 0 skipped: Domain 77, Application 31, Infrastructure 17, API 25.
- Architecture verification: PASS — Domain has no dependencies on Application, Infrastructure, API, EF Core, or external libraries; inventory references products via `ProductIdentifier` without coupling to the `Product` entity.
- Invariant & rule coverage: PASS — non-negative stock invariant (`Quantity >= 0`), positive entry/exit quantity validation, non-empty movement reference/reason trimming, relative signed-delta adjustments, chronological movement tracking, cumulative balance reconciliation, and collection immutability verified.
- Environment notes: Integration tests (`Infrastructure.Tests` and `API.Tests`) require Docker daemon access for Testcontainers; passed completely when the Docker socket was accessible.

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