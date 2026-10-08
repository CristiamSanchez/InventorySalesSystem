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
- Business meaning: A supplier is the business partner that provides products or stock to the company.
- Main responsibilities:
  - maintain supplier identity and business contact data
  - support supply visibility and traceability
  - preserve historical supplier context for stock movements
- Important attributes/concepts:
  - supplier identifier
  - supplier name
  - business contact information
  - active/inactive status
- Relationships with other concepts:
  - one supplier can supply many products
  - supplier records are tied to incoming inventory operations and historical stock movements
- Important business rules:
  - supplier identity must be unambiguous
  - supplier status must be preserved even when the supplier becomes inactive
  - supplier data must remain available for historical review

### 4. Customer

- Name: Customer
- Purpose: Represent the business counterpart that purchases products from the company.
- Business meaning: A customer is a party associated with sales transactions and sales history.
- Main responsibilities:
  - maintain customer identity and contact information
  - support sales processing and customer lookup
  - retain customer history over time
- Important attributes/concepts:
  - customer identifier
  - customer details
  - active/inactive status
  - sales history
- Relationships with other concepts:
  - one customer can have many sales
  - each sale references a customer
  - customer records remain linked to historical records even if customer status changes
- Important business rules:
  - customer identities must avoid duplication and ambiguity
  - inactive customers should not be treated as valid recipients for new sales without a business exception
  - sales history must remain connected to the correct customer record

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
  - product reference
  - movement type
  - quantity
  - date and time
  - associated transaction or reason
  - responsible user
- Relationships with other concepts:
  - belongs to a product
  - may reference a sale, inventory entry, inventory exit, or adjustment
  - helps reconstruct the current inventory state over time
- Important business rules:
  - every movement must be traceable
  - movement type and quantity must match the business action
  - movement history must remain aligned with the current inventory record

### 7. Sale

- Name: Sale
- Purpose: Represent a customer transaction that includes products sold and the commercial result of the transaction.
- Business meaning: A sale is the key transaction of the business through which inventory is consumed and revenue is realized.
- Main responsibilities:
  - define the transaction context
  - identify the customer, date, user, and status
  - coordinate the link between sale details and stock impact
  - preserve a valid historical record of completed sales
- Important attributes/concepts:
  - sale identifier
  - customer reference
  - sale date/time
  - responsible user
  - sale status
  - subtotal, tax, and total values
- Relationships with other concepts:
  - belongs to a customer
  - contains one or more sale detail records
  - may trigger inventory reduction and movement updates
  - is visible through sales history and audit review
- Important business rules:
  - a sale must reference a valid customer and valid user context
  - a sale must contain at least one valid sale detail line before confirmation
  - confirmed sales must not leave inventory negative
  - sale totals must remain consistent with sale detail values and tax logic
  - sale confirmation must align to required inventory reductions and movement records

### 8. Sale Detail

- Name: Sale Detail
- Purpose: Represent an individual product line within a sale.
- Business meaning: Each sale detail captures one product sold as part of the customer transaction.
- Main responsibilities:
  - connect the sale to the product sold
  - represent quantity and commercial value of one product line
  - contribute to tax and total calculations
- Important attributes/concepts:
  - sale reference
  - product reference
  - quantity
  - unit price or commercial value
  - tax treatment contribution
- Relationships with other concepts:
  - belongs to a sale
  - references a product
  - contributes to sale totals and inventory consequences
- Important business rules:
  - each detail must reference a valid product
  - quantities must be positive and valid
  - the detail totals must align with the sale total and tax calculations
  - stock availability must be validated before confirming the sale detail set

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
- Purpose: Record material changes and privileged actions for accountability.
- Business meaning: An audit event is the evidence that a business action occurred, who performed it, and when.
- Main responsibilities:
  - preserve evidence of important actions
  - support operational history and accountability review
  - correlate activity to business objects and users
- Important attributes/concepts:
  - event type
  - affected record
  - responsible user
  - date and time
  - result or summary of action
- Relationships with other concepts:
  - tied to users and business records
  - relevant to sales, inventory, and user access changes
- Important business rules:
  - key business actions must be traceable
  - audit records must align with the actual state change they describe
  - unauthorized events must not be hidden from the accountability trail

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
- Sale status: draft, confirmed, completed, cancelled, or reversed are business states with defined transitions.
- Inventory movement type: entry, exit, adjustment, and sales-related stock change are finite movement states.

### Concepts needing further analysis

- Inventory may later need more analysis to decide whether it is modeled as a separate aggregate, a derived value, or a full tracked entity.
- Audit Event may require further analysis to determine whether it is a generic event record or a constrained set of approved action types.
- Complex supplier and customer relationships may need additional business rules if the project expands beyond the current scope.

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
