import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { InventoryComponent } from './inventory.component';
import {
  AuthService,
  CategoryResponse,
  CategoryService,
  InventoryMovementReportResponse,
  InventoryMovementResponse,
  InventoryResponse,
  InventoryService,
  InventoryStatusReportResponse,
  SupplierResponse,
  SupplierService
} from '@core';

describe('InventoryComponent', () => {
  let mockInventoryService: {
    getByProductIdentifier: ReturnType<typeof vi.fn>;
    getMovementsByProduct: ReturnType<typeof vi.fn>;
    recordEntry: ReturnType<typeof vi.fn>;
    recordExit: ReturnType<typeof vi.fn>;
    recordAdjustment: ReturnType<typeof vi.fn>;
    getStatusReport: ReturnType<typeof vi.fn>;
    getMovementReport: ReturnType<typeof vi.fn>;
  };
  let mockCategoryService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockSupplierService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockAuthService: {
    isAdmin: ReturnType<typeof vi.fn>;
  };

  const initialStockReport: InventoryStatusReportResponse = {
    totalProducts: 2,
    totalUnitsOnHand: 55,
    inStockProductsCount: 1,
    outOfStockProductsCount: 1,
    items: [
      {
        productIdentifier: 'PROD-01',
        productName: 'Taladro Percutor',
        categoryId: 'cat-1',
        categoryName: 'Herramientas',
        quantity: 55,
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

  const initialMovementReport: InventoryMovementReportResponse = {
    fromUtc: '2026-10-01T00:00:00.000Z',
    toUtc: '2026-10-22T23:59:59.999Z',
    totalMovements: 2,
    entryCount: 1,
    exitCount: 1,
    adjustmentCount: 0,
    totalUnitsIn: 60,
    totalUnitsOut: 5,
    netQuantityDelta: 55,
    movements: [
      {
        id: 'mv-1',
        occurredAtUtc: '2026-10-10T12:00:00Z',
        productIdentifier: 'PROD-01',
        movementType: 'Entry',
        quantity: 60,
        reference: 'Factura F-100',
        supplierId: 'sup-1',
        supplierName: 'Ferretería Global',
        userId: 'user-admin'
      },
      {
        id: 'mv-2',
        occurredAtUtc: '2026-10-12T15:00:00Z',
        productIdentifier: 'PROD-01',
        movementType: 'Exit',
        quantity: 5,
        reference: 'Despacho Venta 01',
        supplierId: null,
        supplierName: null,
        userId: 'user-admin'
      }
    ]
  };

  const initialCategories: CategoryResponse[] = [
    { id: 'cat-1', name: 'Herramientas', isActive: true, products: [] }
  ];

  const initialSuppliers: SupplierResponse[] = [
    {
      id: 'sup-1',
      name: 'Ferretería Global',
      contactName: 'Carlos Gómez',
      identificationNumber: 'RFC-123',
      email: 'contacto@global.com',
      phone: '2222-3333',
      address: 'Central',
      isActive: true,
      createdAtUtc: '2026-10-01T00:00:00Z'
    }
  ];

  beforeEach(async () => {
    mockInventoryService = {
      getByProductIdentifier: vi.fn(),
      getMovementsByProduct: vi.fn().mockReturnValue(of([])),
      recordEntry: vi.fn(),
      recordExit: vi.fn(),
      recordAdjustment: vi.fn(),
      getStatusReport: vi.fn().mockReturnValue(of(initialStockReport)),
      getMovementReport: vi.fn().mockReturnValue(of(initialMovementReport))
    };

    mockCategoryService = {
      list: vi.fn().mockReturnValue(of(initialCategories))
    };

    mockSupplierService = {
      list: vi.fn().mockReturnValue(of(initialSuppliers))
    };

    mockAuthService = {
      isAdmin: vi.fn().mockReturnValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [InventoryComponent],
      providers: [
        { provide: InventoryService, useValue: mockInventoryService },
        { provide: CategoryService, useValue: mockCategoryService },
        { provide: SupplierService, useValue: mockSupplierService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create component and load stock report and catalog dependencies on init', () => {
    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(mockInventoryService.getStatusReport).toHaveBeenCalled();
    expect(mockCategoryService.list).toHaveBeenCalled();
    expect(mockSupplierService.list).toHaveBeenCalled();

    expect(component.stockReport()).toEqual(initialStockReport);
    expect(component.filteredStockItems().length).toBe(2);

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('PROD-01');
    expect(rows[0].textContent).toContain('Taladro Percutor');
    expect(rows[0].textContent).toContain('55');
    expect(rows[0].textContent).toContain('Disponible');

    expect(rows[1].textContent).toContain('PROD-02');
    expect(rows[1].textContent).toContain('Agotado');
  });

  it('should filter stock items by search term', () => {
    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;

    // Search by product code
    component.stockSearchTerm.set('PROD-02');
    expect(component.filteredStockItems().length).toBe(1);
    expect(component.filteredStockItems()[0].productIdentifier).toBe('PROD-02');

    // Search by product name
    component.stockSearchTerm.set('Taladro');
    expect(component.filteredStockItems().length).toBe(1);
    expect(component.filteredStockItems()[0].productName).toBe('Taladro Percutor');

    // Clear search
    component.stockSearchTerm.set('');
    expect(component.filteredStockItems().length).toBe(2);
  });

  it('should trigger reload when changing category filter or inStockOnly checkbox', () => {
    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;

    component.onCategoryFilterChange('cat-1');
    expect(mockInventoryService.getStatusReport).toHaveBeenCalledWith({
      categoryId: 'cat-1',
      inStockOnly: null
    });

    component.onInStockOnlyChange(true);
    expect(mockInventoryService.getStatusReport).toHaveBeenCalledWith({
      categoryId: 'cat-1',
      inStockOnly: true
    });
  });

  it('should switch to movements tab and load movements report', () => {
    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.setTab('movements');
    fixture.detectChanges();

    expect(component.activeTab()).toBe('movements');
    expect(mockInventoryService.getMovementReport).toHaveBeenCalled();
    expect(component.movementReport()).toEqual(initialMovementReport);

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('PROD-01');
    expect(rows[0].textContent).toContain('Entrada');
    expect(rows[0].textContent).toContain('+60');
    expect(rows[0].textContent).toContain('Ferretería Global');
  });

  it('should open single product movements modal and display history', () => {
    const singleProductMovements: InventoryMovementResponse[] = [
      {
        id: 'mv-p1',
        productIdentifier: 'PROD-01',
        movementType: 'Adjustment',
        quantity: -2,
        reference: 'Merma transporte',
        occurredAtUtc: '2026-10-18T10:00:00Z'
      }
    ];
    mockInventoryService.getMovementsByProduct.mockReturnValue(of(singleProductMovements));

    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.viewProductMovements('PROD-01');
    fixture.detectChanges();

    expect(component.showProductMovementsModal()).toBe(true);
    expect(component.selectedProductIdentifier()).toBe('PROD-01');
    expect(mockInventoryService.getMovementsByProduct).toHaveBeenCalledWith('PROD-01');
    expect(component.selectedProductMovements()).toEqual(singleProductMovements);

    component.closeProductMovementsModal();
    expect(component.showProductMovementsModal()).toBe(false);
  });

  it('should open operation modal and validate form inputs', () => {
    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openOperationModal('entry', 'PROD-01');
    fixture.detectChanges();

    expect(component.showOperationModal()).toBe(true);
    expect(component.operationForm.value.productIdentifier).toBe('PROD-01');

    // Required fields: reference is empty
    expect(component.operationForm.valid).toBe(false);
    expect(component.operationForm.get('reference')?.valid).toBe(false);

    component.operationForm.patchValue({ reference: 'Doc Ref' });
    expect(component.operationForm.valid).toBe(true);
  });

  it('should record stock entry successfully and refresh data', () => {
    const entryResponse: InventoryResponse = { productIdentifier: 'PROD-01', quantity: 65 };
    mockInventoryService.recordEntry.mockReturnValue(of(entryResponse));

    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openOperationModal('entry');
    component.operationForm.setValue({
      productIdentifier: 'PROD-01',
      quantity: 10,
      adjustmentDirection: 'increase',
      reference: 'Guía Remisión 1024',
      supplierId: 'sup-1'
    });

    component.submitOperation();

    expect(mockInventoryService.recordEntry).toHaveBeenCalledWith({
      productIdentifier: 'PROD-01',
      quantity: 10,
      reference: 'Guía Remisión 1024',
      supplierId: 'sup-1'
    });
    expect(component.showOperationModal()).toBe(false);
    expect(component.successMessage()).toContain('Entrada registrada con éxito');
    expect(mockInventoryService.getStatusReport).toHaveBeenCalledTimes(2); // Initial + after success
  });

  it('should record stock exit successfully', () => {
    const exitResponse: InventoryResponse = { productIdentifier: 'PROD-01', quantity: 50 };
    mockInventoryService.recordExit.mockReturnValue(of(exitResponse));

    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openOperationModal('exit');
    component.operationForm.setValue({
      productIdentifier: 'PROD-01',
      quantity: 5,
      adjustmentDirection: 'decrease',
      reference: 'Despacho interno taller',
      supplierId: ''
    });

    component.submitOperation();

    expect(mockInventoryService.recordExit).toHaveBeenCalledWith({
      productIdentifier: 'PROD-01',
      quantity: 5,
      reference: 'Despacho interno taller'
    });
    expect(component.successMessage()).toContain('Salida registrada con éxito');
  });

  it('should record stock adjustment with positive and negative delta', () => {
    const adjResponse: InventoryResponse = { productIdentifier: 'PROD-01', quantity: 45 };
    mockInventoryService.recordAdjustment.mockReturnValue(of(adjResponse));

    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;

    // Negative adjustment
    component.openOperationModal('adjustment');
    component.operationForm.setValue({
      productIdentifier: 'PROD-01',
      quantity: 5,
      adjustmentDirection: 'decrease',
      reference: 'Merma inventario físico',
      supplierId: ''
    });

    component.submitOperation();

    expect(mockInventoryService.recordAdjustment).toHaveBeenCalledWith({
      productIdentifier: 'PROD-01',
      quantityDelta: -5,
      reason: 'Merma inventario físico'
    });
    expect(component.successMessage()).toContain('Ajuste aplicado con éxito');

    // Positive adjustment
    component.openOperationModal('adjustment');
    component.operationForm.setValue({
      productIdentifier: 'PROD-01',
      quantity: 3,
      adjustmentDirection: 'increase',
      reference: 'Sobrante encontrado',
      supplierId: ''
    });

    component.submitOperation();

    expect(mockInventoryService.recordAdjustment).toHaveBeenCalledWith({
      productIdentifier: 'PROD-01',
      quantityDelta: 3,
      reason: 'Sobrante encontrado'
    });
  });

  it('should handle API conflict error when stock is insufficient', () => {
    mockInventoryService.recordExit.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            statusText: 'Conflict',
            error: {
              type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
              title: 'Insufficient stock',
              status: 409,
              detail: 'Cannot reduce stock below zero. Current stock is 5, requested exit is 10.'
            }
          })
      )
    );

    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.openOperationModal('exit');
    component.operationForm.setValue({
      productIdentifier: 'PROD-01',
      quantity: 10,
      adjustmentDirection: 'decrease',
      reference: 'Despacho grande',
      supplierId: ''
    });

    component.submitOperation();

    expect(component.submittingOperation()).toBe(false);
    expect(component.showOperationModal()).toBe(true);
    expect(component.operationError()).toContain('Cannot reduce stock below zero');
  });

  it('should hide mutation buttons when user is not admin', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(InventoryComponent);
    fixture.detectChanges();

    // "Nueva Operación" button should not be present
    const newOpBtn = fixture.nativeElement.querySelector('.page-actions button');
    expect(newOpBtn).toBeNull();
  });
});
