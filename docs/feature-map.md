# Feature map

This document organizes the approved requirements into a high-level feature structure for the inventory and sales management system. The grouping is intentionally business-oriented and keeps delivery concerns separate from operational domains.

## Feature dependency overview

```text
Foundation
    ↓
Users
    ↓
Authentication
    ↓
Authorization
    ↓
Categories
    ↓
Products
    ↓
Suppliers
    ↓
Customers
    ↓
Inventory
    ↓
Inventory Movements
    ↓
Sales
    ↓
Audit
    ↓
Reports

Users ───────► Authentication
Users ───────► Authorization
Categories ─► Products ─► Inventory ─► Sales
Suppliers ─► Products
Customers ─► Sales
Inventory ─► Inventory Movements
Sales ─────► Inventory Movements
Sales ─────► Audit
Inventory ─► Audit
Users ─────► Audit
Reports ───► Sales, Inventory, Audit, Users
Frontend ───► All business features
Deployment / CI/CD ─► All features
```

## 1. Foundation

- Feature name: Foundation
- Purpose: Establish the core operational and governance context for the system so all business domains can operate consistently.
- Main capabilities:
  - define the business scope and operational context of the system
  - provide a common understanding of inventory, sales, users, and reporting workflows
  - establish the baseline rules for accountabilities, integrity, and business controls
- Related requirement identifiers: FR-CAT-001 to FR-REP-005; NFR-SEC-001 to NFR-CICD-003
- Main business concepts involved: business operations, product lifecycle, stock visibility, transaction accountability, system governance
- Dependencies on other features: This feature underpins all other features and provides the common reference for change management and operational consistency.
- Classification: foundational

## 2. Categories

- Feature name: Categories
- Purpose: Manage product grouping and classification so the business can organize catalog data consistently.
- Main capabilities:
  - register and maintain product categories
  - prevent duplicate or ambiguous classification
  - consult category structure and associated products
  - control category lifecycle when products remain attached
- Related requirement identifiers: FR-CAT-001, FR-CAT-002, FR-CAT-003, FR-CAT-004, FR-CAT-005
- Main business concepts involved: category, product classification, catalog organization, active/inactive status
- Dependencies on other features: depends on Foundation; supports Products and Reports
- Classification: core business

## 3. Products

- Feature name: Products
- Purpose: Maintain the product catalog and current commercial identity needed to operate inventory and sales activities.
- Main capabilities:
  - register products with unique identifiers
  - update product information and commercial status
  - consult quantity and general product details
  - search, filter, and review product records
  - support product activation and deactivation rules
- Related requirement identifiers: FR-PROD-001, FR-PROD-002, FR-PROD-003, FR-PROD-004, FR-PROD-005, FR-PROD-006
- Main business concepts involved: product, product identifier, commercial status, category relationship, supplier relationship, stock visibility
- Dependencies on other features: depends on Foundation and Categories; supports Inventory, Sales, and Reports
- Classification: core business

## 4. Suppliers

- Feature name: Suppliers
- Purpose: Manage the source relationships required to maintain stock continuity and procurement visibility.
- Main capabilities:
  - register and maintain supplier information
  - review supplier relationships and associated products
  - keep supplier records current without losing historical context
  - control supplier status as the business relationship evolves
- Related requirement identifiers: FR-SUP-001, FR-SUP-002, FR-SUP-003, FR-SUP-004, FR-SUP-005
- Main business concepts involved: supplier, supply relationship, product sourcing, supplier status, historical continuity
- Dependencies on other features: depends on Foundation; supports Products and Inventory entries
- Classification: core business

## 5. Customers

- Feature name: Customers
- Purpose: Maintain the customer base required for sales processing and relationship history.
- Main capabilities:
  - register and update customer records
  - search and review customer detail and sales history
  - manage customer status through active and inactive lifecycle states
  - avoid duplicate records that would confuse sales processing
- Related requirement identifiers: FR-CUST-001, FR-CUST-002, FR-CUST-003, FR-CUST-004, FR-CUST-005
- Main business concepts involved: customer, customer relationship, sales account, status tracking, customer history
- Dependencies on other features: depends on Foundation; supports Sales and Reports
- Classification: core business

## 6. Inventory

- Feature name: Inventory
- Purpose: Maintain the current and accurate stock picture needed for operations and sales control.
- Main capabilities:
  - consult current stock by product, category, or time-relevant context
  - review stock status and exception conditions such as low stock or stock-out
  - receive and validate stock-affecting transactions
  - prevent confirmed sales that would create negative inventory
  - maintain a consistent state for stock after entries, exits, and adjustments
- Related requirement identifiers: FR-INV-001, FR-INV-002, FR-INV-003, FR-INV-004, FR-INV-005; FR-INV-ENT-001 to FR-INV-ENT-005; FR-INV-EXT-001 to FR-INV-EXT-005; FR-INV-ADJ-001 to FR-INV-ADJ-005
- Main business concepts involved: stock level, inventory entry, inventory exit, inventory adjustment, availability, stock validation, product movement
- Dependencies on other features: depends on Products, Suppliers, and Foundation; supports Inventory Movements, Sales, Reports, and Audit
- Classification: core business

## 7. Inventory Movements

- Feature name: Inventory Movements
- Purpose: Preserve the transactional history that explains how stock changed over time and why.
- Main capabilities:
  - record movement history for entries, exits, adjustments, and sales-related stock changes
  - associate each movement with product, quantity, date, type, and related business event
  - support historical review and reconciliation of inventory changes
  - provide traceability for operational accountability and audit review
- Related requirement identifiers: FR-INV-MOV-001, FR-INV-MOV-002, FR-INV-MOV-003, FR-INV-MOV-004, FR-INV-MOV-005; FR-SALE-003, FR-SALE-004, FR-SALE-005
- Main business concepts involved: inventory movement, stock history, reconciliation, transaction traceability, audit trail
- Dependencies on other features: depends on Inventory and Sales; supports Audit and Reports
- Classification: core business

## 8. Sales

- Feature name: Sales
- Purpose: Capture and manage customer transactions while preserving financial and stock consistency.
- Main capabilities:
  - create and confirm sales with required customer, transaction, and user information
  - manage sale detail lines for multiple products in a single transaction
  - calculate and review tax and totals for each sale
  - validate stock availability before confirming a sale
  - maintain consistency across sale, sale details, inventory, and movements
  - review current and historical sales records
- Related requirement identifiers: FR-SALE-001 to FR-SALE-008; FR-SALE-DET-001 to FR-SALE-DET-005; FR-TAX-001 to FR-TAX-005
- Main business concepts involved: sale, sale detail, tax, subtotal, total, customer transaction, stock reservation, status workflow
- Dependencies on other features: depends on Customers, Products, Inventory, Users, and Foundation; supports Inventory Movements, Audit, Reports, and Sales history review
- Classification: core business

## 9. Users

- Feature name: Users
- Purpose: Manage user identity and administrative lifecycle for business and operational use of the system.
- Main capabilities:
  - register and maintain user profile information
  - manage user status and administrative state
  - assign and update role or permission scope
  - prevent duplicate or ambiguous access records
- Related requirement identifiers: FR-USR-001, FR-USR-002, FR-USR-003, FR-USR-004, FR-USR-005
- Main business concepts involved: user, user profile, status, responsibility, administrative assignment
- Dependencies on other features: depends on Foundation; supports Authentication, Authorization, Audit, and all business operations
- Classification: supporting

## 10. Authentication

- Feature name: Authentication
- Purpose: Verify user identity before granting access to protected business functionality.
- Main capabilities:
  - require authentication for protected actions and data
  - validate supplied credentials and reject invalid attempts
  - support sign-out or equivalent secure session termination
  - protect sensitive operational data and workflows
- Related requirement identifiers: FR-AUTH-001, FR-AUTH-002, FR-AUTH-003, FR-AUTH-004, FR-AUTH-005; NFR-SEC-001, NFR-SEC-004
- Main business concepts involved: login, user identity, session, secure access, protected resources
- Dependencies on other features: depends on Users and Foundation; enables access to all protected business features
- Classification: supporting

## 11. Authorization

- Feature name: Authorization
- Purpose: Ensure only permitted users can view, modify, or approve sensitive business data and workflows.
- Main capabilities:
  - define or maintain role-based access constraints
  - restrict privileged actions by role or permission
  - prevent unauthorized modifications to inventory, sales, and administrative records
  - support operational accountability for access to restricted content
- Related requirement identifiers: FR-ROLE-001, FR-ROLE-002, FR-ROLE-003, FR-ROLE-004, FR-ROLE-005; NFR-SEC-002, NFR-ERR-004
- Main business concepts involved: role, permissions, access control, restricted operations, accountability
- Dependencies on other features: depends on Users and Authentication; supports all business domains
- Classification: supporting

## 12. Audit

- Feature name: Audit
- Purpose: Preserve accountability for key system actions and operational decisions.
- Main capabilities:
  - record creation, update, approval, and status-change events
  - identify the responsible user and time for significant actions
  - support review of changes to sales, stock, and user-sensitive operations
  - maintain traceable history for operational oversight and accountability
- Related requirement identifiers: FR-AUD-001, FR-AUD-002, FR-AUD-003, FR-AUD-004; FR-SALE-HIST-001 to FR-SALE-HIST-004; NFR-DI-003, NFR-OBS-001, NFR-OBS-002
- Main business concepts involved: audit trail, change history, user responsibility, operational accountability, traceability
- Dependencies on other features: depends on Users, Inventory, Sales, and Authorization; supports Reports and governance review
- Classification: supporting

## 13. Reports

- Feature name: Reports
- Purpose: Provide operational visibility for inventory, sales, movement, and accountability-related review.
- Main capabilities:
  - generate stock and movement reports
  - generate sales and tax-related reports by relevant business filter
  - review historical and current operational information for business decisions
  - keep reporting aligned to authorization and access rules
- Related requirement identifiers: FR-REP-001, FR-REP-002, FR-REP-003, FR-REP-004, FR-REP-005; FR-SALE-HIST-001 to FR-SALE-HIST-004; NFR-PERF-003
- Main business concepts involved: operational reporting, sales analysis, stock visibility, historical review, business accountability
- Dependencies on other features: depends on Inventory, Sales, Audit, and Authorization; supports management review and governance
- Classification: supporting

## 14. Frontend

- Feature name: Frontend
- Purpose: Provide the user-facing experience for business operations, authorization-aware workflows, and operational monitoring.
- Main capabilities:
  - expose standard business workflows for categories, products, inventory, sales, users, and reports
  - present user authentication, authorization, and operational status clearly to authorized users
  - support consistent interaction patterns across the company’s core business processes
- Related requirement identifiers: all functional and non-functional requirements that describe user-facing business operations; FR-AUTH-001 to FR-AUTH-005; NFR-API-001 to NFR-API-003
- Main business concepts involved: user interaction, workflow navigation, operational visibility, role-aware access, data presentation
- Dependencies on other features: depends on the complete business capability set and delivery platform constraints; enables direct business use across all features
- Classification: delivery-related

## 15. Deployment / CI/CD

- Feature name: Deployment / CI/CD
- Purpose: Ensure the system can be deployed consistently and validated repeatedly throughout its lifecycle.
- Main capabilities:
  - provide a repeatable deployment environment for development and operational use
  - automate validation through build and test gates
  - reduce environment drift and improve release confidence
  - support continuous operational readiness for the inventory and sales workflows
- Related requirement identifiers: NFR-DEP-001 to NFR-DEP-003; NFR-CICD-001 to NFR-CICD-003
- Main business concepts involved: delivery pipeline, environment consistency, quality gates, deployment readiness, validation discipline
- Dependencies on other features: depends on the application as a whole; supports all operational features
- Classification: delivery-related

## Feature grouping rationale

The work is organized into a small number of cohesive business features to keep the system understandable without creating artificial module boundaries. Inventory and sales are the operational core, while categories, products, suppliers, and customers support those core processes. Users, authentication, authorization, audit, and reports provide the governance layer that keeps operations controlled and reviewable. Frontend and deployment/CI/CD support the delivery of the business capability set without being part of the business domain itself.

## Summary

This feature map reflects the approved project scope and the requirement set while keeping the design at a behavior and capability level. The features align with the system’s real business flow: establish the operational foundation, maintain catalog and customer relationships, manage stock and movement history, execute sales, enforce access controls, preserve auditing, and deliver operational reporting and deployment readiness.
