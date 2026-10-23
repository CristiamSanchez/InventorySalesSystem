# Domain model

This document defines the conceptual domain model for the inventory and sales management system. It describes the main business concepts, their responsibilities, and the important domain relationships that govern the business behavior without prescribing database structures, implementation classes, or technology-specific design.

## Core domain concepts

### 1. Category

- Name: Category
- Purpose: Organize products into business classifications for operational management, reporting, and navigation.
- Business meaning: A category groups products by commercial family or line.
- Main responsibilities:
  - identify product families clearly
  - support product classification and catalog organization
  - enable filtering and reporting by product group
- Important attributes/concepts:
  - category identifier
  - category name
  - active/inactive state
  - set of products assigned to the category
- Relationships with other concepts:
  - one category can include many products
  - products rely on category assignment for business context and reporting
- Important business rules:
  - category identifiers or names must avoid ambiguity
  - categories cannot be removed while still used by active products
  - products must belong to a valid category to participate in normal operations

### 2. Product

- Name: Product
- Purpose: Represent the sellable and stock-tracked item of the business.
- Business meaning: A product is the fundamental commercial unit that is purchased, stocked, sold, and tracked.
- Main responsibilities:
  - define the product within the catalog
  - maintain commercial identity and status
  - participate in inventory balance and sales workflows
  - support reporting and operational review
- Important attributes/concepts:
  - product identifier
  - product name
  - category
  - supplier relationship
  - commercial status
  - price and tax context
- Relationships with other concepts:
  - belongs to a category
  - may be associated with one or more suppliers
  - is referenced by inventory records and sale details
  - appears in movement history and reporting
- Important business rules:
  - product identifiers must be unique
  - a product must have valid identity and category information before use in operations
  - inactive products should not be used in new sales or stock operations without an explicit exception
  - product status must remain consistent with inventory and sales activity

### 3. Supplier

- Name: Supplier
- Purpose: Represent the external source of stock and product supply.
- Business meaning: A supplier is the business partner, vendor, or manufacturer that provides products or stock to the company.
- Main responsibilities:
  - maintain supplier identity, company/commercial name, contact person, and optional contact information
  - provide an authoritative supplier identity (`Id`) for incoming inventory operations, supplier consultations, and future purchasing workflows
  - govern supplier active/inactive status to prevent discontinued or suspended vendors from being used in new inventory entries
  - preserve supplier history and source traceability over time without losing past operational context
- Important attributes/concepts:
  - `Id`: unique supplier identifier (`Guid`)
  - `Name`: required commercial or corporate supplier name (`string`, 1–200 characters, trimmed)
  - `ContactName`: optional contact representative / sales agent name (`string?`, max 200 characters, trimmed)
  - `IdentificationNumber`: optional tax ID, fiscal registration code, or commercial identifier (`string?`, max 50 characters, trimmed, uppercase normalized, unique across suppliers when provided)
  - `Email`: optional contact email address (`EmailAddress?`, max 320 characters, lowercase normalized, validated format using domain value object)
  - `Phone`: optional contact telephone number (`string?`, max 30 characters, trimmed)
  - `Address`: optional warehouse or fiscal address (`string?`, max 300 characters, trimmed)
  - `IsActive`: supplier status flag (`bool`, defaults to `true` on creation)
  - `CreatedAtUtc`: creation timestamp (`DateTimeOffset`)
- Domain methods:
  - `Supplier(name, contactName = null, identificationNumber = null, email = null, phone = null, address = null, createdAtUtc = null)`: factory/constructor for new suppliers (defaults `IsActive = true`).
  - `Supplier(id, name, contactName, identificationNumber, email, phone, address, isActive, createdAtUtc)`: complete constructor for reconstituting entity state.
  - `UpdateDetails(string name, string? contactName, string? identificationNumber, EmailAddress? email, string? phone, string? address)`: updates commercial and contact details with full validation.
  - `Activate()`: activates the supplier (`IsActive = true`).
  - `Deactivate()`: deactivates the supplier (`IsActive = false`).
- Relationships with other concepts:
  - one supplier can supply many products over time
  - incoming inventory entries and future purchase receipts reference `Supplier` by value (`SupplierId: Guid?`) while preserving the supplier's name snapshot in the movement/receipt reference for historical immutability
  - products do not require a mandatory supplier link to exist in the catalog; supplier-product sourcing relationships belong to transactional inventory entries and future purchasing workflows
  - supplier deactivation never removes historical inventory movements or product sourcing records
- Important business rules:
  - a supplier must have a valid non-empty corporate/commercial name (BR-SUP-001)
  - identification number, when supplied, must be unique across suppliers (case-insensitively) to prevent duplicate vendor accounts and ambiguous source tracking (BR-SUP-002)
  - contact information (contact name, email, phone, address) is optional to accommodate informal or local suppliers, but must be normalized and validated whenever supplied (BR-SUP-005)
  - inactive suppliers cannot be used for recording new inventory entries or future purchase receipts (BR-SUP-003)
  - supplier history remains permanently available and immutable; suppliers associated with historical inventory entries must never be deleted from the system (BR-SUP-004)

### 4. Customer

- Name: Customer
- Purpose: Represent the commercial counterpart that purchases products from the company.
- Business meaning: A customer is the entity associated with sales transactions, receivables, and historical commercial records.
- Main responsibilities:
  - maintain customer identity, commercial name, and optional contact information
  - provide an authoritative customer identity (`Id`) for sales transactions and sales history querying
  - govern customer active/inactive status to prevent inactive customers from being used in new sales
  - preserve customer history and commercial continuity over time
- Important attributes/concepts:
  - `Id`: unique customer identifier (`Guid`)
  - `Name`: required commercial or legal customer name (`string`, 1–200 characters, trimmed)
  - `IdentificationNumber`: optional tax ID, national ID, or commercial identification code (`string?`, max 50 characters, trimmed, uppercase normalized, unique across customers when provided)
  - `Email`: optional contact email address (`EmailAddress?`, max 320 characters, lowercase normalized, validated format)
  - `Phone`: optional contact telephone number (`string?`, max 30 characters, trimmed)
  - `Address`: optional physical or billing address (`string?`, max 300 characters, trimmed)
  - `IsActive`: customer status flag (`bool`, defaults to `true` on creation)
  - `CreatedAtUtc`: creation timestamp (`DateTimeOffset`)
- Domain methods:
  - `Customer(name, identificationNumber = null, email = null, phone = null, address = null, createdAtUtc = null)`: factory/constructor for new customers (defaults `IsActive = true`).
  - `Customer(id, name, identificationNumber, email, phone, address, isActive, createdAtUtc)`: complete constructor for reconstituting state.
  - `UpdateDetails(string name, string? identificationNumber, EmailAddress? email, string? phone, string? address)`: updates commercial and contact details with full validation.
  - `Activate()`: activates the customer (`IsActive = true`).
  - `Deactivate()`: deactivates the customer (`IsActive = false`).
- Relationships with other concepts:
  - one customer can have many sales over time
  - `Sale` references `Customer` via `CustomerId` (`Guid?`, identifier by value) while maintaining `CustomerReference` (`string`) as an immutable snapshot of the customer name at the time of sale
  - customer deactivation never removes historical sales or movement records
- Important business rules:
  - a customer must have a valid non-empty name (BR-CUST-001)
  - identification number, when supplied, must be unique across customers to prevent duplicate customer records (BR-CUST-002)
  - contact information (email, phone, address) is optional to accommodate counter/walk-in commerce, but must be normalized and validated whenever supplied (BR-CUST-005)
  - inactive customers cannot be used for new sales or new draft sale associations (BR-CUST-003)
  - sales history remains permanently tied to the customer record even if status changes or the customer is renamed (BR-CUST-004, BR-CUST-006)

### 5. Inventory

- Name: Inventory
- Purpose: Represent the current stock position for a product.
- Business meaning: Inventory is the validated current stock available to the business for operations and sales.
- Main responsibilities:
  - maintain current stock quantity for each product
  - reflect the result of valid stock transactions
  - support review of availability and stock condition
  - support validation before sales or stock consumption
- Important attributes/concepts:
  - product reference
  - current quantity available
  - stock condition or exception state
  - relationship to movement history
- Relationships with other concepts:
  - inventory belongs to a product
  - inventory is changed by inventory entries, exits, adjustments, and sales
  - inventory is reconciled through inventory movement records
- Important business rules:
  - inventory quantity cannot become negative
  - every accepted stock change must be traceable to a valid business action
  - current stock must remain consistent with verified movement history

### 6. Inventory Movement

- Name: Inventory Movement
- Purpose: Record the business event behind every change in stock quantity.
- Business meaning: An inventory movement explains why stock changed and which operational action caused it.
- Main responsibilities:
  - capture stock-affecting events over time
  - preserve chronology and accountability of stock history
  - support reconciliation and audit review
- Important attributes/concepts:
  - `Id`: unique movement identifier (`Guid`)
  - `ProductIdentifier`: product reference value object (`ProductIdentifier`)
  - `MovementType`: type of movement (`Entry`, `Exit`, `Adjustment`)
  - `Quantity`: quantity changed (`int`)
  - `Reference`: business reference, delivery note, invoice, or adjustment reason (`string`, max 500 characters)
  - `SupplierId`: optional registered supplier identifier (`Guid?`, identifier by value, applicable to `Entry` movements)
  - `SupplierName`: optional commercial name snapshot of the supplier at entry time (`string?`, 1–200 characters, immutable historical snapshot)
  - `OccurredAtUtc`: occurrence timestamp (`DateTimeOffset`)
- Relationships with other concepts:
  - belongs to `Inventory` aggregate
  - for incoming entry movements, may reference `Supplier` via `SupplierId` while maintaining `SupplierName` as an immutable snapshot
  - for sales-related exit movements, references `Sale` through `Reference` (`$"Sale {sale.Id}"`)
  - helps reconstruct the current inventory state over time
- Important business rules:
  - every movement must be traceable
  - movement type and quantity must match the business action
  - movement history must remain aligned with the current inventory record
  - incoming movements associated with a supplier must reference an active supplier and preserve the supplier commercial name snapshot at delivery time (BR-SUP-003, BR-SUP-006)
  - suppliers associated with past inventory movements cannot be deleted (BR-SUP-004)

### 7. Sale

- Name: Sale
- Purpose: Represent a customer transaction that includes products sold and the commercial result of the transaction.
- Business meaning: A sale is the key transaction of the business through which inventory is consumed and revenue is realized.
- Main responsibilities:
  - define the transaction context and identity
  - identify the customer, date, user, and lifecycle status
  - encapsulate and coordinate the collection of sale detail lines
  - maintain financial integrity: calculate subtotal, tax amount, and total from details
  - coordinate the link between sale confirmation and warehouse stock reduction
  - preserve a valid, immutable historical record of completed sales
- Important attributes/concepts:
  - `Id`: unique sale identifier (`Guid`)
  - `CustomerId`: optional identifier of the registered customer (`Guid?`, identifier by value)
  - `CustomerReference`: customer commercial name or business reference snapshot (`string`, required, 1–200 characters, preserves the exact customer name at the time the sale was created)
  - `UserId`: identifier of the authenticated user responsible for the transaction (`Guid`)
  - `CreatedAtUtc`: timestamp when the sale was created (`DateTimeOffset`)
  - `ConfirmedAtUtc`: nullable timestamp when the sale was confirmed (`DateTimeOffset?`)
  - `Status`: lifecycle state (`SaleStatus`: `Draft = 1`, `Confirmed = 2`, `Cancelled = 3`)
  - `Subtotal`: sum of all line subtotals before tax (`decimal`)
  - `TaxAmount`: sum of all line tax amounts (`decimal`)
  - `Total`: gross sale amount (`Subtotal + TaxAmount`, `decimal`)
  - `Details`: encapsulated collection of line items (`IReadOnlyCollection<SaleDetail>`)
- Relationships with other concepts:
  - acts as the Aggregate Root for `SaleDetail` entities
  - references a registered customer via `CustomerId` (optional) and always preserves `CustomerReference` snapshot
  - references the responsible authenticated user via `UserId`
  - on confirmation, triggers inventory reduction (`Inventory.RegisterExit`) and movement tracking (`InventoryMovement`) across each sold product
  - visible through sales history and audit review
- Important business rules:
  - a sale must reference a valid customer and valid responsible user context (BR-SALE-001, BR-USER-004)
  - a sale must contain at least one valid sale detail line before confirmation (BR-SALE-002)
  - sale totals and taxes must be mathematically consistent with detail lines (BR-SALE-005)
  - confirmed sales must not leave inventory negative (BR-SALE-007, BR-INV-002)
  - sale confirmation and inventory exit movements must be committed in a single atomic transaction (Decision 7, BR-SALE-008)
  - once confirmed or cancelled, the sale and its details become immutable (BR-SALE-006)

### 8. Sale Detail

- Name: Sale Detail
- Purpose: Represent an individual product line within a sale.
- Business meaning: Each sale detail captures one product sold as part of the customer transaction, freezing commercial values at the time of sale.
- Main responsibilities:
  - connect the sale to the specific catalog product sold via `ProductIdentifier`
  - capture quantity and unit price as an immutable snapshot
  - compute line subtotal, tax amount, and total
  - contribute to overall sale totals and inventory consumption
- Important attributes/concepts:
  - `Id`: unique detail line identifier (`Guid`)
  - `SaleId`: reference to the parent sale (`Guid`)
  - `ProductIdentifier`: product business identifier value object (`ProductIdentifier`)
  - `Quantity`: quantity sold (positive integer, `Quantity > 0`)
  - `UnitPrice`: unit price at the time of sale snapshot (`decimal`, `UnitPrice >= 0`)
  - `TaxRate`: applicable tax rate (e.g. `0.16m` for 16% VAT, `0.0m` for exempt; `TaxRate >= 0`)
  - `Subtotal`: line value before tax (`Quantity * UnitPrice`, `decimal`)
  - `TaxAmount`: tax amount for the line (`Subtotal * TaxRate`, rounded to 2 decimal places with `MidpointRounding.AwayFromZero`, `decimal`)
  - `Total`: line total including tax (`Subtotal + TaxAmount`, `decimal`)
- Relationships with other concepts:
  - belongs exclusively to a parent `Sale` aggregate root
  - references a `Product` by `ProductIdentifier` value object
  - maps directly to one `InventoryMovement` of type `Exit` during sale confirmation
- Important business rules:
  - each detail must reference a valid product identifier (BR-SALE-003)
  - quantities must be strictly positive integers (BR-SALE-003)
  - unit price is a point-in-time snapshot, guaranteeing historical sales remain unchanged when catalog prices fluctuate (BR-SALE-010)
  - a sale cannot contain duplicate detail lines for the same product identifier; adding an existing product increments the line quantity (BR-SALE-011)
  - detail subtotal, tax amount, and total must align with financial precision rules (BR-SALE-005, BR-SALE-012)
  - stock availability must be validated for each detail before confirming the sale (BR-SALE-004)

### 9. User

- Name: User
- Purpose: Represent an actor who accesses and operates the system.
- Business meaning: A user is the identity through which actions, responsibilities, and access rights are assigned.
- Main responsibilities:
  - identify the person or actor interacting with the system
  - maintain user status and profile information
  - support authentication and authorization decisions
  - provide accountability for operational actions
- Important attributes/concepts:
  - user identifier
  - user profile information
  - active/inactive status
  - assigned role or responsibilities
- Relationships with other concepts:
  - a user may hold one or more roles
  - users perform sales and inventory operations
  - users are connected to audit and movement records
- Important business rules:
  - active status is required for operational work
  - inactive users must not perform protected actions without approval
  - user actions must be traceable to the responsible user

### 10. Role

- Name: Role
- Purpose: Define the set of responsibilities and authorized actions a user can perform.
- Business meaning: A role groups users by access boundaries and operational authority.
- Main responsibilities:
  - represent the expected business scope of a user
  - support access control for sensitive business functions
  - provide separation of responsibilities across operational domains
- Important attributes/concepts:
  - role identifier
  - role name
  - responsibility scope
  - assigned users
- Relationships with other concepts:
  - many users may hold a role
  - role controls what each user may do in sales, inventory, user, and reporting contexts
- Important business rules:
  - users without required authorization must not modify protected records
  - role assignment must remain consistent with the user’s business responsibility
  - sensitive actions must remain reviewable in relation to the user’s assigned role

### 11. Audit Event

- Name: Audit Event
- Purpose: Record material state changes, lifecycle transitions, and privileged security actions for operational accountability and historical audit.
- Business meaning: An audit event is an immutable business fact proving that a discrete action occurred, who or what performed it, when it happened, and what business record was affected.
- Main responsibilities:
  - preserve immutable evidence of material operational and security actions
  - capture the responsible actor (`UserId` or system indication) and UTC occurrence timestamp
  - link actions to specific domain aggregates and identifiers (`EntityName`, `EntityId`)
  - record structured metadata details for review without exposing secrets
  - guarantee non-repudiation and temporal ordering of actions
- Important attributes/concepts:
  - `Id`: unique audit event identifier (`Guid`)
  - `EventType`: standardized action or lifecycle event name (`string`, required, 1–100 characters; e.g. `SaleCreated`, `SaleConfirmed`, `SaleCancelled`, `InventoryMovementRecorded`, `ProductActivated`, `ProductDeactivated`, `SupplierActivated`, `SupplierDeactivated`, `CustomerActivated`, `CustomerDeactivated`, `UserLoginSucceeded`, `UserLoginFailed`, `UserActivated`, `UserDeactivated`)
  - `EntityName`: name of the domain aggregate or target category (`string`, required, 1–100 characters; e.g. `Sale`, `InventoryMovement`, `Product`, `Supplier`, `Customer`, `User`)
  - `EntityId`: technical or business identifier of the target record (`string?`, optional/nullable, max 100 characters; null for system-wide or non-entity events such as failed login attempts)
  - `UserId`: identifier of the authenticated user who initiated the action (`Guid?`, optional/nullable; null for system-initiated tasks or unauthenticated attempts)
  - `OccurredAtUtc`: point-in-time timestamp when the event occurred in UTC (`DateTimeOffset`)
  - `MetadataJson`: structured operational payload in JSON format (`string?`, max 4000 characters or text; contains event-specific facts such as quantities, totals, reasons, or sanitized client context, strictly excluding credentials)
- Relationships with other concepts:
  - references a `User` by identifier (`UserId`), or represents system-initiated actions when `UserId` is null
  - references business domain aggregates (`Sale`, `InventoryMovement`, `Product`, `Supplier`, `Customer`, `User`) by `EntityName` and `EntityId`
  - created alongside operational state changes in the same atomic database transaction
  - consumed by administrative audit queries and operational reports
- Important business rules:
  - audit records are strictly immutable: once created, they cannot be modified, edited, or deleted (BR-AUD-001, BR-AUD-002)
  - state-modifying business actions must record an audit event within the same atomic transaction as the state change (BR-AUD-001, Decision 39)
  - read operations must not generate audit events (BR-AUD-004, Decision 39)
  - credentials, passwords, password hashes, JWTs, and encryption keys must never be logged in audit events or metadata (BR-AUD-005, NFR-SEC-003, NFR-OBS-003)
  - system-initiated actions must explicitly record system provenance when `UserId` is null (BR-AUD-006)

### 12. Reporting context

- Name: Reporting context
- Purpose: Provide a business view of the trusted operational history without introducing a separate transactional source of truth.
- Business meaning: Reporting is a derived use of the actual business data, not a separate domain that overrides operational reality.
- Main responsibilities:
  - compile current and historical sales, inventory, and audit data
  - present business views for operational review
  - preserve authorization when presenting sensitive data
- Important attributes/concepts:
  - report focus or type
  - filters such as period, customer, or product
  - data source or underlying business records
- Relationships with other concepts:
  - reports are derived from products, users, sales, inventory, and movement history
  - reports should reflect the system’s trusted operational records
- Important business rules:
  - report data must be consistent with the system of record
  - authorized users only may access restricted report data
  - inventory and sales totals in reports must match operational history

## Entities, value objects, and state concepts

### Entity concepts

- Product: an entity because it has a distinct identity and lifecycle that matters to the business.
- Category: an entity because it persists as a catalog grouping with relationships to multiple products.
- Supplier: an entity because it represents an ongoing commercial relationship and historical context.
- Customer: an entity because a customer’s identity and sales history remain important over time.
- Sale: an entity because it is a transaction with lifecycle rules, integrity requirements, and historical continuity.
- Sale Detail: an entity-like line item because it has meaning as a specific part of a sale and its own commercial relationship to product and quantity.
- User: an entity because it has identity, status, authorization, and operational activity over time.
- Role: an entity because it defines access responsibility and is used across the system.
- Audit Event: an entity because it records a discrete action with a user, time, and effect.
- Inventory: an entity-like business concept because it represents the current stock condition of a product and is governed by movement history and business constraints.

### Value object concepts

- Product identifier: a value object because it is an immutable business reference that identifies a product as a single concept.
- Stock quantity: a value object because it represents a measured amount with rules and constraints, not just an arbitrary number.
- Monetary amount: a value object because it should be treated as a validated business value with consistent semantics.
- Movement type: a value-like concept because it should be interpreted as a defined category of stock change.
- Contact information and commercial data may also be represented as value-like bundles when their details are treated as a cohesive business description.

### State concepts

- Product state: numeric or symbolic states such as active/inactive should be treated as finite business states.
- Customer state: active/inactive and other relationship states should be modeled as business states rather than loose strings.
- User state: active/inactive and authorization-related states are business states that influence access and actions.
- Sale status: finite lifecycle states (`Draft = 1`, `Confirmed = 2`, `Cancelled = 3`). `Draft` allows adding/removing items without consuming stock. `Confirmed` locks items, calculates final totals, and atomically decrements inventory via `Exit` movements. `Cancelled` is a terminal void state (reversing inventory if cancelled from `Confirmed`).
- Inventory movement type: finite movement categories (`Entry = 1`, `Exit = 2`, `Adjustment = 3`), where sale confirmation produces `Exit` movements.

### Concepts resolved and needing further analysis

- Inventory aggregate: resolved in Phase 2 as an independent aggregate root with encapsulated movements, optimistic concurrency control via PostgreSQL `xmin`, and engine-level non-negative check constraint.
- Sales and Inventory transaction boundary: resolved as an atomic operation spanning the `Sale`, its `SaleDetail` items, all affected `Inventory` aggregate stock decrements, and their corresponding `InventoryMovement` records.
- Concurrency protection: resolved via existing inventory OCC (`xmin`); concurrent sales competing for limited stock will trigger concurrency conflicts on commit if stock was altered concurrently, rolling back the entire sale transaction and preventing stock from dropping below zero.
- Customer reference and Customer aggregate: planned in Phase 4.1 as an independent aggregate root (`Customer`) with hybrid sale linking (`CustomerId?` identifier reference by value plus immutable `CustomerReference` commercial name snapshot), preserving aggregate boundaries and backwards compatibility without breaking existing sales.
- Product catalog pricing: `Product` currently has no price attribute. `SaleDetail` captures `UnitPrice` as an immutable snapshot. Whether `Product` is later extended with a default list price or prices are always supplied upon sale line creation remains an open business decision.
- Audit Event: resolved in Phase 6.1 as an independent, append-only domain entity (`AuditEvent`) recording discrete operational and security actions across sales, inventory, entity lifecycles, and authentication. Audit records are transactionally committed with the originating state change, strictly immutable, and protected from storing credentials.
- Complex supplier and customer relationships: may need additional business rules if the project expands beyond the current scope.

## Domain relationships

```text
Category
    ↓
Product
    ↓
Inventory
    ↓
Inventory Movement

Supplier ─► Product
Customer ─► Sale
User ─────► Sale
User ─────► Inventory Movement
User ─────► Audit Event
Role ─────► User

Product ─► Sale Detail
Sale ───► Sale Detail
Sale ───► Inventory Movement
Inventory ─► Inventory Movement
```

## Summary

The conceptual domain model centers on products, stock, customer sales, and the users who act within the system. Product and category hierarchy provide the catalog foundation; inventory and inventory movement history provide operational truth; sales and sale details drive the commercial transaction; and users, roles, and audit events preserve accountability and access control. These domain concepts together define the business behavior and invariants that must remain true for the system to operate reliably.
