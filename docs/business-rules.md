# Business rules

This document defines the business rules that govern the behavior of the inventory and sales management system. These rules describe the required business invariants and validation behavior without prescribing technology, database design, or implementation details.

## Rule catalog

### Categories

- BR-CAT-001 | Business area: Categories | Rule description: A category must have the information required to identify it clearly for operational use. | Priority: High | Type: Validation
- BR-CAT-002 | Business area: Categories | Rule description: Category names or identifiers must be unique enough to avoid ambiguous classification. | Priority: High | Type: Invariant
- BR-CAT-003 | Business area: Categories | Rule description: A category may be active or inactive, but an inactive category must not be treated as a valid classification for new business records unless an authorized exception is granted. | Priority: Medium | Type: State transition
- BR-CAT-004 | Business area: Categories | Rule description: A category cannot be removed while it is still associated with active products, unless an explicit business authorization process approves the change. | Priority: High | Type: Authorization
- BR-CAT-005 | Business area: Categories | Rule description: Product classification must remain coherent: each product must belong to a valid category for operational review and reporting. | Priority: High | Type: Consistency

### Products

- BR-PROD-001 | Business area: Products | Rule description: Each product must have a defined product identifier that uniquely identifies it in the catalog. | Priority: Critical | Type: Invariant
- BR-PROD-002 | Business area: Products | Rule description: Product identifiers must be unique across the active catalog to avoid duplicate product records. | Priority: Critical | Type: Invariant
- BR-PROD-003 | Business area: Products | Rule description: A product must have a valid name and a valid category assignment before it is considered usable in operational workflows. | Priority: High | Type: Validation
- BR-PROD-004 | Business area: Products | Rule description: Product price, cost, and tax-related values must be valid and consistent with the business rules for commercial operations. | Priority: High | Type: Calculation
- BR-PROD-005 | Business area: Products | Rule description: A product may be active or inactive, but inactive products must not be used in new sales or inventory operations unless an explicit business exception is approved. | Priority: High | Type: State transition
- BR-PROD-006 | Business area: Products | Rule description: Product status must remain coherent with category assignment, stock behavior, and sales activity. | Priority: Medium | Type: Consistency

### Suppliers

- BR-SUP-001 | Business area: Suppliers | Rule description: A supplier must have the minimum identifying information required for business contact and traceability. | Priority: High | Type: Validation
- BR-SUP-002 | Business area: Suppliers | Rule description: Supplier records must be unique enough to avoid duplicate business relationships or ambiguous source tracking. | Priority: Medium | Type: Invariant
- BR-SUP-003 | Business area: Suppliers | Rule description: Supplier status must be maintained clearly, with historical records preserved even when a supplier becomes inactive. | Priority: Medium | Type: State transition
- BR-SUP-004 | Business area: Suppliers | Rule description: Supplier information must remain available for historical review when it is associated with past inventory entries or related transactions. | Priority: Medium | Type: Consistency

### Customers

- BR-CUST-001 | Business area: Customers | Rule description: A customer must have the identifying information needed for sales processing and traceability. | Priority: High | Type: Validation
- BR-CUST-002 | Business area: Customers | Rule description: Customer records must be unique enough to avoid duplicate customer identities and conflicting sales history. | Priority: High | Type: Invariant
- BR-CUST-003 | Business area: Customers | Rule description: A customer may be active or inactive, but inactive customers must not be treated as valid recipients for new sales unless a business exception permits the action. | Priority: Medium | Type: State transition
- BR-CUST-004 | Business area: Customers | Rule description: Sales history must remain associated to the correct customer record even when customer status changes. | Priority: High | Type: Consistency

### Inventory

- BR-INV-001 | Business area: Inventory | Rule description: Inventory quantity must represent the current available stock for a product at a given point in time. | Priority: Critical | Type: Invariant
- BR-INV-002 | Business area: Inventory | Rule description: Inventory quantity cannot become negative as a result of accepted stock movements or sales. | Priority: Critical | Type: Invariant
- BR-INV-003 | Business area: Inventory | Rule description: Inventory entries must reference a valid product and a valid positive quantity before they are accepted as stock increases. | Priority: High | Type: Validation
- BR-INV-004 | Business area: Inventory | Rule description: Inventory exits must reference a valid product and a valid quantity available for the business operation before they are accepted. | Priority: Critical | Type: Validation
- BR-INV-005 | Business area: Inventory | Rule description: Inventory adjustments must include a clear reason and must produce an auditable account of the quantity change. | Priority: High | Type: Validation
- BR-INV-006 | Business area: Inventory | Rule description: Every inventory movement must be traceable to a product, a movement type, a quantity, and a relevant business action or transaction. | Priority: Critical | Type: Consistency
- BR-INV-007 | Business area: Inventory | Rule description: Inventory levels must remain consistent with the cumulative effect of valid entries, exits, adjustments, and sales-related reductions. | Priority: Critical | Type: Consistency
- BR-INV-008 | Business area: Inventory | Rule description: Product stock status must remain consistent with product activity and current inventory records. | Priority: Medium | Type: Consistency

### Sales

- BR-SALE-001 | Business area: Sales | Rule description: A sale must reference a valid customer and must include enough information to identify the transaction and responsible user. | Priority: Critical | Type: Validation
- BR-SALE-002 | Business area: Sales | Rule description: A sale must contain at least one valid sale detail line before it can be confirmed. | Priority: Critical | Type: Validation
- BR-SALE-003 | Business area: Sales | Rule description: Sale detail lines must reference a valid product and a positive quantity. | Priority: Critical | Type: Validation
- BR-SALE-004 | Business area: Sales | Rule description: Sale quantity and product selection must be compatible with current inventory availability before a sale is confirmed. | Priority: Critical | Type: Consistency
- BR-SALE-005 | Business area: Sales | Rule description: Sale totals and taxes must be mathematically consistent with the sale details and the applicable tax treatment for the sale. | Priority: Critical | Type: Calculation
- BR-SALE-006 | Business area: Sales | Rule description: A sale can transition through valid business states, but invalid or unauthorized status changes must not alter the business record. | Priority: High | Type: State transition
- BR-SALE-007 | Business area: Sales | Rule description: A confirmed sale must not leave the corresponding product inventory in a negative state. | Priority: Critical | Type: Invariant
- BR-SALE-008 | Business area: Sales | Rule description: A confirmed sale and its corresponding inventory changes must remain consistent across sale, sale detail, inventory, and movement records. | Priority: Critical | Type: Consistency
- BR-SALE-009 | Business area: Sales | Rule description: Cancellation or reversal of a sale must obey the business approval conditions and must not leave the transaction history logically inconsistent. | Priority: High | Type: State transition

### Users and authorization

- BR-USER-001 | Business area: Users | Rule description: A user must have a valid identity and active status before being granted access to protected business actions. | Priority: Critical | Type: Authorization
- BR-USER-002 | Business area: Users | Rule description: User status must be maintained consistently; inactive users must not be allowed to execute operational work unless explicitly reactivated through an approved process. | Priority: High | Type: State transition
- BR-USER-003 | Business area: Users | Rule description: Role assignment and authorization boundaries must restrict access to business functions according to the authorized responsibilities of the user. | Priority: Critical | Type: Authorization
- BR-USER-004 | Business area: Users | Rule description: Sensitive business actions must be associated with the user who performed them for accountability and review. | Priority: High | Type: Consistency
- BR-USER-005 | Business area: Users | Rule description: Users without the required authorization must not create, modify, approve, or cancel protected inventory or sales records. | Priority: Critical | Type: Authorization

### Audit

- BR-AUD-001 | Business area: Audit | Rule description: Key operational events, including creation, update, approval, and status changes, must be traceable to the responsible user and time of action. | Priority: High | Type: Consistency
- BR-AUD-002 | Business area: Audit | Rule description: The system must preserve the history of materially important actions affecting inventory, sales, and user access. | Priority: High | Type: Consistency
- BR-AUD-003 | Business area: Audit | Rule description: Business decisions that affect stock movement, sales confirmation, or user access must be reviewable for accountability. | Priority: High | Type: Consistency

### Reports

- BR-REP-001 | Business area: Reports | Rule description: Reported inventory and sales values must be derived from the same operational history and current validated state that the business uses for operational decisions. | Priority: High | Type: Consistency
- BR-REP-002 | Business area: Reports | Rule description: Reports must only display data that is authorized for the requesting user or role. | Priority: High | Type: Authorization
- BR-REP-003 | Business area: Reports | Rule description: Report content must reflect the system’s trusted business records, including valid movement history and completed sales data. | Priority: Medium | Type: Consistency

## Important business invariants

1. Product identity must be unique according to the defined product identifier.
2. Inventory quantity cannot become negative.
3. A sale must contain at least one valid sale detail line.
4. Sale quantities must be positive and must not exceed the valid available stock for confirmation.
5. Sale totals must be mathematically consistent with their details and the applicable taxes.
6. A confirmed sale must correspond to the required inventory reduction and movement history.
7. Inventory movements must be traceable to a product, quantity, type, and relevant business transaction or approval.
8. Invalid, unauthorized, or failed operations must not alter business state.
9. A confirmed sale and its corresponding inventory changes must remain consistent across sale, sale details, inventory, and inventory movements.
10. Business records must preserve accountability through linked user actions and operational history.

## State transitions

### Product lifecycle

- Product creation: The product enters an active or inactive lifecycle state based on valid registration and business rules.
- Product activation: A previously inactive product can return to active status only when it meets the required business conditions.
- Product deactivation: A product may become inactive when the business decides it is no longer used in active operations, without destroying historical sales and movement records.

### User lifecycle

- User creation: The system creates a user record with identity and status information.
- User activation: A user becomes active when authorized for normal operational access.
- User deactivation: A user becomes inactive when business policy suspends or removes the person from active access.
- Role change: A user’s assigned responsibilities may change without affecting the historical record of prior actions.

### Sale lifecycle

- Draft or in-progress sale: A sale may exist before final confirmation and must not yet impose final inventory impact.
- Confirmed sale: A sale becomes confirmed only when it satisfies the required validation and availability conditions.
- Completed sale: The sale remains part of the historical operational record and must remain consistent with inventory and movement history.
- Cancelled or reversed sale: A sale may be cancelled or reversed only under valid business conditions and with required authorization.

### Inventory movement types

- Inventory entry: Increases stock and must be traceable to the relevant business action.
- Inventory exit: Decreases stock and must be valid against current inventory and authorized business purpose.
- Inventory adjustment: Changes stock by a defined reason and must remain traceable and auditable.
- Sales-related reduction: Reduces stock only when a confirmed sale has satisfied all validation and consistency rules.

## Business rule summary

The business rules in this system revolve around four core principles: valid data, consistent stock, trustworthy sales, and accountable actions. The business must prevent invalid product, customer, or supplier states; prevent inventory from becoming negative; ensure sales are based on valid details, positive quantities, and current stock availability; and preserve records that show who acted, what changed, and why. These rules provide the governing logic before detailed technical design is considered.

## Deferred decisions

The following implementation and architectural decisions are intentionally left for later phases and are not part of this business-rule definition:

- final transaction and concurrency approach for sales and inventory updates
- final database schema and storage strategy
- final API contract design
- final permission matrix for all roles and permissions
- exact tax engine behavior and jurisdiction-specific rules
- final technical mechanism for audit storage and reporting generation
- exact state enumeration structures and validation logic for user, product, and sale lifecycles
