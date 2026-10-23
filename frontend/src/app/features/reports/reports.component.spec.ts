import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ReportsComponent } from './reports.component';
import {
  AuthService,
  CategoryResponse,
  CategoryService,
  CustomerResponse,
  CustomerService,
  InventoryMovementReportResponse,
  InventoryStatusReportResponse,
  ProductResponse,
  ProductSalesReportResponse,
  ProductService,
  ReportService,
  SalesSummaryReportResponse,
  SupplierResponse,
  SupplierService
} from '@core';

describe('ReportsComponent', () => {
  let mockReportService: {
    getSalesSummary: ReturnType<typeof vi.fn>;
    getProductSales: ReturnType<typeof vi.fn>;
    getInventoryStatus: ReturnType<typeof vi.fn>;
    getInventoryMovements: ReturnType<typeof vi.fn>;
  };
  let mockAuthService: {
    isAdmin: ReturnType<typeof vi.fn>;
  };
  let mockCategoryService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockCustomerService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockSupplierService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockProductService: {
    list: ReturnType<typeof vi.fn>;
  };

  const mockSalesSummaryReport: SalesSummaryReportResponse = {
    fromUtc: '2026-10-01T00:00:00.000Z',
    toUtc: '2026-10-22T23:59:59.999Z',
    totalTransactions: 3,
    confirmedTransactions: 1,
    draftTransactions: 1,
    cancelledTransactions: 1,
    totalSubtotal: 100.0,
    totalTax: 16.0,
    totalRevenue: 116.0,
    sales: [
      {
        id: 'sale-1',
        createdAtUtc: '2026-10-15T10:00:00Z',
        confirmedAtUtc: '2026-10-15T10:05:00Z',
        status: 'Confirmed',
        customerId: 'cust-1',
        customerReference: 'Cliente Premium',
        userId: 'user-admin',
        totalUnits: 2,
        lineCount: 1,
        subtotal: 100.0,
        taxAmount: 16.0,
        total: 116.0
      },
      {
        id: 'sale-2',
        createdAtUtc: '2026-10-16T11:00:00Z',
        confirmedAtUtc: null,
        status: 'Draft',
        customerId: null,
        customerReference: 'Mostrador',
        userId: 'user-admin',
        totalUnits: 1,
        lineCount: 1,
        subtotal: 50.0,
        taxAmount: 8.0,
        total: 58.0
      }
    ]
  };

  const mockProductSalesReport: ProductSalesReportResponse = {
    fromUtc: '2026-10-01T00:00:00.000Z',
    toUtc: '2026-10-22T23:59:59.999Z',
    totalProductsSold: 2,
    totalUnitsSold: 15,
    totalSubtotal: 1000.0,
    totalTax: 160.0,
    totalRevenue: 1160.0,
    items: [
      {
        productIdentifier: 'PROD-01',
        productName: 'Taladro Percutor',
        categoryId: 'cat-1',
        categoryName: 'Herramientas',
        unitsSold: 10,
        subtotal: 800.0,
        taxAmount: 128.0,
        total: 928.0
      },
      {
        productIdentifier: 'PROD-02',
        productName: 'Broca Concreto',
        categoryId: 'cat-1',
        categoryName: 'Herramientas',
        unitsSold: 5,
        subtotal: 200.0,
        taxAmount: 32.0,
        total: 232.0
      }
    ]
  };

  const mockInventoryStatusReport: InventoryStatusReportResponse = {
    totalProducts: 2,
    totalUnitsOnHand: 50,
    inStockProductsCount: 1,
    outOfStockProductsCount: 1,
    items: [
      {
        productIdentifier: 'PROD-01',
        productName: 'Taladro Percutor',
        categoryId: 'cat-1',
        categoryName: 'Herramientas',
        quantity: 50,
        hasInventoryRecord: true,
        isActive: true,
        stockStatus: 'InStock'
      },
      {
        productIdentifier: 'PROD-02',
        productName: 'Sierra Circular',
        categoryId: 'cat-1',
        categoryName: 'Herramientas',
        quantity: 0,
        hasInventoryRecord: false,
        isActive: true,
        stockStatus: 'OutOfStock'
      }
    ]
  };

  const mockInventoryMovementReport: InventoryMovementReportResponse = {
    fromUtc: '2026-10-01T00:00:00.000Z',
    toUtc: '2026-10-22T23:59:59.999Z',
    totalMovements: 2,
    entryCount: 1,
    exitCount: 1,
    adjustmentCount: 0,
    totalUnitsIn: 60,
    totalUnitsOut: 10,
    netQuantityDelta: 50,
    movements: [
      {
        id: 'mov-1',
        occurredAtUtc: '2026-10-10T12:00:00Z',
        productIdentifier: 'PROD-01',
        movementType: 'Entry',
        quantity: 60,
        reference: 'Factura F-100',
        supplierId: 'sup-1',
        supplierName: 'Distribuidora Central',
        userId: 'admin-user'
      },
      {
        id: 'mov-2',
        occurredAtUtc: '2026-10-15T15:00:00Z',
        productIdentifier: 'PROD-01',
        movementType: 'Exit',
        quantity: 10,
        reference: 'Venta V-101',
        supplierId: null,
        supplierName: null,
        userId: 'admin-user'
      }
    ]
  };

  const mockCategories: CategoryResponse[] = [
    { id: 'cat-1', name: 'Herramientas', isActive: true, products: [] }
  ];

  const mockCustomers: CustomerResponse[] = [
    {
      id: 'cust-1',
      name: 'Cliente Premium',
      identificationNumber: 'RFC-101',
      email: 'prem@empresa.com',
      phone: null,
      address: null,
      isActive: true,
      createdAtUtc: '2026-10-01T00:00:00Z'
    }
  ];

  const mockSuppliers: SupplierResponse[] = [
    {
      id: 'sup-1',
      name: 'Distribuidora Central',
      identificationNumber: 'SUP-001',
      contactName: 'Carlos Proveedor',
      email: 'carlos@sup.com',
      phone: null,
      address: null,
      isActive: true,
      createdAtUtc: '2026-10-01T00:00:00Z'
    }
  ];

  const mockProducts: ProductResponse[] = [
    {
      identifier: 'PROD-01',
      name: 'Taladro Percutor',
      categoryId: 'cat-1',
      categoryName: 'Herramientas',
      isActive: true
    }
  ];

  beforeEach(() => {
    mockReportService = {
      getSalesSummary: vi.fn().mockReturnValue(of(mockSalesSummaryReport)),
      getProductSales: vi.fn().mockReturnValue(of(mockProductSalesReport)),
      getInventoryStatus: vi.fn().mockReturnValue(of(mockInventoryStatusReport)),
      getInventoryMovements: vi.fn().mockReturnValue(of(mockInventoryMovementReport))
    };

    mockAuthService = {
      isAdmin: vi.fn().mockReturnValue(true)
    };

    mockCategoryService = {
      list: vi.fn().mockReturnValue(of(mockCategories))
    };

    mockCustomerService = {
      list: vi.fn().mockReturnValue(of(mockCustomers))
    };

    mockSupplierService = {
      list: vi.fn().mockReturnValue(of(mockSuppliers))
    };

    mockProductService = {
      list: vi.fn().mockReturnValue(of(mockProducts))
    };

    TestBed.configureTestingModule({
      imports: [ReportsComponent],
      providers: [
        { provide: ReportService, useValue: mockReportService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: CategoryService, useValue: mockCategoryService },
        { provide: CustomerService, useValue: mockCustomerService },
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: ProductService, useValue: mockProductService }
      ]
    });
  });

  it('should create component and initialize default tab as sales-summary for admin user', () => {
    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.activeTab()).toBe('sales-summary');
    expect(mockReportService.getSalesSummary).toHaveBeenCalled();
    expect(component.salesReport()).toEqual(mockSalesSummaryReport);
    expect(component.categories().length).toBe(1);
    expect(component.customers().length).toBe(1);
    expect(component.suppliers().length).toBe(1);
    expect(component.products().length).toBe(1);
  });

  it('should initialize default tab as inventory-status for non-admin user', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.activeTab()).toBe('inventory-status');
    expect(mockReportService.getInventoryStatus).toHaveBeenCalled();
    expect(mockReportService.getSalesSummary).not.toHaveBeenCalled();
    expect(component.inventoryStatusReport()).toEqual(mockInventoryStatusReport);
  });

  it('should show restriction notice when non-admin user switches to admin-only tab', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.setTab('sales-summary');
    fixture.detectChanges();

    expect(component.activeTab()).toBe('sales-summary');
    expect(mockReportService.getSalesSummary).not.toHaveBeenCalled();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Acceso Restringido a Administradores');
  });

  it('should load sales summary report with valid query parameters', () => {
    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.salesFromDate.set('2026-10-01');
    component.salesToDate.set('2026-10-20');
    component.salesCustomerId.set('cust-1');
    component.salesStatus.set('Confirmed');
    component.salesUserId.set('admin-1');

    component.loadSalesSummary();

    expect(mockReportService.getSalesSummary).toHaveBeenCalledWith({
      fromUtc: '2026-10-01T00:00:00.000Z',
      toUtc: '2026-10-20T23:59:59.999Z',
      customerId: 'cust-1',
      status: 'Confirmed',
      userId: 'admin-1'
    });
    expect(component.errorMessage()).toBeNull();
  });

  it('should reject invalid date range where fromDate is greater than toDate', () => {
    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.salesFromDate.set('2026-10-25');
    component.salesToDate.set('2026-10-10');

    mockReportService.getSalesSummary.mockClear();
    component.loadSalesSummary();

    expect(mockReportService.getSalesSummary).not.toHaveBeenCalled();
    expect(component.errorMessage()).toBe('La fecha inicial no puede ser posterior a la fecha final.');
  });

  it('should load product sales report and compute top products ranking', () => {
    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.setTab('product-sales');
    fixture.detectChanges();

    expect(mockReportService.getProductSales).toHaveBeenCalled();
    expect(component.productSalesReport()).toEqual(mockProductSalesReport);

    const top = component.topProducts();
    expect(top.length).toBe(2);
    expect(top[0].productIdentifier).toBe('PROD-01');
    expect(top[0].total).toBe(928.0);

    const pct = component.getRevenuePercentage(928.0, 1160.0);
    expect(pct).toBe(80);
  });

  it('should load inventory status report and filter items by search term', () => {
    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.setTab('inventory-status');
    fixture.detectChanges();

    expect(mockReportService.getInventoryStatus).toHaveBeenCalled();
    expect(component.inventoryStatusReport()?.totalProducts).toBe(2);

    // Initial unfiltered
    expect(component.filteredInventoryItems().length).toBe(2);

    // Search by name
    component.inventorySearchTerm.set('Taladro');
    expect(component.filteredInventoryItems().length).toBe(1);
    expect(component.filteredInventoryItems()[0].productIdentifier).toBe('PROD-01');

    // Search by code
    component.inventorySearchTerm.set('PROD-02');
    expect(component.filteredInventoryItems().length).toBe(1);
    expect(component.filteredInventoryItems()[0].productName).toBe('Sierra Circular');

    // Stock health percentage
    expect(component.stockHealthPercentage()).toBe(50); // 1 in stock of 2 total = 50%
  });

  it('should load inventory movement traceability report with correct parameters', () => {
    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.setTab('inventory-movements');
    component.movementFromDate.set('2026-10-05');
    component.movementToDate.set('2026-10-20');
    component.movementType.set('Entry');
    component.movementSupplierId.set('sup-1');
    component.movementProductIdentifier.set('PROD-01');

    component.loadInventoryMovements();

    expect(mockReportService.getInventoryMovements).toHaveBeenCalledWith({
      fromUtc: '2026-10-05T00:00:00.000Z',
      toUtc: '2026-10-20T23:59:59.999Z',
      productIdentifier: 'PROD-01',
      movementType: 'Entry',
      supplierId: 'sup-1'
    });
    expect(component.inventoryMovementReport()?.netQuantityDelta).toBe(50);
  });

  it('should handle API errors and display user-friendly message', () => {
    mockReportService.getSalesSummary.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' }))
    );

    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.errorMessage()).toContain('error en el servidor');
    expect(component.loadingSales()).toBe(false);
  });

  it('should reset filters and reload report for current tab', () => {
    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.salesCustomerId.set('cust-1');
    component.salesStatus.set('Confirmed');

    component.resetSalesFilters();

    expect(component.salesCustomerId()).toBe('');
    expect(component.salesStatus()).toBe('');
    expect(mockReportService.getSalesSummary).toHaveBeenCalled();
  });

  it('should invoke window.print when printReport is called', () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});

    const fixture = TestBed.createComponent(ReportsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.printReport();
    expect(printSpy).toHaveBeenCalled();

    printSpy.mockRestore();
  });
});
