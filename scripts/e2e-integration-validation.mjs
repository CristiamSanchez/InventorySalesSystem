import { execSync } from 'child_process';

const BASE_URL = process.env.BASE_URL || 'http://localhost:4200';
const DB_CONTAINER = process.env.DB_CONTAINER || 'sistema-integration-db';
const DB_NAME = process.env.DB_NAME || 'inventario_integration';

let adminToken = '';
let operatorToken = '';
let adminUserId = '';
let operatorUserId = '';

let categoryId = '';
let tempCategoryId = '';
let productIdentifier = 'LAP-001';
let accessoryIdentifier = 'MOU-001';
let customerId = '';
let supplierId = '';
let saleId = '';

let passedChecks = 0;
let totalChecks = 0;

function assert(condition, message) {
  totalChecks++;
  if (!condition) {
    console.error(`  ❌ FAILED: ${message}`);
    throw new Error(`Assertion failed: ${message}`);
  }
  passedChecks++;
  console.log(`  ✓ ${message}`);
}

async function request(path, options = {}) {
  const url = `${BASE_URL}${path}`;
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {})
  };

  const res = await fetch(url, {
    method: options.method || 'GET',
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  let data = null;
  const contentType = res.headers.get('content-type') || '';
  if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
    data = await res.json();
  } else {
    data = await res.text();
  }

  return { status: res.status, headers: res.headers, data };
}

function executePsql(query) {
  const cmd = `docker exec ${DB_CONTAINER} psql -U postgres -d ${DB_NAME} -t -A -c "${query.replace(/"/g, '\\"')}"`;
  return execSync(cmd, { encoding: 'utf-8' }).trim();
}

console.log('================================================================');
console.log('  SistemaInventarioYVentas — Frontend E2E Integration Suite');
console.log(`  Testing against: ${BASE_URL} (Proxied to ASP.NET Core Kestrel)`);
console.log('================================================================\n');

async function run() {
  const now = new Date();
  const fromUtc = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
  const toUtc = new Date(now.getTime() + 24 * 60 * 60 * 1000).toISOString();

  // -------------------------------------------------------------
  // SECTION 1: App Shell & Proxy Connectivity
  // -------------------------------------------------------------
  console.log('▶ SECTION 1: App Shell & Proxy Connectivity');
  {
    const res = await request('/');
    assert(res.status === 200, 'Frontend dev server serves index.html (200 OK)');
    assert(typeof res.data === 'string' && res.data.includes('<app-root>'), 'index.html contains <app-root>');

    const apiCheck = await request('/auth/me');
    assert(apiCheck.status === 401, 'Reverse proxy routes /auth requests to Kestrel (401 Unauthorized ProblemDetails)');
    assert(apiCheck.data && apiCheck.data.title === 'Unauthorized', 'ProblemDetails returned through proxy');
  }

  // -------------------------------------------------------------
  // SECTION 2: Authentication & Authorization Workflows
  // -------------------------------------------------------------
  console.log('\n▶ SECTION 2: Authentication & Authorization Workflows');
  {
    const adminEmail = `admin-${Date.now()}@test.local`;
    const operatorEmail = `operator-${Date.now()}@test.local`;
    const password = 'Password123!';

    // Register admin user
    const regAdmin = await request('/auth/register', {
      method: 'POST',
      body: { name: 'Admin E2E User', email: adminEmail, password }
    });
    assert(regAdmin.status === 201, 'POST /auth/register creates user account (201 Created)');
    assert(regAdmin.data.userId, 'Registration response contains userId');
    assert(regAdmin.data.isActive === false, 'New user is inactive pending administrative activation');
    adminUserId = regAdmin.data.userId;

    // Login before activation must fail with 401
    const unactLogin = await request('/auth/login', {
      method: 'POST',
      body: { email: adminEmail, password }
    });
    assert(unactLogin.status === 401, 'Login of inactive account is rejected (401 Unauthorized)');
    assert(unactLogin.data.title === 'Authentication failed', 'Rejection returns RFC 7807 ProblemDetails');

    // Activate admin in DB (Roles: [1, 0] = Admin, User)
    executePsql(`UPDATE users SET "IsActive" = true, "Roles" = '[1, 0]'::jsonb WHERE "Id" = '${adminUserId}';`);

    // Login admin with valid credentials
    const loginAdmin = await request('/auth/login', {
      method: 'POST',
      body: { email: adminEmail, password }
    });
    assert(loginAdmin.status === 200, 'POST /auth/login with valid credentials succeeds (200 OK)');
    assert(loginAdmin.data.accessToken, 'Login response contains accessToken');
    assert(loginAdmin.data.tokenType === 'Bearer', 'tokenType is Bearer');
    assert(loginAdmin.data.expiresAtUtc, 'expiresAtUtc timestamp is present');
    adminToken = loginAdmin.data.accessToken;

    // Login with invalid password
    const badLogin = await request('/auth/login', {
      method: 'POST',
      body: { email: adminEmail, password: 'WrongPassword!' }
    });
    assert(badLogin.status === 401, 'POST /auth/login with invalid password returns 401');

    // Verify /auth/me for admin
    const meAdmin = await request('/auth/me', {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(meAdmin.status === 200, 'GET /auth/me with Bearer token succeeds (200 OK)');
    assert(meAdmin.data.userId === adminUserId, 'me response userId matches authenticated admin');
    assert(meAdmin.data.roles.includes('Admin'), 'roles includes Admin');
    assert(meAdmin.data.roles.includes('User'), 'roles includes User');

    // Verify /auth/me with invalid or missing token
    const meBadToken = await request('/auth/me', {
      headers: { Authorization: 'Bearer invalid.token.value' }
    });
    assert(meBadToken.status === 401, 'GET /auth/me with invalid token returns 401 Unauthorized');

    const meNoToken = await request('/auth/me');
    assert(meNoToken.status === 401, 'GET /auth/me with no token returns 401 Unauthorized');

    // Register operator user
    const regOp = await request('/auth/register', {
      method: 'POST',
      body: { name: 'Operator E2E User', email: operatorEmail, password }
    });
    assert(regOp.status === 201, 'POST /auth/register creates operator user (201 Created)');
    operatorUserId = regOp.data.userId;

    // Activate operator in DB (Roles: [0] = User only)
    executePsql(`UPDATE users SET "IsActive" = true, "Roles" = '[0]'::jsonb WHERE "Id" = '${operatorUserId}';`);

    // Login operator
    const loginOp = await request('/auth/login', {
      method: 'POST',
      body: { email: operatorEmail, password }
    });
    assert(loginOp.status === 200, 'POST /auth/login operator succeeds (200 OK)');
    operatorToken = loginOp.data.accessToken;

    const meOp = await request('/auth/me', {
      headers: { Authorization: `Bearer ${operatorToken}` }
    });
    assert(meOp.status === 200, 'GET /auth/me for operator succeeds');
    assert(meOp.data.roles.includes('User') && !meOp.data.roles.includes('Admin'), 'operator has User role only');

    // Authorization Matrix: Operator receives 403 Forbidden on AdminOnly endpoints
    const forbiddenAdminEndpoints = [
      { method: 'POST', path: '/api/categories', body: { name: 'Test' } },
      { method: 'POST', path: '/api/products', body: { identifier: 'T-1', name: 'T', categoryId: '00000000-0000-0000-0000-000000000000' } },
      { method: 'POST', path: '/api/customers', body: { name: 'Test' } },
      { method: 'POST', path: '/api/suppliers', body: { name: 'Test' } },
      { method: 'POST', path: '/api/inventory/entries', body: { productIdentifier: 'P', quantity: 1, reference: 'R' } },
      { method: 'POST', path: '/api/inventory/exits', body: { productIdentifier: 'P', quantity: 1, reference: 'R' } },
      { method: 'POST', path: '/api/inventory/adjustments', body: { productIdentifier: 'P', quantityDelta: 1, reason: 'R' } },
      { method: 'POST', path: '/api/sales', body: { customerReference: 'R' } },
      { method: 'GET', path: `/api/reports/sales-summary?fromUtc=${fromUtc}&toUtc=${toUtc}` },
      { method: 'GET', path: `/api/reports/product-sales?fromUtc=${fromUtc}&toUtc=${toUtc}` },
      { method: 'GET', path: '/api/audit' }
    ];

    for (const ep of forbiddenAdminEndpoints) {
      const res = await request(ep.path, {
        method: ep.method,
        headers: { Authorization: `Bearer ${operatorToken}` },
        body: ep.body
      });
      assert(res.status === 403, `Non-admin user receives 403 Forbidden on ${ep.method} ${ep.path}`);
    }

    // Operator CAN access Authenticated endpoints
    const allowedOperatorEndpoints = [
      '/api/categories',
      '/api/products',
      '/api/customers',
      '/api/suppliers',
      '/api/reports/inventory-status',
      `/api/reports/inventory-movements?fromUtc=${fromUtc}&toUtc=${toUtc}`
    ];

    for (const path of allowedOperatorEndpoints) {
      const res = await request(path, {
        headers: { Authorization: `Bearer ${operatorToken}` }
      });
      assert(res.status === 200, `Authenticated operator can access ${path} (200 OK)`);
    }
  }

  // -------------------------------------------------------------
  // SECTION 3: Catalog Management (Categories, Products, Customers, Suppliers)
  // -------------------------------------------------------------
  console.log('\n▶ SECTION 3: Catalog Management');
  const runId = Date.now();
  const cat1Name = `Laptops Cómputo ${runId}`;
  const cat1UpdatedName = `Laptops Portátiles ${runId}`;
  const tempCatName = `Categoría Temporal ${runId}`;
  const custName = `Corp Tec ${runId} S.A.`;
  const custUpdatedName = `Corp Tec Avanzada ${runId} S.A.`;
  const supName = `Mayorista Tech ${runId}`;
  const supUpdatedName = `Mayorista Tech Avanzado ${runId}`;
  {
    // Categories: Create Category 1
    const catCreate = await request('/api/categories', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { name: cat1Name }
    });
    assert(catCreate.status === 201, 'POST /api/categories creates category (201 Created)');
    assert(catCreate.data.id && catCreate.data.name === cat1Name, 'Category data returned');
    categoryId = catCreate.data.id;

    // List categories
    const catList = await request('/api/categories', {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(catList.status === 200, 'GET /api/categories returns list (200 OK)');
    assert(Array.isArray(catList.data) && catList.data.some(c => c.id === categoryId), 'Category present in listing');

    // Update Category 1
    const catUpdate = await request(`/api/categories/${categoryId}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { name: cat1UpdatedName, isActive: true }
    });
    assert(catUpdate.status === 200, 'PUT /api/categories/{id} updates category name (200 OK)');
    assert(catUpdate.data.name === cat1UpdatedName, 'Updated name reflected');

    // Create & Delete temporary empty Category 2
    const tempCat = await request('/api/categories', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { name: tempCatName }
    });
    assert(tempCat.status === 201, 'Temporary category created (201 Created)');
    tempCategoryId = tempCat.data.id;

    const delCat = await request(`/api/categories/${tempCategoryId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(delCat.status === 204, 'DELETE /api/categories/{id} for empty category returns 204 No Content');

    // Verify temp category removed
    const catListAfterDel = await request('/api/categories', {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(!catListAfterDel.data.some(c => c.id === tempCategoryId), 'Deleted category no longer in list');

    // Products: Create Product 1
    productIdentifier = `LAP-${Date.now() % 100000}`;
    const prodCreate = await request('/api/products', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        identifier: productIdentifier,
        name: 'ThinkPad X1 Carbon E2E',
        categoryId
      }
    });
    assert(prodCreate.status === 201, 'POST /api/products creates product (201 Created)');
    assert(prodCreate.data.identifier === productIdentifier, 'Product identifier matches');
    assert(prodCreate.data.categoryName === cat1UpdatedName, 'Category name embedded');

    // Create Accessory Product 2
    accessoryIdentifier = `MOU-${Date.now() % 100000}`;
    const accCreate = await request('/api/products', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        identifier: accessoryIdentifier,
        name: 'Mouse Óptico Ergonómico E2E',
        categoryId
      }
    });
    assert(accCreate.status === 201, 'Accessory product created (201 Created)');

    // Attempt to DELETE Category 1 with active products attached -> 409 Conflict
    const delConflict = await request(`/api/categories/${categoryId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(delConflict.status === 409, 'DELETE /api/categories/{id} with products returns 409 Conflict');
    assert(delConflict.data.title === 'Category in use', 'ProblemDetails title is "Category in use"');

    // List & Filter Products
    const prodList = await request(`/api/products?categoryId=${categoryId}&searchTerm=ThinkPad`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(prodList.status === 200, 'GET /api/products with filters returns 200');
    assert(prodList.data.length === 1 && prodList.data[0].identifier === productIdentifier, 'Search filter matches Product 1');

    // Edit Product & Status change
    const prodUpdate = await request(`/api/products/${productIdentifier}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        name: 'ThinkPad X1 Carbon Gen 11 E2E',
        categoryId,
        isActive: false
      }
    });
    assert(prodUpdate.status === 200, 'PUT /api/products/{id} updates product (200 OK)');
    assert(prodUpdate.data.isActive === false, 'Product successfully deactivated');

    // Reactivate product
    const prodReactivate = await request(`/api/products/${productIdentifier}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        name: 'ThinkPad X1 Carbon Gen 11 E2E',
        categoryId,
        isActive: true
      }
    });
    assert(prodReactivate.data.isActive === true, 'Product successfully reactivated');

    // Confirm unsupported Product Delete is 405
    const prodDelete = await request(`/api/products/${productIdentifier}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(prodDelete.status === 405, 'DELETE /api/products/{id} returns 405 Method Not Allowed (unsupported)');

    // Customers: Create Customer
    const custCreate = await request('/api/customers', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        name: custName,
        identificationNumber: `J-${Date.now() % 1000000}`,
        email: `compras-${runId}@corptec.local`,
        phone: '+506 2222-3333',
        address: 'San José, Costa Rica'
      }
    });
    assert(custCreate.status === 201, 'POST /api/customers creates customer (201 Created)');
    customerId = custCreate.data.id;

    // Edit Customer
    const custEdit = await request(`/api/customers/${customerId}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        name: custUpdatedName,
        identificationNumber: custCreate.data.identificationNumber,
        email: `compras-${runId}@corptec.local`,
        phone: '+506 2222-4444',
        address: 'San José Centro, Costa Rica'
      }
    });
    assert(custEdit.status === 200, 'PUT /api/customers/{id} updates customer details (200 OK)');
    assert(custEdit.data.name === custUpdatedName, 'Updated customer name confirmed');

    // Deactivate Customer
    const custDeact = await request(`/api/customers/${customerId}/deactivate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(custDeact.status === 200, 'POST /api/customers/{id}/deactivate succeeds (200 OK)');
    assert(custDeact.data.isActive === false, 'Customer is inactive');

    // Reactivate Customer
    const custAct = await request(`/api/customers/${customerId}/activate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(custAct.status === 200, 'POST /api/customers/{id}/activate succeeds (200 OK)');
    assert(custAct.data.isActive === true, 'Customer is active');

    // Confirm unsupported Customer Delete is 405
    const custDelete = await request(`/api/customers/${customerId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(custDelete.status === 405, 'DELETE /api/customers/{id} returns 405 Method Not Allowed (unsupported)');

    // Suppliers: Create Supplier
    const supCreate = await request('/api/suppliers', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        name: supName,
        contactName: 'Carlos Gómez',
        identificationNumber: `J-${(Date.now() + 1) % 1000000}`,
        email: `ventas-${runId}@mayoristatech.local`,
        phone: '+506 2555-4444',
        address: 'Heredia, Costa Rica'
      }
    });
    assert(supCreate.status === 201, 'POST /api/suppliers creates supplier (201 Created)');
    supplierId = supCreate.data.id;

    // Edit Supplier
    const supEdit = await request(`/api/suppliers/${supplierId}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        name: supUpdatedName,
        contactName: 'Carlos Gómez Fernández',
        identificationNumber: supCreate.data.identificationNumber,
        email: `ventas-${runId}@mayoristatech.local`,
        phone: '+506 2555-9999',
        address: 'Heredia Centro, Costa Rica'
      }
    });
    assert(supEdit.status === 200, 'PUT /api/suppliers/{id} updates supplier details (200 OK)');
    assert(supEdit.data.contactName === 'Carlos Gómez Fernández', 'Updated contact name confirmed');

    // Deactivate Supplier
    const supDeact = await request(`/api/suppliers/${supplierId}/deactivate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(supDeact.status === 200, 'POST /api/suppliers/{id}/deactivate succeeds');
    assert(supDeact.data.isActive === false, 'Supplier is inactive');

    // Reactivate Supplier
    const supAct = await request(`/api/suppliers/${supplierId}/activate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(supAct.status === 200, 'POST /api/suppliers/{id}/activate succeeds');
    assert(supAct.data.isActive === true, 'Supplier is active');

    // Confirm unsupported Supplier Delete is 405
    const supDelete = await request(`/api/suppliers/${supplierId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(supDelete.status === 405, 'DELETE /api/suppliers/{id} returns 405 Method Not Allowed (unsupported)');
  }

  // -------------------------------------------------------------
  // SECTION 4: Inventory & Movement Traceability
  // -------------------------------------------------------------
  console.log('\n▶ SECTION 4: Inventory & Movement Traceability');
  {
    // Initial stock report shows product with 0 stock and HasInventoryRecord = false
    const initStock = await request(`/api/reports/inventory-status?categoryId=${categoryId}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(initStock.status === 200, 'GET /api/reports/inventory-status returns 200');
    const pStatus = initStock.data.items.find(i => i.productIdentifier === productIdentifier);
    assert(pStatus && pStatus.quantity === 0 && pStatus.hasInventoryRecord === false, 'Uninitialized product reports 0 stock and hasInventoryRecord = false');

    // Stock Entry with Supplier snapshot (20 units)
    const entryRes = await request('/api/inventory/entries', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier,
        quantity: 20,
        reference: 'Factura Compra 8849',
        supplierId
      }
    });
    assert(entryRes.status === 200, 'POST /api/inventory/entries records stock entry (200 OK)');
    assert(entryRes.data.quantity === 20, 'Stock balance updated to 20');

    // Stock Entry for accessory (50 units)
    const accEntry = await request('/api/inventory/entries', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier: accessoryIdentifier,
        quantity: 50,
        reference: 'Stock inicial accesorios'
      }
    });
    assert(accEntry.status === 200 && accEntry.data.quantity === 50, 'Accessory stock initialized to 50');

    // Verify Movement History for product
    const movs = await request(`/api/inventory/${productIdentifier}/movements`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(movs.status === 200, 'GET /api/inventory/{productIdentifier}/movements returns movements');
    assert(movs.data.length >= 1, 'Movement list contains recorded entry');
    const entryMov = movs.data[0];
    assert(entryMov.movementType === 'Entry', 'Movement type is Entry');
    assert(entryMov.supplierId === supplierId, 'Supplier ID correctly associated');
    assert(entryMov.supplierName === supUpdatedName, 'Supplier name snapshotted at entry time');

    // Stock Exit (2 units)
    const exitRes = await request('/api/inventory/exits', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier,
        quantity: 2,
        reference: 'Muestra comercial cliente'
      }
    });
    assert(exitRes.status === 200 && exitRes.data.quantity === 18, 'POST /api/inventory/exits reduces stock to 18');

    // Insufficient Stock Exit Conflict (attempting to exit 100 units from 18 available)
    const overExit = await request('/api/inventory/exits', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier,
        quantity: 100,
        reference: 'Retiro excesivo'
      }
    });
    assert(overExit.status === 409, 'Excessive stock exit returns 409 Conflict');
    assert(overExit.data.title === 'Insufficient stock', 'Problem title is "Insufficient stock"');

    // Verify stock is still 18 after failed exit
    const checkStock = await request(`/api/inventory/${productIdentifier}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(checkStock.status === 200 && checkStock.data.quantity === 18, 'Stock remains unchanged at 18 after failed operation');

    // Stock Adjustment (-3 units)
    const adjRes = await request('/api/inventory/adjustments', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier,
        quantityDelta: -3,
        reason: 'Ajuste físico por caja rota'
      }
    });
    assert(adjRes.status === 200 && adjRes.data.quantity === 15, 'POST /api/inventory/adjustments updates stock to 15');

    // Inactive Supplier Entry Rejection
    await request(`/api/suppliers/${supplierId}/deactivate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    const inactiveSupEntry = await request('/api/inventory/entries', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier,
        quantity: 5,
        reference: 'Entrada proveedor inactivo',
        supplierId
      }
    });
    assert(inactiveSupEntry.status === 400, 'Entry with inactive supplier is rejected with 400 Bad Request');
    // Reactivate supplier
    await request(`/api/suppliers/${supplierId}/activate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
  }

  // -------------------------------------------------------------
  // SECTION 5: Sales Workflow & Atomic Confirmation
  // -------------------------------------------------------------
  console.log('\n▶ SECTION 5: Sales Workflow & Atomic Confirmation');
  {
    // Create Draft Sale with Customer
    const saleCreate = await request('/api/sales', {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { customerId }
    });
    assert(saleCreate.status === 201, 'POST /api/sales creates draft sale (201 Created)');
    assert(saleCreate.data.status === 'Draft', 'Sale status is Draft');
    assert(saleCreate.data.customerReference === custUpdatedName, 'Customer reference snapshotted');
    saleId = saleCreate.data.id;

    // Update Customer on Draft Sale (walk-in)
    const updateCust = await request(`/api/sales/${saleId}/customer`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { customerReference: 'Cliente Mostrador Ocasional' }
    });
    assert(updateCust.status === 200, 'PUT /api/sales/{id}/customer updates customer to walk-in');
    assert(updateCust.data.customerReference === 'Cliente Mostrador Ocasional', 'Walk-in reference set');

    // Re-assign registered customer
    await request(`/api/sales/${saleId}/customer`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { customerId }
    });

    // Add Detail Line 1: LAP-001, quantity 2, unitPrice 1200, taxRate 0.13
    const addLine1 = await request(`/api/sales/${saleId}/details`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier,
        quantity: 2,
        unitPrice: 1200.00,
        taxRate: 0.13
      }
    });
    assert(addLine1.status === 200, 'POST /api/sales/{id}/details adds Line 1 (200 OK)');
    assert(addLine1.data.subtotal === 2400.00, 'Server-calculated subtotal is 2400.00');
    assert(addLine1.data.taxAmount === 312.00, 'Server-calculated taxAmount is 312.00 (13%)');
    assert(addLine1.data.total === 2712.00, 'Server-calculated total is 2712.00');

    // Add Detail Line 2: MOU-001, quantity 4, unitPrice 35, taxRate 0.13
    const addLine2 = await request(`/api/sales/${saleId}/details`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        productIdentifier: accessoryIdentifier,
        quantity: 4,
        unitPrice: 35.00,
        taxRate: 0.13
      }
    });
    assert(addLine2.status === 200, 'Line 2 added successfully');
    assert(addLine2.data.details.length === 2, 'Sale has 2 detail lines');
    assert(addLine2.data.subtotal === 2540.00, 'Recalculated subtotal is 2540.00 (2400 + 140)');

    // Update Detail Line 2 quantity to 5
    const updLine2 = await request(`/api/sales/${saleId}/details/${accessoryIdentifier}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: {
        quantity: 5,
        unitPrice: 35.00,
        taxRate: 0.13
      }
    });
    assert(updLine2.status === 200, 'PUT /api/sales/{id}/details/{productId} updates line quantity to 5');
    assert(updLine2.data.subtotal === 2575.00, 'Recalculated subtotal is 2575.00');

    // Remove Detail Line 2
    const delLine2 = await request(`/api/sales/${saleId}/details/${accessoryIdentifier}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(delLine2.status === 200, 'DELETE /api/sales/{id}/details/{productId} removes line');
    assert(delLine2.data.details.length === 1, 'Sale now has 1 detail line');
    assert(delLine2.data.total === 2712.00, 'Sale total returned to 2712.00');

    // Test Insufficient Stock Conflict on Sale Confirmation
    // Current stock of LAP-001 is 15. Update line to require 100 units!
    await request(`/api/sales/${saleId}/details/${productIdentifier}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { quantity: 100, unitPrice: 1200.00, taxRate: 0.13 }
    });

    const failedConfirm = await request(`/api/sales/${saleId}/confirm`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(failedConfirm.status === 409, 'Sale confirmation with insufficient stock returns 409 Conflict');
    assert(failedConfirm.data.title === 'Insufficient stock', 'Title is "Insufficient stock"');

    // Verify sale is STILL in Draft status and inventory is STILL 15
    const saleCheck = await request(`/api/sales/${saleId}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(saleCheck.data.status === 'Draft', 'Sale remains in Draft status after abortive confirmation');

    const invCheck = await request(`/api/inventory/${productIdentifier}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(invCheck.data.quantity === 15, 'Inventory stock remains completely intact at 15');

    // Adjust Line 1 quantity to 5 (available is 15) and CONFIRM
    await request(`/api/sales/${saleId}/details/${productIdentifier}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { quantity: 5, unitPrice: 1200.00, taxRate: 0.13 }
    });

    const successConfirm = await request(`/api/sales/${saleId}/confirm`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(successConfirm.status === 200, 'POST /api/sales/{id}/confirm succeeds (200 OK)');
    assert(successConfirm.data.status === 'Confirmed', 'Sale transitioned to Confirmed');
    assert(successConfirm.data.confirmedAtUtc, 'confirmedAtUtc timestamp is populated');
    assert(successConfirm.data.total === 6780.00, 'Confirmed sale total is 6780.00 (6000 + 780)');

    // Verify Inventory Stock deducted atomically (15 - 5 = 10)
    const invAfterConfirm = await request(`/api/inventory/${productIdentifier}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(invAfterConfirm.data.quantity === 10, 'Stock deducted to 10 atomically upon confirmation');

    // Verify movement recorded referencing Sale
    const movsAfterSale = await request(`/api/inventory/${productIdentifier}/movements`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    const saleMov = movsAfterSale.data.find(m => m.reference && m.reference.includes(`Sale ${saleId}`));
    assert(saleMov && saleMov.movementType === 'Exit' && saleMov.quantity === 5, 'Inventory movement created with Sale reference');

    // Lookup confirmed sale by ID (accessible by operator too)
    const opLookup = await request(`/api/sales/${saleId}`, {
      headers: { Authorization: `Bearer ${operatorToken}` }
    });
    assert(opLookup.status === 200, 'Operator can look up confirmed sale by ID (200 OK)');
    assert(opLookup.data.status === 'Confirmed', 'Sale details intact');

    // Modifying confirmed sale details must be rejected
    const modifyConfirmed = await request(`/api/sales/${saleId}/details`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` },
      body: { productIdentifier, quantity: 1, unitPrice: 100, taxRate: 0 }
    });
    assert(modifyConfirmed.status === 400, 'Adding details to confirmed sale rejected with 400 Bad Request');

    // Verify cancellation endpoint is unsupported
    const cancelReq = await request(`/api/sales/${saleId}/cancel`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(cancelReq.status === 404 || cancelReq.status === 405, 'Sale cancellation endpoint is absent (404/405)');
  }

  // -------------------------------------------------------------
  // SECTION 6: Reports & Analytics
  // -------------------------------------------------------------
  console.log('\n▶ SECTION 6: Reports & Analytics');
  {
    // Sales Summary Report
    const salesSummary = await request(`/api/reports/sales-summary?fromUtc=${fromUtc}&toUtc=${toUtc}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(salesSummary.status === 200, 'GET /api/reports/sales-summary returns 200');
    assert(salesSummary.data.confirmedTransactions >= 1, 'confirmedTransactions count >= 1');
    assert(salesSummary.data.totalRevenue >= 6780.00, 'totalRevenue reflects realized confirmed revenue');
    const saleItem = salesSummary.data.sales.find(s => s.id === saleId);
    assert(saleItem && saleItem.status === 'Confirmed', 'Confirmed sale listed in summary');
    assert(saleItem.totalUnits === 5, 'totalUnits is 5');

    // Filter Sales Summary by customerId
    const custSummary = await request(`/api/reports/sales-summary?fromUtc=${fromUtc}&toUtc=${toUtc}&customerId=${customerId}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(custSummary.status === 200 && custSummary.data.sales.length >= 1, 'Sales summary filtered by customerId succeeds');

    // Product Sales Performance Report
    const prodSales = await request(`/api/reports/product-sales?fromUtc=${fromUtc}&toUtc=${toUtc}&categoryId=${categoryId}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(prodSales.status === 200, 'GET /api/reports/product-sales returns 200');
    assert(prodSales.data.totalProductsSold >= 1, 'totalProductsSold count >= 1');
    const pSale = prodSales.data.items.find(i => i.productIdentifier === productIdentifier);
    assert(pSale && pSale.unitsSold === 5, 'Units sold for product is 5');
    assert(pSale.total === 6780.00, 'Product gross revenue matches line total (6780.00)');

    // Inventory Stock Status Report
    const invStatus = await request(`/api/reports/inventory-status?categoryId=${categoryId}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(invStatus.status === 200, 'GET /api/reports/inventory-status returns 200');
    const lapStock = invStatus.data.items.find(i => i.productIdentifier === productIdentifier);
    assert(lapStock && lapStock.quantity === 10 && lapStock.stockStatus === 'InStock', 'Product reports 10 units InStock');

    // Inventory Movement Traceability Report
    const movReport = await request(`/api/reports/inventory-movements?fromUtc=${fromUtc}&toUtc=${toUtc}&productIdentifier=${productIdentifier}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(movReport.status === 200, 'GET /api/reports/inventory-movements returns 200');
    assert(movReport.data.totalMovements >= 4, 'Reports all 4 movements (Entry 20, Exit 2, Adj -3, Sale 5)');
    assert(movReport.data.netQuantityDelta === 10, 'Net quantity delta is exactly +10');
    assert(movReport.data.totalUnitsIn === 20, 'Total units in is 20');
    assert(movReport.data.totalUnitsOut === 7, 'Total units out is 7 (2 manual exit + 5 sale exit)');
    assert(movReport.data.adjustmentCount === 1, 'Adjustment count is 1 (-3 units)');
  }

  // -------------------------------------------------------------
  // SECTION 7: Audit Log & Forensic Traceability
  // -------------------------------------------------------------
  console.log('\n▶ SECTION 7: Audit Log & Forensic Traceability');
  {
    // List Audit Events
    const auditList = await request('/api/audit', {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(auditList.status === 200, 'GET /api/audit returns audit event list (200 OK)');
    assert(Array.isArray(auditList.data) && auditList.data.length > 0, 'Audit events captured');

    // Verify chronological order (descending by occurredAtUtc)
    const dates = auditList.data.map(e => new Date(e.occurredAtUtc).getTime());
    let isSorted = true;
    for (let i = 0; i < dates.length - 1; i++) {
      if (dates[i] < dates[i + 1]) {
        isSorted = false;
        break;
      }
    }
    assert(isSorted, 'Audit events natively sorted in descending chronological order');

    // Filter by eventType="SaleConfirmed"
    const saleConfAudit = await request('/api/audit?eventType=SaleConfirmed', {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(saleConfAudit.status === 200, 'Filter audit by eventType=SaleConfirmed succeeds');
    assert(saleConfAudit.data.length >= 1 && saleConfAudit.data.every(e => e.eventType === 'SaleConfirmed'), 'All events have eventType=SaleConfirmed');

    // Filter by entityName="Sale"
    const saleEntityAudit = await request('/api/audit?entityName=Sale', {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(saleEntityAudit.status === 200 && saleEntityAudit.data.every(e => e.entityName === 'Sale'), 'Filter by entityName=Sale returns only Sale events');

    // Detail inspection by ID
    const sampleEvent = saleConfAudit.data[0];
    const eventDetail = await request(`/api/audit/${sampleEvent.id}`, {
      headers: { Authorization: `Bearer ${adminToken}` }
    });
    assert(eventDetail.status === 200, 'GET /api/audit/{id} returns single event');
    assert(eventDetail.data.id === sampleEvent.id, 'Event ID matches');
    assert(eventDetail.data.hasUser === true, 'hasUser is true for user-initiated action');
    assert(eventDetail.data.userId === adminUserId, 'User ID attribution matches acting admin');

    // Validate metadata JSON
    assert(typeof eventDetail.data.metadataJson === 'string', 'metadataJson is string');
    let parsedMeta = null;
    try {
      parsedMeta = JSON.parse(eventDetail.data.metadataJson);
    } catch {
      // ignore
    }
    assert(parsedMeta !== null, 'metadataJson parses as valid JSON object');

    // Assert zero sensitive secrets in metadata
    const rawJson = eventDetail.data.metadataJson.toLowerCase();
    assert(!rawJson.includes('password'), 'Metadata contains no password strings');
    assert(!rawJson.includes('secret'), 'Metadata contains no secret strings');
    assert(!rawJson.includes('token'), 'Metadata contains no token strings');
    assert(!rawJson.includes('hash'), 'Metadata contains no hash strings');
  }

  console.log('\n================================================================');
  console.log(`  E2E INTEGRATION VALIDATION PASSED: ${passedChecks}/${totalChecks} assertions passed`);
  console.log('================================================================\n');
}

run().catch((err) => {
  console.error('\n❌ E2E Integration Suite Encountered Error:', err);
  process.exit(1);
});
