# Application Screenshot Guide

This directory holds visual demonstration screenshots of the **SistemaInventarioVentas** (InventorySalesSystem) web application for public portfolio presentation in the root `README.md`.

## Recommended Capture Environment

To produce clean, consistent screenshots:

1. **Browser**: Chromium or Google Chrome in light mode or high-contrast theme.
2. **Viewport**: 1920 × 1080 (16:9 1080p) or 1440 × 900 at 100% browser zoom (`Ctrl + 0`).
3. **Format**: PNG, optimized/lossless compression.
4. **Data Seed**: Run local development or production compose stack and populate sample categories, products, customers, suppliers, inventory stock entries, and at least 2 confirmed sales.

---

## Screen Inventory & Status

| # | Screen Name | Route | Filename | Status | Key Visual Elements to Capture |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **01** | **Authentication** | `/login` | `01-login.png` | **Captured** (1920×1080) | Centered login card, brand logo/title, email & password inputs, sign-in button. |
| **02** | **Executive Dashboard** | `/dashboard` | `02-dashboard.png` | Pending Manual Capture | Navigation sidebar, KPI summary cards, quick navigation shortcuts, operational indicators. |
| **03** | **Catalog - Categories** | `/categories` | `03-catalog-categories.png` | Pending Manual Capture | Category table, product count badges, active status, creation modal dialog. |
| **04** | **Catalog - Products** | `/products` | `04-catalog-products.png` | Pending Manual Capture | SKU identifiers, product names, category badges, active/inactive toggles, search filter. |
| **05** | **Customer Registry** | `/customers` | `05-customers.png` | Pending Manual Capture | Tax identification numbers (`Cedula`/`RUT`/`NIT`), contact info, active status toggles. |
| **06** | **Supplier Registry** | `/suppliers` | `06-suppliers.png` | Pending Manual Capture | Contact person, phone number, physical address, active status toggles. |
| **07** | **Inventory - Stock Status** | `/inventory` | `07-inventory-stock.png` | Pending Manual Capture | Stock status tab, categorical badges (`InStock`, `LowStock`, `OutOfStock`), inventory summary. |
| **08** | **Inventory - Traceability** | `/inventory` | `08-inventory-movements.png` | Pending Manual Capture | Movement history tab, entry/exit/adjustment types, supplier snapshots, user attribution. |
| **09** | **Sales - POS Builder** | `/sales` | `09-sales-pos.png` | Pending Manual Capture | Draft sale builder, customer selector, itemized line additions, server tax & total calculation. |
| **10** | **Sales - Printable Receipt** | `/sales` | `10-sales-receipt.png` | Pending Manual Capture | Clean thermal receipt modal, sale UTC confirmation timestamp, line items, totals, print button. |
| **11** | **Reports & Analytics** | `/reports` | `11-reports-analytics.png` | Pending Manual Capture | Multi-tab dashboard, sales summary cards, product sales ranking, CSS revenue progress bars. |
| **12** | **Audit Log Stream** | `/audit` | `12-audit-log.png` | Pending Manual Capture | Forensic audit log stream, event filtering, user IDs, UTC timestamps, JSON metadata modal. |

---

## Screenshot Capture Workflow

### Automated Capture (Headless)
The public authentication screen (`01-login.png`) was captured directly from the live Angular application in a headless browser:
```bash
firefox --headless --screenshot docs/screenshots/01-login.png --window-size 1920,1080 http://localhost:4200/login
```

### Manual Capture for Authenticated Views (02–12)
Because the operational views require JWT authentication stored in browser `sessionStorage` and dynamic API interactions, capture the remaining views interactively:

1. **Start the local stack**:
   ```bash
   docker compose up -d
   dotnet ef database update --project src/SistemaInventarioVentas.Infrastructure --startup-project src/SistemaInventarioVentas.Infrastructure
   dotnet run --project src/SistemaInventarioVentas.API &
   cd frontend && npm start
   ```

2. **Open browser and authenticate**:
   - Navigate to `http://localhost:4200/login`.
   - Log in with the administrative credentials created during setup.

3. **Capture screens**:
   - Use browser Developer Tools Device Mode (`Ctrl + Shift + M`) set to `1920x1080` or maximize the browser window at 100% zoom.
   - Navigate to each route (`/dashboard`, `/categories`, etc.).
   - Trigger the browser full-page or node screenshot command (e.g., in Chrome DevTools: `Ctrl + Shift + P` -> "Capture screenshot").
   - Save the image directly to `docs/screenshots/<filename>` with the corresponding name from the table above.
