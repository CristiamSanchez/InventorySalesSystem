# Architecture

## 1. Architectural style

This project follows a Clean Architecture style. The core business meaning lives at the center of the system and is protected from technical delivery details. The structure is intentionally domain-first so the inventory and sales rules remain stable as the project evolves.

The design is grounded in the existing domain model and business rules. The domain defines what the business is, the application defines how the business operates in use cases, infrastructure provides the technical support, and the API exposes the system through transport-specific concerns.

At this phase, the project approves a backend technical direction within that architecture: C#, .NET 10, ASP.NET Core, Minimal APIs, and Clean Architecture. These are implementation decisions that support the documented business design; they do not change the domain model, the business rules, or the dependency boundaries already defined.

The project remains consistent with the architecture already documented: the backend runtime and HTTP framework are chosen to serve the domain-first structure, not to replace it.

## 1.1 Approved backend technology direction

The backend direction for this project is:

1. C# and .NET 10 as the backend platform.
   - C# provides a strong, typed, mature language for business-focused backend work.
   - .NET 10 is the selected runtime and SDK choice for the backend implementation platform.
   - This fits the project because the system is business-heavy, validation-heavy, and needs clear separation between domain rules and delivery concerns.

2. ASP.NET Core as the web framework.
   - ASP.NET Core provides a modern hosting model, dependency injection, HTTP pipeline support, and a mature framework for web-facing enterprise backend applications.
   - It fits this project because the system needs a web boundary but must keep that boundary outside the business rules and domain logic.

3. Minimal APIs as the HTTP API approach.
   - Minimal APIs are appropriate for a focused backend whose core concern is business coordination and transport adaptation rather than large controller-heavy patterns.
   - They fit this project because the API layer should remain thin, explicit, and focused on request/response adaptation rather than owning business logic.
   - Minimal APIs do not alter the business architecture; they only define the HTTP delivery surface.

4. Clean Architecture as the architectural structure.
   - The project is organized around the Domain, Application, Infrastructure, and API layers defined in this document.
   - This fits the project because the operational rules for stock, sales, auditability, and inventory consistency are critical business invariants that must not be driven by HTTP or persistence details.

5. Expected project/layer structure.
   - Domain: encapsulates the business model, rules, invariants, and domain concepts such as product, inventory, sale, sale detail, customer, user, and movement history.
   - Application: coordinates use cases, validates workflow sequencing, and orchestrates the business operations required by the system.
   - Infrastructure: implements persistence, technical adapters, and integration details behind the contracts required by the application and domain.
   - API: handles HTTP concerns, request adaptation, response shaping, and transport-level validation without owning business rules.

6. Dependency direction between these projects.
   - The dependency flow remains inward: the outer layers depend on the business rules and contracts defined by the inner layers.
   - In practice, the API depends on the application layer and the abstractions it requires; the application depends on domain contracts; infrastructure implements those contracts and adapts to external concerns.
   - The domain must not depend on API transport models, HTTP concerns, database frameworks, or specific persistence implementation details.

7. General conventions for maintainability and testability.
   - Keep the Domain free from HTTP, persistence, and UI concerns.
   - Keep the Application focused on orchestration and validation of use cases.
   - Keep Infrastructure behind interfaces or contracts defined by the inner layers.
   - Keep the API thin and transport-specific.
   - Test business invariants in the domain and application without requiring a web server or database to validate the core rules.
   - Preserve the documented business boundaries so a future implementation can evolve without distorting the inventory and sales rules.

8. Backend decisions that remain deferred until later phases.
   - Database schema design and detailed query and repository implementation remain deferred; PostgreSQL and Entity Framework Core are the selected database and persistence framework in section 1.2.
   - Concrete transaction manager, isolation level, and concurrency-control mechanism remain deferred.
   - Detailed API contracts, authentication implementation details beyond the direction in section 1.4, and UI integration remain deferred.
   - The selected backend and persistence technologies do not lock in those remaining implementation details.

These backend decisions are subordinate to the documented architecture and must remain consistent with the business rules captured in the domain model, business rules, and architecture definitions.

## 1.2 Approved persistence technology direction

The persistence direction for this project is:

1. PostgreSQL as the database engine.
   - PostgreSQL is the selected relational database for the system.
   - It fits the project because the domain model and business rules involve structured operational data, auditability, transactional integrity, and relationships among products, categories, inventory, sales, sale details, users, and movements.
   - PostgreSQL is appropriate for a business system that needs stable relational modeling, transactional guarantees, and clear support for consistency-critical operations without choosing a technology-first architecture.

2. Entity Framework Core as the persistence framework.
   - Entity Framework Core is the selected ORM and persistence framework for the backend implementation.
   - It fits the project because it is a mature .NET framework that can map domain-aware persistence structures to relational data while keeping infrastructure concerns separate from the business model.
   - EF Core is a technical implementation choice that supports the project’s need for persistence without changing the business meaning or architectural boundaries.

3. Infrastructure owns EF Core implementation details.
   - All persistence implementation details, database access logic, entity mapping, and database-specific concerns belong to the Infrastructure layer.
   - Infrastructure is responsible for EF Core configuration, data access adapters, and the concrete implementation behind application-facing contracts.
   - Infrastructure does not redefine the project’s domain rules; it provides the technical mechanism that stores and retrieves them.

4. Domain remains independent from EF Core and PostgreSQL.
   - The Domain layer must remain free from EF Core types, database-specific classes, and PostgreSQL-specific assumptions.
   - Domain concepts, rules, and invariants continue to be expressed in business terms and remain testable without database implementation details.
   - This preserves the independence of the business rules described in the domain model and business rules documents.

5. EF Core entity configuration must be separated from Domain classes when appropriate.
   - The project may use separate persistence configuration for relational mapping, table rules, indexes, constraints, and ownership relationships.
   - This separation is appropriate when the Domain model should remain focused on business meaning while Infrastructure owns the mapping and persistence metadata.
   - Domain classes should not become dependent on EF Core configuration concerns merely to support storage.

6. Migration ownership and responsibility.
   - Migrations are owned by Infrastructure because they belong to the persistence implementation and the chosen relational model.
   - Migration creation, maintenance, and review remain the responsibility of the persistence layer and its implementation team, not the Domain or API layers.
   - Migrations must be evaluated against the business rules and consistency principles, but they are still a technical concern rather than a business-policy source.

7. Application interacts with persistence through abstractions/contracts.
   - The Application layer should depend on contracts that describe the behavior the use case needs, such as loading required state, persisting transactional results, or recording movement history.
   - Infrastructure provides the concrete implementation behind those contracts.
   - This keeps the application focused on orchestration and business flow, while persistence remains external and replaceable.

8. Conceptual transaction boundary for the Sale workflow.
   - The business transaction boundary remains the sale confirmation workflow as an atomic business operation.
   - This includes the consistent outcome across the sale record, sale detail records, inventory consequences, and movement history.
   - The persistence mechanism may support this boundary technically, but the business rule is owned by the domain and application workflow, not by the database alone.

9. Persistence-level responsibility for preserving business consistency.
   - Infrastructure is responsible for implementing the storage mechanisms that preserve the required consistency rules.
   - This includes durable persistence of the relevant domain state and the supporting relational records needed to represent stock, sales, and movement history.
   - Infrastructure must honor the expected consistency semantics, but it must not replace the core business rules that define validity and invariants.

10. Concurrency remains a business-consistency concern.
   - Inventory updates are concurrency-sensitive because multiple operations may attempt to consume or adjust the same stock at the same time.
   - The business design requires that inventory never become negative and that inventory state remain consistent with the cumulative effect of valid operations.
   - Concurrency control is therefore a business-consistency concern in the implementation planning stage, even though specific locking or isolation choices remain deferred.

11. Database constraints may reinforce invariants but must not replace Domain rules.
   - Database constraints, foreign keys, unique indexes, and check constraints may reinforce the model and reduce invalid data entry.
   - However, they are not a substitute for the domain’s business rules.
   - The domain remains the source of truth for business validity, while persistence constraints are a technical safeguard that supports the same intent.

12. Implementation details that remain deferred.
   - Specific database schema design remains deferred.
   - Specific isolation levels and transaction locking strategies remain deferred.
   - Concrete repository design and persistence contracts remain deferred to later implementation planning.
   - Detailed migration planning, indexing strategy, and data seeding strategy remain deferred.
   - Any database-specific optimization or performance tuning remains deferred until the implementation phase.

These persistence decisions remain subordinate to the architecture and must not distort the business model, the domain rules, or the Clean Architecture boundaries already approved for this project.

## 1.3 Approved frontend technology direction

The frontend direction for this project is:

1. Angular as the frontend framework.
   - Angular is the selected frontend framework for the user-facing application.
   - It fits this project because the system spans multiple operational domains such as categories, products, inventory, sales, movement history, users, and reports, all of which benefit from a structured, reusable UI architecture.
   - Angular supports a maintainable frontend structure with reusable components, service-based API integration, and feature-oriented organization without encroaching on the business model defined by the Domain.

2. TypeScript as the frontend language.
   - TypeScript is the selected language for the frontend implementation.
   - It fits this project because the UI must interact with business data such as product, stock, customer, sale, and audit information, and strong typing reduces ambiguity when consuming API contracts and representing UI state.
   - TypeScript is also a natural fit for a larger feature set where the frontend must remain understandable and maintainable across multiple feature areas.

3. HTTP communication with the ASP.NET Core Minimal API.
   - The frontend communicates with the backend through HTTP requests to the ASP.NET Core Minimal API.
   - This maintains a clear separation between the presentation layer and the backend execution model while preserving the agreed contract between the client and the service boundary.
   - The frontend is responsible for calling the API; it is not responsible for implementing the server-side business rules or persistence logic.

4. Frontend responsibilities and boundaries.
   - UI/components: render screens, gather user input, present state, and orchestrate user interaction at the interface level.
   - Frontend services: encapsulate HTTP calls, request shaping, response mapping, and backend integration logic.
   - Models/types: represent API payloads, form data, and UI state for feature-specific screens and flows.
   - Authentication state: track whether the user is authenticated, what claims or roles are available, and whether the current session is allowed to access specific features.
   - API communication: provide the client-facing translation between frontend state and backend endpoints without becoming a second implementation of the Domain.
   - Error handling: translate transport errors, validation errors, and unavailable backend state into appropriate user feedback and recovery options.

5. Maintainable frontend structure suited to the feature areas.
   - The frontend should be organized around feature areas such as users, categories, products, inventory, sales, and reports, rather than around technical implementation detail alone.
   - Shared modules should contain reusable UI components, common forms, utility logic, and cross-cutting concerns.
   - Core infrastructure should hold cross-cutting concerns such as authentication flow, HTTP configuration, environment settings, and shared types.
   - This structure supports the project’s feature map without mixing UI concerns with domain business rules.

6. Authentication and authorization integration.
   - Authentication and authorization are concerns of the overall system, but the frontend is responsible only for representing authentication state and providing session-related user experience, not for security enforcement.
   - The frontend may route or present features according to the current authentication state, while the API remains the source of authentication validation and authorization enforcement.
   - The backend is responsible for validating the user’s authority for every protected operation; frontend route or feature checks are usability aids only.

7. Frontend independence from backend implementation details.
   - The frontend must remain independent from backend implementation details except for the agreed API contract.
   - It should not rely on EF Core, SQL schema details, ASP.NET Core internal types, or other persistence-specific assumptions.
   - The frontend should depend on stable, explicit API contracts and business-facing data contracts rather than on backend class structures or infrastructure choices.

8. Frontend decisions that remain deferred.
   - The exact Angular version and project configuration remain deferred.
   - Routing strategy, state management approach, and component library choices remain deferred.
   - Detailed API contract design remains deferred.
   - Token handling in the frontend, including its storage mechanism, and detailed refresh behavior remain deferred; the selected API authentication direction is documented in section 1.4.
   - Form validation strategy, client-side caching, and error-logging conventions remain deferred.
   - Component and feature layout beyond the high-level structure remain deferred.

These frontend decisions remain subordinate to the architecture and project requirements. They support the documented business model and feature map without creating business logic in the client layer or blurring the boundary between the Domain and the UI.

## 1.4 Authentication and security direction

The following direction establishes system security responsibilities. Phase 1.9 implements the initial API authentication path while leaving broader authorization and session lifecycle work deferred.

1. Authentication and authorization have distinct responsibilities.
   - Authentication establishes and validates the identity of the actor making a request.
   - Authorization determines whether that authenticated identity may perform a requested action on a resource, considering the assigned role and applicable business rules.
   - A valid identity does not, by itself, grant permission to every operation.

2. The backend is the authoritative security boundary.
   - The API must validate authentication and enforce authorization for every protected endpoint and operation, regardless of checks performed by the frontend.
   - Application workflows must not rely on a client-supplied identity, role, or permission as proof of authority.
   - Security decisions that affect business data must remain effective when requests bypass the frontend and call the API directly.

3. The API uses signed JWT bearer access tokens.
   - Infrastructure issues HS256-signed access tokens with a 15-minute lifetime. The API validates the signature, required issuer and audience, signed-token requirement, subject identifier, and expiration (with a 30-second clock skew).
   - Tokens contain the user identifier in `sub`, a unique token identifier in `jti`, and one `role` claim per current role. Passwords, password hashes, and other sensitive profile data are excluded.
   - Issuer, audience, and signing key are required application configuration. The signing key is externally supplied and must contain at least 32 UTF-8 bytes; there is no insecure development fallback.
   - Infrastructure implements the Application token-issuer boundary. ASP.NET Core bearer authentication validates tokens before the API current-user adapter reads identity claims.

4. Passwords must be stored using secure password hashing.
   - Store only a one-way, adaptive password hash using a unique salt and an algorithm designed for password storage; never store plaintext passwords or reversibly encrypted passwords.
   - Passwords and password hashes must not be exposed in responses, logs, audit events, or ordinary application diagnostics.
   - Infrastructure uses ASP.NET Core Identity's versioned PasswordHasher implementation (Identity V3, PBKDF2-HMAC-SHA512 with a random per-password salt). Work-factor configuration/upgrades and credential lifecycle workflows remain implementation/operational concerns; custom cryptography is not permitted.

5. Access tokens are short-lived; refresh remains deferred.
   - Access tokens expire after 15 minutes by default, with the supported configuration bounded to 1–60 minutes.
   - Refresh-token issuance, rotation, revocation, logout semantics, and handling of role or account changes remain deferred. Until then, a user must authenticate again after token expiry.

6. Authorization follows role-based access control.
   - Roles represent groups of responsibilities; authorization must apply least privilege and deny access when the required permission is not established.
   - Role membership may inform access but does not replace operation-specific business checks, such as valid sale transitions or inventory rules.
   - The backend enforces role and permission rules. The final role catalog, permission matrix, and role-change propagation policy remain deferred.

7. Authenticated identity is made available to Application use cases through an application boundary.
   - Application defines `ICurrentUser` with the authenticated state, stable `UserId`, and role names; the API implements it from the validated request principal through `HttpCurrentUser`.
   - Application and Domain code must not depend on HTTP request objects or framework-specific principal types.
   - The user identifier used for accountability must come from the validated server-side identity, not from an untrusted request field.

8. The frontend represents authentication state but is not the security authority.
   - The Angular client may maintain and display whether a session appears authenticated, and may use available role information to guide navigation and feature visibility.
   - Client-side guards and state are not authorization controls; the API must independently validate and authorize every protected request.
   - The client must handle expired or rejected authentication and authorization responses without treating locally held state as proof of access.
   - Token storage and other browser-specific defenses remain deferred.

9. Unauthorized and forbidden operations have distinct conceptual outcomes.
   - Requests to `/auth/me` or Category/Product endpoints without a valid, unexpired token receive HTTP 401. Invalid credentials and inactive accounts receive the same generic HTTP 401 Problem Details response.
   - An authenticated request that lacks permission is an authorization failure (conceptually HTTP 403).
   - Responses should be consistent and avoid revealing sensitive account, resource, or policy details. The initial catalog policy allows authenticated User and Admin roles to read categories/products and requires Admin for catalog creation, update, or removal. Registration and login remain public. No policies for future business areas are defined here.

10. Security principles apply across the system.
    - Passwords: minimize handling and retention; use password-specific hashing; do not log or disclose credentials.
    - Tokens: validate before use, limit exposure and lifetime, protect signing secrets, and support secure refresh and revocation.
    - Sensitive data: disclose only what the caller is authorized to access; protect it in transit and at rest according to the eventual deployment and data classification.
    - Authorization: use least privilege, deny by default, and enforce access on the backend for each protected operation and relevant data scope.
    - Auditability: associate sensitive business actions with the validated user identity and action time; preserve reviewable history without placing passwords, raw tokens, or other secrets in audit records.

11. Implementation decisions remain intentionally deferred.
    - Signing-key provisioning, secure storage, rotation, and deployment-specific key lifecycle.
    - Refresh-token lifecycle, revocation/logout semantics, and role/account-change propagation.
    - Credential recovery, multi-factor authentication, login rate limiting, lockout policy, and other abuse controls.
    - Browser token storage and related CSRF, XSS, and cross-origin protections.
    - Fine-grained role/permission matrix, role-management operations, policies for future business endpoints, and authorization data scope.
    - Registration activation and initial Admin provisioning workflows; new registrations remain inactive until activated through a future operational or administrative process.
    - Password-hash rehash-on-login, audit retention and privacy policy, and deployment-specific transport/data protection settings.

## 1.5 Testing responsibilities by layer

Testing should follow the system's architectural boundaries so business rules are verified independently from technical adapters, while integrations are tested at the boundaries they exercise.

- Domain: unit tests verify entities, value objects, invariants, and business rules without infrastructure or transport dependencies.
- Application: unit tests verify application services and collaborators; use-case tests verify complete application workflows, including sequencing, validation, and expected outcomes through controlled dependency substitutes.
- Infrastructure: integration tests verify persistence and other technical adapters against the real technologies or services they implement contracts for.
- API: API and integration tests verify HTTP endpoint behavior, request/response mapping, authentication and authorization boundaries, and integration with the Application layer.
- Frontend: component and integration tests verify rendering, user interaction, frontend state, and communication behavior across collaborating client components and services.

Tests should be placed and scoped according to the behavior under test. A test at an outer boundary complements rather than replaces focused tests of inner-layer business rules. The .NET test projects use xUnit and are separated by backend layer; coverage targets, test-environment tooling, and additional integration-test support packages remain deferred until concrete tests require them.

## 1.6 Containerization, CI/CD, and deployment direction

This section defines the development and delivery direction without creating container definitions, CI workflows, or deployment resources.

1. Docker is the standard containerization approach.
   - Use Docker to provide reproducible local service dependencies and to package application components for consistent validation and deployment.
   - Containerization supports, but does not replace, the Domain, Application, Infrastructure, and API boundaries.

2. PostgreSQL runs in Docker for local development.
   - Developers should be able to run the selected PostgreSQL database in a Docker container for local development.
   - Local database data and credentials are environment-specific; local persistence and initialization details will be defined when container configuration is implemented.
   - This local-development decision does not require PostgreSQL to run in Docker in deployment environments.

3. Backend containerization.
   - Package the ASP.NET Core API as a Docker image suitable for consistent validation and deployment.
   - The image should contain the API runtime and application, while environment-specific configuration and secrets are supplied externally at runtime.
   - The backend image must not contain local developer credentials, production secrets, or environment-specific database contents.

4. Frontend containerization.
   - At a high level, build the Angular application and package its static frontend output in a Docker image for serving to users.
   - The frontend remains a client of the API; packaging does not move backend business or security responsibilities into the frontend.
   - Specific serving software, image composition, and runtime configuration mechanism remain deferred.

5. Configuration is separated by environment.
   - Development, test/validation, and deployment environments must use appropriately separated configuration.
   - Keep environment-specific values outside application images where practical, and inject runtime configuration through the eventual environment’s configuration mechanism.
   - Non-sensitive defaults and examples may be documented as placeholders; actual credentials and sensitive values must remain outside source control.

6. GitHub Actions is the planned CI/CD platform.
   - GitHub Actions is the planned platform for automated validation when workflows are introduced.
   - The minimum CI quality gates are dependency restore, build, and automated tests for the applicable backend and frontend components.
   - The checks should run for proposed changes before merge and for changes to the mainline branch.

7. CI quality gates must prevent broken changes from being merged.
   - Pull-request checks must report success only when restore, build, and tests pass for the components in scope.
   - Configure the repository to require these checks before merging; failed, missing, or incomplete required checks must block merge.
   - Human review and any additional branch policies complement these automated checks; CI does not replace review.

8. Local development and deployment have different responsibilities.
   - Local development prioritizes reproducible dependencies and convenient feedback; Docker-hosted PostgreSQL is a local service dependency, and developers may run the API and frontend using their normal development workflow.
   - CI validates source changes in a controlled environment and provides passing build/test results.
   - Deployment runs packaged application components with environment-specific configuration, secrets, data services, and operational controls. Local convenience settings and data must not be carried into deployment.
   - This phase defines direction only: it does not provision environments, publish images, deploy the system, or choose a hosting provider.

9. Source-control exclusions for sensitive and generated material.
   - Never commit passwords, API keys, tokens, private keys, signing material, certificates containing private keys, or production credentials.
   - Do not commit populated environment-specific configuration files, local secret files (including actual `.env` files), local database contents or dumps, or credentials embedded in container configuration.
   - Do not commit environment-specific deployment overrides that expose sensitive values. Keep only safe templates with placeholders when examples are useful.
   - Use approved local secret mechanisms and deployment/CI secret stores when those are selected; never echo secrets into logs or build output.
   - Ignore generated build outputs, local container state, and other machine-specific artifacts where appropriate.

10. Deployment decisions remain deferred.
    - Hosting provider, deployment topology, environment inventory, and production runtime sizing.
    - Container registry, image tagging/signing/scanning, release versioning, and artifact retention.
    - Deployment automation, promotion/approval process, rollout and rollback strategy, and availability requirements.
    - Production database hosting, backup/restore, migration execution, and operational ownership.
    - Exact configuration and secret-management services, access controls, and rotation procedures.
    - Container runtime hardening details, networking, ingress/TLS, observability, and scaling configuration.
    - Concrete Dockerfiles, local orchestration files, and GitHub Actions workflow definitions.

## 2. Dependency direction

Dependencies point inward:

- Domain is the innermost layer
- Application is outside the domain but still business-oriented
- Infrastructure is outside the application and adapts to technical concerns
- API is the outer boundary where HTTP concerns are handled

The general rule is:

- inner layers define the rules and contracts
- outer layers implement or adapt the technical details
- code in the inner layers must not depend on frameworks, infrastructure implementations, or transport mechanisms

This keeps business rules independent from persistence, UI, and HTTP concerns.

## 3. Responsibilities by layer

### Domain

The Domain layer contains the business model and governing rules for the system. It includes the core concepts identified in the domain model: product, category, supplier, customer, inventory, inventory movement, sale, sale detail, user, role, audit event, and reporting context.

The Domain is responsible for:

- defining business entities and their identity
- expressing invariants and business rules
- preserving the meaning of stock, sales, customer relationships, and accountability
- enforcing rules that cannot be violated without breaking the business model

The Domain should not depend on:

- HTTP controllers or request models
- database libraries or ORMs
- specific persistence implementations
- delivery frameworks or UI libraries

### Application

The Application layer coordinates the use cases of the business. It is where business workflows are assembled, validated, and executed in the correct order.

The Application is responsible for:

- use-case orchestration
- coordinating domain behavior and required validations
- enforcing transaction boundaries relevant to the operation
- preparing requests and interpreting the result of domain operations

This layer should remain focused on business orchestration and not become a container for infrastructure code or transport logic.

### Infrastructure

The Infrastructure layer implements the technical systems required by the application and domain. It adapts the system to external implementations such as persistence, integrations, file systems, or platform services.

The Infrastructure is responsible for:

- repositories and data access adapters
- persistence implementation details
- technical support for movement history, audit records, and transactional workflows
- concrete implementations behind interfaces defined by the inner layers

Infrastructure should implement contracts defined elsewhere; it should not redefine the business rules.

### API

The API layer is the outer boundary that exposes the system over HTTP or other transport interfaces.

The API is responsible for:

- accepting incoming requests
- mapping transport payloads into application operations
- returning responses and status codes
- enforcing request-level validation and transport-specific authorization flow
- translating technical errors into appropriate HTTP-facing responses

The API layer should not own the business rules. It should translate external interaction into calls to the application layer without embedding domain logic.

## 4. Rules for keeping the Domain independent from frameworks and infrastructure

The Domain must remain independent from infrastructure and technical frameworks. The following rules guide that boundary:

1. Domain logic must be expressible without a web framework, ORM, or storage implementation.
2. Domain concepts must not depend on HTTP request/response types, database contexts, or concrete persistence classes.
3. Infrastructure must implement abstractions or adapters required by the application and domain.
4. The Domain defines the vocabulary of the business; infrastructure adapts to that vocabulary.
5. Domain behaviors must be testable without database or web server execution.
6. The core business model must remain stable even if the implementation technology changes.

This does not mean creating excessive abstraction. The project should add interfaces only when there is a real dependency boundary or external variability. The goal is to avoid coupling business policy to delivery mechanisms without introducing unnecessary indirection.

## 5. Where business rules, use-case orchestration, persistence, and HTTP concerns belong

### Business rules

Business rules belong to the Domain layer.

Examples from the project include:

- inventory quantity cannot become negative
- a sale cannot be confirmed without valid customer and sale detail information
- stock changes must be traceable through movement history
- user actions must be attributable and auditable
- category and product status rules must remain coherent with business operations

These are core business invariants and should be implemented where the business meaning lives.

### Use-case orchestration

Use-case orchestration belongs to the Application layer.

This includes:

- coordinating product registration and validation
- validating stock availability before a sale is confirmed
- orchestrating inventory updates after stock entry, exit, or adjustment
- assembling the sequence of domain operations needed for a business workflow

The application layer defines how the business process runs, while the domain layer defines what is valid.

### Persistence

Persistence concerns belong to Infrastructure.

This includes:

- repositories
- transaction handling implementation
- storage adapters
- mapping between domain concepts and persistence representation

Persistence is a technical concern and not the source of business meaning.

### HTTP concerns

HTTP concerns belong to the API layer.

This includes:

- request validation at the boundary
- response shaping
- status codes and contract mapping
- authentication and authorization flow as request-level concerns
- transport-specific exception handling

The API should adapt the external contract to the application and domain without containing the actual rules of the business.

## 6. SOLID and Dependency Inversion guidance

The architecture should follow SOLID principles, especially Dependency Inversion, without introducing unnecessary abstractions.

### Single Responsibility

Each layer has a clear and limited responsibility:

- Domain owns business meaning and invariants
- Application owns process and orchestration
- Infrastructure owns technical implementation
- API owns the delivery boundary

### Open/Closed

The domain and application should be open to extension without requiring the business rules to be redefined for each new feature. Infrastructure can change without forcing the domain to absorb technology-specific concerns.

### Liskov Substitution

Implementations behind the contracts used by the application should honor the same behavior promised by those contracts. This is especially relevant for repositories, adapters, and services used by the application layer.

### Interface Segregation

Interfaces should remain specific to the behavior they need, not broad or generic just for convenience. This project should keep its abstractions focused on real boundaries, not on every small interaction.

### Dependency Inversion

Dependency Inversion is central to this architecture:

- business-level policy should not depend on concrete technical mechanisms
- the application should depend on abstractions that describe required behavior
- infrastructure should implement those abstractions without controlling the business logic

This keeps inventory and sales invariants durable even if persistence or delivery technology changes later.

### Avoiding unnecessary abstractions

The project should not create abstraction layers for every concept. Good guidance is:

- introduce abstractions only at genuine boundaries
- keep contracts close to the relevant business capability
- avoid generic wrappers when a concrete implementation is straightforward and stable
- prefer simple, meaningful boundaries over a large abstraction framework

## 7. Practical SOLID and Dependency Inversion guidelines for this project

These principles should be applied in a practical, business-focused way. The goal is not to build a large abstraction machine; the goal is to protect the business process from technical variation while keeping the code understandable and maintainable.

### 7.1 Single Responsibility Principle

Each component or module should have one primary reason to change.

In this project, that means:

- the Domain owns the rules that define valid business behavior
- the Application owns the sequence of actions needed to execute a use case
- Infrastructure owns the details required to persist or integrate with external systems
- the API owns the transport contract and request/response boundary

This avoids putting product validation, stock rules, HTTP logic, and persistence logic into the same place.

### 7.2 Open/Closed Principle

The architecture should be open for extension and closed for unnecessary modification.

For this project, the important aspect is that new inventory or sales behaviors should be added without rewriting the central business invariants. For example:

- adding a new sales workflow or a new report should not require changing the core inventory rules
- adding a new persistence adapter should not force a rewrite of the business model
- expanding validation or business state handling should happen in the layer where the rule belongs, not by redefining the rule in the API layer

The business invariants should remain stable even when surrounding behavior evolves.

### 7.3 Liskov Substitution Principle

Any implementation used behind a contract must satisfy the behavior expected by the caller.

For example:

- if the Application depends on a persistence contract, the infrastructure implementation must fulfill the required behavior consistently
- if a domain service or rule validator is replaced, it must preserve the same business guarantees
- a concrete implementation must not quietly violate the invariants promised by the abstraction it fulfills

This principle matters for inventory and sales operations because a weak substitute implementation could compromise stock consistency or auditability.

### 7.4 Interface Segregation Principle

The architecture should favor narrow, intent-based contracts instead of broad, general-purpose interfaces.

This project should use interfaces only when they represent a real dependency boundary or external variability. Examples of justified contracts include:

- a persistence contract required by an inventory workflow
- a notification or integration contract required by a business action
- a security or authorization port used by the application boundary

Interfaces are not justified merely because a concept is "important" or "future-proof". If a class or module is simple and stable, a direct implementation is often better than a broad abstraction.

### 7.5 Dependency Inversion Principle

The most important policy for this project is that higher-level business logic should depend on abstractions, not on concrete infrastructure.

The Application layer may require behavior such as:

- loading a product or customer record
- saving inventory movement history
- checking current stock availability
- persisting a sale or transaction result

These behaviors should be expressed as contracts used by the Application, not as implementation details that the Application imports directly from Infrastructure.

In practical terms:

- the Application declares what it needs
- the Infrastructure implements the concrete mechanism behind that need
- the Domain remains focused on the business meaning and rules

This ensures that inventory validation, sale confirmation, and auditability remain governed by business logic rather than by storage implementation details.

### 7.6 When an interface is justified

An interface is justified when all of the following are true:

1. the dependency is external to the layer that needs it
2. the dependency is likely to vary or be replaced over time
3. the consumer needs a stable abstraction to preserve its own logic
4. the behavior is meaningful as a contract, not just a convenience wrapper
5. the abstraction reduces coupling without creating speculative complexity

Examples in this project may include contracts for persistence, stock lookup, or authentication-related behavior at the application boundary. These are real seams, not arbitrary placeholders.

### 7.7 When an interface is unnecessary

An interface is unnecessary when:

- the behavior is trivial, stable, and local to a single implementation
- the abstraction adds no real decoupling or flexibility
- the code is already simple and does not require a dependency boundary
- the abstraction would exist only because the team expects future changes that are not yet needed
- the abstraction would create a generic service or repository that does not reflect a real business capability

For this project, unnecessary abstractions should be avoided in the Domain and Application unless they solve a genuine boundary problem.

### 7.8 Which layer should own abstractions and contracts

The ownership of abstractions should follow the layer that is actually responsible for the behavior:

- Domain owns business-domain contracts only when the abstraction describes a business capability or rule that belongs to the domain itself
- Application owns use-case contracts for behaviors the application needs to orchestrate, such as loading state, saving results, validating prerequisites, or interacting with external systems at the application boundary
- Infrastructure owns the concrete implementations behind those contracts
- API owns transport or request contracts for HTTP concerns and not business-domain contracts

The main rule is: abstractions should be defined close to the consumer that needs them, while concrete implementations belong to the infrastructure layer that can provide them.

### 7.9 How Application can depend on abstractions without depending on Infrastructure

The Application must depend on contracts, not concrete infrastructure classes.

This is achieved by:

- declaring the required operations in an application-level contract or port
- keeping the contract focused on the behavior the application needs
- allowing the infrastructure implementation to be provided later through composition or assembly
- never importing concrete infrastructure code into the application workflow

This keeps the flow of control aligned with the architecture: Application logic remains testable, business-oriented, and independent from storage or framework implementation details.

### 7.10 How Infrastructure implements Application contracts

Infrastructure is responsible for implementing the contracts required by the Application.

Examples of this pattern include:

- a persistence adapter for product or stock data access
- an adapter that records inventory movements and sales history
- an implementation of a security contract used by the application boundary
- a concrete integration mechanism behind an external service port

The important rule is that Infrastructure does not define or rewrite the business rules. It only supplies the mechanism that satisfies the contract used by the application.

### 7.11 How Domain remains independent

The Domain must stay independent by avoiding any dependency on:

- storage frameworks or database logic
- concrete persistence implementations
- request or response models
- web or UI frameworks
- delivery and infrastructure-specific contracts unless they are explicitly modeled as domain abstractions and only when the behavior is genuinely part of the business model

Domain logic should be expressible in its own terms: product validity, category integrity, stock invariants, sale confirmation conditions, and user accountability. Those rules should not be increased or diluted by infrastructure concerns.

### 7.12 Rules to avoid speculative abstractions and premature patterns

The project should follow strict guardrails:

1. Do not introduce interfaces for every class or method.
2. Do not create repositories, services, or generic wrappers before there is a real need.
3. Do not abstract a dependency that is already simple and stable.
4. Do not build generic patterns for future features that are not in the current scope.
5. Do not let the architecture drift into framework-first or persistence-first design.
6. Do not make the Domain depend on presentation or data-access concerns.
7. Do not impose broad abstractions for small, local behaviors.
8. Prefer the simplest design that preserves the business invariants and keeps layers decoupled.

A good architecture for this project is intentionally strict about boundaries, but deliberately modest about abstraction. The design should support business integrity without creating unnecessary complexity.

## 8. Business logic and use-case boundaries

This project should keep a strict separation between business logic and operational orchestration. The boundary should be defined by the business meaning, not by implementation convenience.

### 8.1 Domain responsibilities

The Domain layer owns the business reality and the rules that make the system valid.

It includes:

- business invariants such as non-negative inventory and valid sale confirmation
- entity behavior such as product or sale lifecycle rules
- value object rules such as valid stock quantities, monetary amounts, and identifiers
- state transitions such as product activation/deactivation, user status changes, and sale status changes
- domain validation that ensures the business object is valid before it becomes part of a workflow

Examples from this project:

- a product cannot be considered valid if it lacks a valid identifier or category
- inventory cannot be negative after confirmation of a transaction
- a sale cannot be confirmed without valid customer and sale detail conditions
- a stock movement must be traceable to a real business action

The Domain should decide what is valid and what is not. It should not decide whether a request arrived over HTTP or whether a value is stored in a database.

### 8.2 Application responsibilities

The Application layer owns the use case workflow and coordination among domain rules and external contracts.

It is responsible for:

- use-case orchestration and sequencing
- workflow coordination between domain objects and technical contracts
- application-level validation that prepares or guards business operations
- coordination of domain actions and external persistence requirements
- authorization decisions when those decisions are part of the application workflow and not the business rule itself

The Application layer decides how a process runs, but it should not redefine the business meaning. It arranges the correct order of steps and ensures the required contracts are satisfied.

### 8.3 Infrastructure responsibilities

Infrastructure owns technical implementation required by the application and domain.

It is responsible for:

- persistence and retrieval of business records
- adapters for external systems or integrations
- concrete implementations of application contracts
- transaction support and technical execution of storage operations

The Infrastructure layer is not allowed to invent business policy. It only implements the mechanism required by a real application or domain need.

### 8.4 API responsibilities

The API layer owns the transport boundary.

It is responsible for:

- HTTP request handling
- request/response mapping
- authentication and authorization integration at the boundary
- translating exceptions and business failures into HTTP responses
- serializing domain or application results for external clients

The API layer should not decide whether a sale is valid or whether inventory can be negative. It should translate the incoming request into an application command and return a response appropriate to the transport contract.

## 9. Architectural boundary for the Sale use case

The Sale use case is a good example of a workflow that spans layers while preserving the business boundary.

### 9.1 Sale validation

Sale validation belongs primarily to the Domain and the Application.

The Domain owns the underlying rules that determine whether a sale is valid, such as:

- sale must reference a valid customer
- sale must have valid status transitions
- sale must contain valid detail lines
- the totals and values must be consistent with business rules

The Application can perform additional workflow validation to ensure the required information and invariants are present before the use case proceeds.

### 9.2 Inventory validation

Inventory validation belongs to the Domain and the Application together, but the core rule remains in the Domain.

The Domain owns the invariant that inventory cannot become negative and that stock availability must be checked against valid business actions.

The Application coordinates the validation required for the sale workflow, including:

- confirming that the selected product is valid
- checking stock availability against the requested quantity
- determining whether the sale request can proceed under current inventory state

The Application may use infrastructure contracts to query current stock, but it does not replace the underlying stock rule with a technical check.

### 9.3 Total calculation

Total calculation belongs to the business logic and should not be hidden in the API or persistence layer.

The Domain is the correct place for the rules that define:

- how line totals are computed
- how tax effects are applied
- how subtotal, tax, and total are derived
- how aggregation rules remain consistent across the sale

The Application may coordinate the flow of these calculations but should not make them arbitrary or implementation-driven.

### 9.4 Inventory deduction

Inventory deduction is not a technical operation alone; it is a business action with a rule behind it.

The Domain owns the rule that a confirmed sale causes inventory reduction only when the resulting state remains valid.

The Application orchestrates the sequence in which this deduction occurs within the sale workflow and ensures that all required records and validations have been satisfied before the deduction is applied.

### 9.5 Inventory movement creation

Inventory movement creation belongs to the business workflow and should be treated as part of the sale transaction record.

The Domain owns the requirement that every stock change must be traceable and align with the actual business action.

The Application coordinates the movement creation as part of the confirmed sale operation, ensuring that the sale, sale detail, inventory, and movement history remain consistent.

### 9.6 Transaction coordination

The transaction coordination belongs to the Application layer, even though the rules behind it are domain-driven.

The Application orchestrates the overall sale flow so that the system does not partially commit business state. The coordinated workflow should ensure that:

- sale validation succeeds before any stock deduction
- inventory availability is validated before confirmation
- total and tax calculations are consistent
- inventory deduction and movement creation are performed as part of one coherent operation
- failure at any point prevents partial business state changes

In other words, the Application is responsible for the execution sequence and the atomicity boundary, while the Domain is responsible for the rules that determine whether the sequence is valid.

## 10. Rules for what stays in Domain vs. what belongs to Application

### Must remain in Domain

These rules belong in the Domain because they define the invariant meaning of the business:

- inventory cannot become negative
- a sale must be associated with valid customer and product conditions
- a sale must have valid detail lines before confirmation
- stock changes must be traceable and auditable
- product and customer states must remain coherent with business usage
- sale totals and taxes must be mathematically consistent with the sale details

### Belongs to Application

These decisions belong to the Application because they coordinate behavior across domain objects and infrastructure:

- sequence of operations for sale confirmation
- coordination between sale validation and inventory checks
- retrieval of current stock state through infrastructure contracts
- persistence and transaction boundaries
- orchestration of movement creation after a valid business action
- validation and guard logic that prepares a use case for execution

### Shared responsibility boundary

Some rules require both layers:

- validation may start in the Domain, but the Application can enforce additional operational checks before a use case runs
- inventory rules are domain invariants, but the Application decides how and when current stock information is gathered and applied in the workflow
- sale status transitions may be domain-defined, while the Application determines whether the workflow is allowed to proceed under the current auth and context

## 11. Failure handling and partial state prevention

Failures must not leave the business in a half-updated state. This is a core architectural rule for inventory and sales operations.

The system must ensure:

- a failing validation prevents any stock deduction
- a failed inventory check prevents sale confirmation
- a movement cannot be recorded without a valid corresponding business action
- a transaction cannot be partially committed across sale, inventory, and movement records
- any failure in the workflow is treated as an all-or-nothing business outcome

This means the Application is responsible for coordinating a coherent operation and for treating the use case as an atomic business unit. The Domain provides the invariants that ensure the business state remains valid, and Infrastructure supports the technical implementation required to preserve transaction integrity.

## 12. Transactions and business consistency

The sale workflow is a business operation that must be treated as a single atomic unit. The business meaning is not simply “save a sale record”; it is “confirm a valid sale and preserve a consistent state across related records.”

### 12.1 Atomicity of the Sale workflow

The business operation that must succeed or fail as a whole is the confirmed sale use case. This operation spans the following business facts:

- sale creation or confirmation
- sale detail validation and persistence
- inventory availability validation
- inventory deduction
- creation of inventory movement records
- preservation of customer, product, and audit consistency

The system must not leave the business in a partially confirmed state in which the sale is recorded but the stock is not reduced, or the stock is reduced without a valid movement record.

### 12.2 Required consistency across related records

The architecture requires consistent business state among these records:

- Sale
- Sale Details
- Inventory
- Inventory Movements

A valid sale must remain consistent with:

- the customer and product validity rules
- the valid quantity and total rules
- the stock state before and after the operation
- the movement history that explains the stock change

If any required fact is invalid, the business operation must be rejected before the state change is committed.

### 12.3 What happens when any step fails

When any step in the sale workflow fails, the business behavior must be treated as a failed transaction. The outcome must be:

- no partial inventory deduction
- no partial movement creation
- no partially confirmed sale state
- no inconsistent stock history

The business should behave as if the entire sale operation never happened. In semantic terms, the business operation is atomic: either the full set of required effects is accepted, or none of them is.

### 12.4 Boundary of the business operation

The boundary of the atomic business operation is the application-level sale confirmation workflow, not a single database row or storage primitive. The business boundary is defined by the requirement that the full sale and inventory outcome remain valid together.

This means the Application layer is responsible for coordinating the full sequence, while the Domain defines the rules that make the sequence valid.

### 12.5 Role of the Application layer

The Application layer owns the transaction boundary at the business level.

It is responsible for:

- sequencing validations and domain operations in the correct order
- confirming that the sale can proceed under current business rules
- coordinating inventory availability checks
- invoking the domain rules that determine whether total and stock changes are valid
- ensuring all required records are prepared before the operation is accepted
- rejecting or aborting the workflow if any required step fails

The Application is the layer that understands the business workflow as a whole. It does not own the technical storage implementation, but it does own the operational boundary that makes the business process atomic.

### 12.6 Role of Infrastructure

Infrastructure provides the technical mechanism necessary to realize the atomic business operation.

Its role is to:

- supply the persistence and transaction mechanism needed to uphold the business operation
- coordinate the execution of the required writes as a single technical unit
- support rollback or cancellation behavior when the business workflow fails
- adapt the business operation to the chosen storage technology without redefining business meaning

Infrastructure does not decide whether the sale is valid or whether inventory may become negative. It only supports the technical execution of a business decision already made by the domain and application layers.

### 12.7 Concurrency and simultaneous inventory changes

The project must assume that multiple operations may attempt to change the same inventory at the same time. Business-level behavior must therefore treat inventory as a shared business resource that can be invalidated by concurrent updates.

The expected business behavior is:

- if stock availability changes between validation and execution, the sale operation must not proceed with stale inventory assumptions
- a concurrent update must not allow a sale to consume stock that is no longer available
- the result should be a rejected or retried business operation rather than a partial inventory update
- the system must preserve the invariant that inventory never becomes negative even under concurrent access

This is a business consistency requirement, not a database implementation decision. The exact technical mechanism used to detect or prevent invalid concurrent updates is intentionally deferred.

### 12.8 Non-negotiable invariant

The most important invariant is: inventory must never become negative.

This rule must remain true across:

- normal sales
- return or cancellation workflows
- stock adjustments
- movement history reconciliation
- concurrent or repeated operations

If a workflow would violate this invariant, it must fail before any business state change is finalized.

### 12.9 Business transaction boundaries versus database implementation details

The transaction strategy must be documented at the business level before a storage technology is selected.

Business-level decisions:

- what counts as one business operation
- which records must remain consistent together
- when a business workflow is atomic
- what failure means for the business state
- which invariant must never be violated

Database or ORM implementation details remain separate and intentionally deferred, including:

- specific transaction APIs
- isolation levels
- locking strategy
- concurrency control technique
- framework-specific change tracking or unit-of-work behavior

The architecture should describe the required business outcome without assuming any specific storage technology.

## 13. Consistency with the existing domain model and business rules

These decisions are consistent with the existing domain model and business rules:

- the domain model defines inventory, sales, customers, products, and movement records as related business concepts with strong consistency requirements
- the business rules explicitly require non-negative inventory, valid sale confirmation, and traceable stock changes
- the architecture keeps those invariants in the Domain while the Application coordinates the atomic workflow
- the API and Infrastructure remain external to the business decision itself and do not determine whether a sale is valid

This is consistent with the project’s goal of preserving business integrity while keeping the technical execution of the workflow separate from the meaning of the business event.

## 14. Deferred decisions

The architecture and business-boundary phases intentionally did not decide the following. Later technical-stack decisions have since selected C#/.NET 10, ASP.NET Core Minimal APIs, PostgreSQL, and Entity Framework Core; the remaining implementation decisions are:

- database schema
- transaction API choices
- isolation levels or locking strategy
- exact concurrency-control implementation
- specific queue, event, or integration pattern for asynchronous processing

These details remain deferred and must preserve the approved business transaction model and architecture.
