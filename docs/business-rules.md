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

- BR-SUP-001 | Business area: Suppliers | Rule description: A supplier must have the identifying information needed for commercial contact and traceability: a non-empty, trimmed corporate or commercial name (1–200 characters). | Priority: Critical | Type: Validation
- BR-SUP-002 | Business area: Suppliers | Rule description: Supplier identification numbers (Tax ID, RFC, CIF, or commercial code), when provided, must be unique across all suppliers (case-insensitively) to prevent duplicate vendor accounts and ambiguous source tracking. Multiple suppliers without an identification number may coexist. | Priority: High | Type: Invariant
- BR-SUP-003 | Business area: Suppliers | Rule description: A supplier may be active or inactive. Inactive suppliers must not be used for recording new inventory entries or new stock receipts unless explicitly reactivated. | Priority: High | Type: State transition
- BR-SUP-004 | Business area: Suppliers | Rule description: Sourcing history must remain permanently associated with the correct supplier record even when supplier status changes or contact details are updated. Supplier records associated with past inventory movements or entries must never be deleted. | Priority: Critical | Type: Consistency
- BR-SUP-005 | Business area: Suppliers | Rule description: Contact information (contact person, email, phone, address) is optional to support diverse supplier relationships; when provided, contact name must be trimmed (max 200 characters), email must be a valid lowercase-normalized address, phone trimmed (max 30 characters), and address trimmed (max 300 characters). | Priority: Medium | Type: Validation
- BR-SUP-006 | Business area: Suppliers | Rule description: Incoming inventory entries and movements associated with a supplier snapshot the supplier's commercial name into their audit reference at creation time, ensuring historical inventory movements accurately reflect the supplier name at the time of delivery even if the supplier is later renamed. | Priority: High | Type: Invariant

### Customers

- BR-CUST-001 | Business area: Customers | Rule description: A customer must have the identifying information needed for sales processing and traceability: a non-empty, trimmed commercial name (1–200 characters). | Priority: Critical | Type: Validation
- BR-CUST-002 | Business area: Customers | Rule description: Customer identification numbers (Tax ID, National ID, or commercial code), when provided, must be unique across all customers (case-insensitively) to prevent duplicate customer identities and ambiguous sales history. | Priority: High | Type: Invariant
- BR-CUST-003 | Business area: Customers | Rule description: A customer may be active or inactive. Inactive customers must not be used as recipients for new sales or new draft sale associations unless explicitly reactivated. | Priority: High | Type: State transition
- BR-CUST-004 | Business area: Customers | Rule description: Sales history must remain permanently associated with the correct customer record even when customer status changes or contact details are updated. Customer records with associated sales must not be deleted. | Priority: Critical | Type: Consistency
- BR-CUST-005 | Business area: Customers | Rule description: Contact information (email, phone, address) is optional to support diverse counter and commercial sales; when provided, email must be a valid lowercase-normalized address, phone trimmed (max 30 characters), and address trimmed (max 300 characters). | Priority: Medium | Type: Validation
- BR-CUST-006 | Business area: Customers | Rule description: A sale snapshots the customer commercial name into its immutable `CustomerReference` at creation time, ensuring historical sales and invoices accurately reflect the customer name at the time of purchase even if the customer is later renamed. | Priority: Critical | Type: Invariant

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
- BR-SALE-010 | Business area: Sales | Rule description: Sale detail unit prices are immutable snapshots captured at the time of sale line creation, unaffected by subsequent catalog price changes. | Priority: Critical | Type: Invariant
- BR-SALE-011 | Business area: Sales | Rule description: A sale cannot contain duplicate detail lines for the same product identifier; adding an existing product increments the line quantity. | Priority: High | Type: Validation
- BR-SALE-012 | Business area: Sales | Rule description: Sale subtotal, tax amount, and total calculations must use standard financial decimal arithmetic and rounding (MidpointRounding.AwayFromZero to two decimal places). | Priority: Critical | Type: Calculation
- BR-SALE-013 | Business area: Sales | Rule description: Sale confirmation requires that the sale, all its detail lines, all corresponding inventory stock decrements, and all inventory movement records be committed in a single atomic transaction. | Priority: Critical | Type: Consistency
- BR-SALE-014 | Business area: Sales | Rule description: A confirmed or cancelled sale is immutable; no detail lines may be added, removed, or modified once a sale leaves the Draft status. | Priority: Critical | Type: State transition

### Users and authorization

- BR-USER-001 | Business area: Users | Rule description: A user must have a valid identity and active status before being granted access to protected business actions. | Priority: Critical | Type: Authorization
- BR-USER-002 | Business area: Users | Rule description: User status must be maintained consistently; inactive users must not be allowed to execute operational work unless explicitly reactivated through an approved process. | Priority: High | Type: State transition
- BR-USER-003 | Business area: Users | Rule description: Role assignment and authorization boundaries must restrict access to business functions according to the authorized responsibilities of the user. | Priority: Critical | Type: Authorization
- BR-USER-004 | Business area: Users | Rule description: Sensitive business actions must be associated with the user who performed them for accountability and review. | Priority: High | Type: Consistency
- BR-USER-005 | Business area: Users | Rule description: Users without the required authorization must not create, modify, approve, or cancel protected inventory or sales records. | Priority: Critical | Type: Authorization

### Audit

- BR-AUD-001 | Business area: Audit | Rule description: Key operational events, including creation, update, approval, status changes, and inventory movements, must be traceable to the responsible user and UTC timestamp. | Priority: High | Type: Consistency
- BR-AUD-002 | Business area: Audit | Rule description: The system must preserve an immutable, append-only history of materially important actions affecting inventory, sales, catalog entities, and user access. | Priority: High | Type: Invariant
- BR-AUD-003 | Business area: Audit | Rule description: Business decisions that affect stock movement, sales confirmation, or entity lifecycles must be reviewable for administrative accountability. | Priority: High | Type: Consistency
- BR-AUD-004 | Business area: Audit | Rule description: Read and query operations (searches, detail views, report generation) must not generate audit events to prevent log bloat and performance degradation. | Priority: Medium | Type: Validation
- BR-AUD-005 | Business area: Audit | Rule description: Audit records and metadata must never capture or log passwords, password hashes, JWT access tokens, encryption keys, or sensitive credentials. | Priority: Critical | Type: Authorization
- BR-AUD-006 | Business area: Audit | Rule description: Once committed, audit event records are strictly immutable and cannot be edited, altered, or deleted. | Priority: Critical | Type: Invariant
- BR-AUD-007 | Business area: Audit | Rule description: Audit records for state-modifying business operations must be committed in the same atomic database transaction as the business state change, preventing phantom or missing audit records. | Priority: Critical | Type: Consistency
- BR-AUD-008 | Business area: Audit | Rule description: Audit records must attribute actions to the validated server-side identity (UserId) when authenticated, or explicitly denote system provenance when unauthenticated or system-initiated. | Priority: High | Type: Consistency

### Reports

- BR-REP-001 | Business area: Reports | Rule description: Reported inventory and sales values must be derived from the same operational history and current validated state that the business uses for operational decisions. | Priority: High | Type: Consistency
- BR-REP-002 | Business area: Reports | Rule description: Reports must only display data that is authorized for the requesting user or role. | Priority: High | Type: Authorization
- BR-REP-003 | Business area: Reports | Rule description: Report content must reflect the system’s trusted business records, including valid movement history and completed sales data. | Priority: Medium | Type: Consistency
- BR-REP-004 | Business area: Reports | Rule description: Financial revenue metrics (subtotals, tax amounts, grand totals) in sales summary reports must strictly aggregate completed and legally recognized sales (Status == Confirmed). Draft sales (pending, not inventory-reducing) and Cancelled sales (voided/reversed) must never be summed into revenue totals; when included in history views, non-confirmed sales must be clearly differentiated and counted separately from realized revenue. | Priority: Critical | Type: Calculation
- BR-REP-005 | Business area: Reports | Rule description: Product sales performance and detail reports must calculate sold units, unit prices, subtotal amounts, and taxes exclusively from the immutable snapshot values recorded on sale detail lines (SaleDetail.UnitPrice, SaleDetail.Subtotal, SaleDetail.TaxAmount, SaleDetail.Total), completely isolated from any subsequent catalog price or tax rate changes on the Product entity. | Priority: Critical | Type: Invariant
- BR-REP-006 | Business area: Reports | Rule description: Current inventory stock reports must reflect physical stock balances from the Inventory operational store. Registered catalog products that do not have an initialized inventory row must be reported with an available stock of zero and an explicit indicator (HasInventoryRecord = false) rather than being omitted from catalog-wide stock reports. | Priority: High | Type: Consistency
- BR-REP-007 | Business area: Reports | Rule description: Reporting operations are strictly read-only and must never alter entity state, acquire persistent transactional locks, or generate audit events (in accordance with BR-AUD-004). All reporting queries must execute with read-only query semantics (EF Core .AsNoTracking()) against the primary operational store. | Priority: High | Type: Consistency

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

### Customer lifecycle

- Customer creation: The customer enters the active lifecycle state by default upon valid registration.
- Customer deactivation: An active customer can be deactivated when commercial activity ceases. Inactive customers cannot be assigned to new sales. Historical sales remain fully linked and accessible.
- Customer activation: A deactivated customer can be reactivated to participate in new sales transactions.

### User lifecycle

- User creation: The system creates a user record with identity and status information.
- User activation: A user becomes active when authorized for normal operational access.
- User deactivation: A user becomes inactive when business policy suspends or removes the person from active access.
- Role change: A user’s assigned responsibilities may change without affecting the historical record of prior actions.

### Sale lifecycle

- Draft: An in-progress customer sale. Items can be added, modified, or removed. Does not consume inventory and does not record inventory movements. Can transition to Confirmed or Cancelled.
- Confirmed: A sale finalized after satisfying all validation and stock availability checks. The sale details, quantities, prices, and totals become immutable. Inventory stock is decremented atomically across all items, recording Exit movements linking to the sale.
- Cancelled: A voided sale. Transitioning from Draft to Cancelled has no inventory effect. Transitioning from Confirmed to Cancelled requires authorized reversal, restoring stock via Entry movements linked to the cancellation. Cancelled is a terminal state.

### Inventory movement types

- Inventory entry: Increases stock and must be traceable to the relevant business action. When originating from a vendor delivery, it may reference a registered supplier; must validate that the supplier is active and snapshot the supplier's commercial name at the time of delivery (BR-SUP-003, BR-SUP-006). Manual or internal stock entries without a supplier remain fully supported.
- Inventory exit: Decreases stock and must be valid against current inventory and authorized business purpose.
- Inventory adjustment: Changes stock by a defined reason and must remain traceable and auditable.
- Sales-related reduction: Reduces stock only when a confirmed sale has satisfied all validation and consistency rules.

### Audit event lifecycle

- Event emission: An audit event is generated synchronously upon execution of state-modifying operational workflows (sale creation/confirmation/cancellation, inventory movement recording, entity activation/deactivation) or authentication attempts.
- Transactional coupling: For operational state changes, the audit event is persisted within the same database transaction as the entity mutation. If the transaction rolls back, the audit record is also rolled back, guaranteeing zero phantom audit events (BR-AUD-007).
- Terminal immutability: Once written and committed to persistence, an audit event is strictly append-only and cannot be updated, modified, or deleted (BR-AUD-006).

## Business rule summary

The business rules in this system revolve around four core principles: valid data, consistent stock, trustworthy sales, and accountable actions. The business must prevent invalid product, customer, or supplier states; prevent inventory from becoming negative; ensure sales are based on valid details, positive quantities, and current stock availability; and preserve records that show who acted, what changed, and why. These rules provide the governing logic before detailed technical design is considered.

## Deferred decisions

The following implementation and architectural decisions are intentionally left for later phases and are not part of this business-rule definition:

- final transaction and concurrency approach for sales and inventory updates
- final database schema and storage strategy
- final API contract design
- final permission matrix for all roles and permissions
- exact tax engine behavior and jurisdiction-specific rules
- final technical mechanism for reporting generation and audit retention pruning policies (audit event model and transactional storage are resolved in Phase 6.1)
- exact state enumeration structures and validation logic for user, product, and sale lifecycles
