import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { SalesComponent } from './sales.component';
import {
  AuthService,
  CustomerResponse,
  CustomerService,
  ProductResponse,
  ProductService,
  SaleResponse,
  SaleService,
  SalesSummaryReportResponse
} from '@core';

describe('SalesComponent', () => {
  let mockSaleService: {
    getById: ReturnType<typeof vi.fn>;
    createDraft: ReturnType<typeof vi.fn>;
    updateCustomer: ReturnType<typeof vi.fn>;
    addDetail: ReturnType<typeof vi.fn>;
    updateDetail: ReturnType<typeof vi.fn>;
    removeDetail: ReturnType<typeof vi.fn>;
    confirm: ReturnType<typeof vi.fn>;
    listSummaryReport: ReturnType<typeof vi.fn>;
  };
  let mockCustomerService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockProductService: {
    list: ReturnType<typeof vi.fn>;
  };
  let mockAuthService: {
    isAdmin: ReturnType<typeof vi.fn>;
  };

  const initialCustomers: CustomerResponse[] = [
    {
      id: 'cust-1',
      name: 'Distribuidora Central S.A.',
      identificationNumber: 'RFC-1010',
      email: 'ventas@central.com',
      phone: '2222-1111',
      address: 'San José',
      isActive: true,
      createdAtUtc: '2026-10-01T00:00:00Z'
    }
  ];

  const initialProducts: ProductResponse[] = [
    {
      identifier: 'PROD-01',
      name: 'Taladro Percutor',
      categoryId: 'cat-1',
      categoryName: 'Herramientas',
      isActive: true
    }
  ];

  const mockDraftSale: SaleResponse = {
    id: 'sale-draft-1',
    customerReference: 'Consumidor Final',
    userId: 'user-admin',
    status: 'Draft',
    subtotal: 0.0,
    taxAmount: 0.0,
    total: 0.0,
    createdAtUtc: '2026-10-22T10:00:00Z',
    confirmedAtUtc: null,
    details: [],
    customerId: null
  };

  const mockDraftSaleWithDetail: SaleResponse = {
    ...mockDraftSale,
    subtotal: 50.0,
    taxAmount: 8.0,
    total: 58.0,
    details: [
      {
        id: 'det-1',
        saleId: 'sale-draft-1',
        productIdentifier: 'PROD-01',
        quantity: 1,
        unitPrice: 50.0,
        taxRate: 0.16,
        subtotal: 50.0,
        taxAmount: 8.0,
        total: 58.0
      }
    ]
  };

  const mockConfirmedSale: SaleResponse = {
    ...mockDraftSaleWithDetail,
    status: 'Confirmed',
    confirmedAtUtc: '2026-10-22T10:15:00Z'
  };

  const mockHistoryReport: SalesSummaryReportResponse = {
    fromUtc: '2026-10-01T00:00:00.000Z',
    toUtc: '2026-10-22T23:59:59.999Z',
    totalTransactions: 1,
    confirmedTransactions: 1,
    draftTransactions: 0,
    cancelledTransactions: 0,
    totalSubtotal: 50.0,
    totalTax: 8.0,
    totalRevenue: 58.0,
    sales: [
      {
        id: 'sale-draft-1',
        createdAtUtc: '2026-10-22T10:00:00Z',
        confirmedAtUtc: '2026-10-22T10:15:00Z',
        status: 'Confirmed',
        customerId: null,
        customerReference: 'Consumidor Final',
        userId: 'user-admin',
        totalUnits: 1,
        lineCount: 1,
        subtotal: 50.0,
        taxAmount: 8.0,
        total: 58.0
      }
    ]
  };

  beforeEach(async () => {
    mockSaleService = {
      getById: vi.fn().mockReturnValue(of(mockDraftSale)),
      createDraft: vi.fn().mockReturnValue(of(mockDraftSale)),
      updateCustomer: vi.fn().mockReturnValue(of(mockDraftSale)),
      addDetail: vi.fn().mockReturnValue(of(mockDraftSaleWithDetail)),
      updateDetail: vi.fn().mockReturnValue(of(mockDraftSaleWithDetail)),
      removeDetail: vi.fn().mockReturnValue(of(mockDraftSale)),
      confirm: vi.fn().mockReturnValue(of(mockConfirmedSale)),
      listSummaryReport: vi.fn().mockReturnValue(of(mockHistoryReport))
    };

    mockCustomerService = {
      list: vi.fn().mockReturnValue(of(initialCustomers))
    };

    mockProductService = {
      list: vi.fn().mockReturnValue(of(initialProducts))
    };

    mockAuthService = {
      isAdmin: vi.fn().mockReturnValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [SalesComponent],
      providers: [
        { provide: SaleService, useValue: mockSaleService },
        { provide: CustomerService, useValue: mockCustomerService },
        { provide: ProductService, useValue: mockProductService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create component and load customer and product catalogs on init', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(mockCustomerService.list).toHaveBeenCalled();
    expect(mockProductService.list).toHaveBeenCalled();
    expect(component.customers().length).toBe(1);
    expect(component.products().length).toBe(1);
  });

  it('should show empty/idle state when no active sale exists', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const emptyTitle = fixture.nativeElement.querySelector('.state-title');
    expect(emptyTitle?.textContent).toContain('No hay una venta en curso');
  });

  it('should open modal and create draft sale for walk-in customer', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.startNewSaleModal();
    fixture.detectChanges();

    expect(component.showNewSaleModal()).toBe(true);
    expect(component.newSaleForm.valid).toBe(true);

    component.onCreateDraftSale();

    expect(mockSaleService.createDraft).toHaveBeenCalledWith({
      customerId: null,
      customerReference: 'Consumidor Final'
    });
    expect(component.showNewSaleModal()).toBe(false);
    expect(component.currentSale()).toEqual(mockDraftSale);
  });

  it('should create draft sale for registered customer', () => {
    const registeredSale: SaleResponse = {
      ...mockDraftSale,
      customerId: 'cust-1',
      customerReference: 'Distribuidora Central S.A.'
    };
    mockSaleService.createDraft.mockReturnValue(of(registeredSale));

    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.startNewSaleModal();
    component.newSaleForm.patchValue({ customerType: 'registered', customerId: 'cust-1' });
    component.onCustomerTypeChange();

    component.onCreateDraftSale();

    expect(mockSaleService.createDraft).toHaveBeenCalledWith({
      customerId: 'cust-1',
      customerReference: null
    });
    expect(component.currentSale()?.customerReference).toBe('Distribuidora Central S.A.');
  });

  it('should add detail line to draft sale and update server totals', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.currentSale.set(mockDraftSale);
    fixture.detectChanges();

    component.detailForm.setValue({
      productIdentifier: 'PROD-01',
      quantity: 1,
      unitPrice: 50.0,
      taxRate: 0.16
    });

    component.onAddDetail();

    expect(mockSaleService.addDetail).toHaveBeenCalledWith('sale-draft-1', {
      productIdentifier: 'PROD-01',
      quantity: 1,
      unitPrice: 50.0,
      taxRate: 0.16
    });
    expect(component.currentSale()).toEqual(mockDraftSaleWithDetail);
    expect(component.currentSale()?.total).toBe(58.0);
  });

  it('should remove detail line from draft sale', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.currentSale.set(mockDraftSaleWithDetail);
    fixture.detectChanges();

    component.onRemoveDetail('det-1');

    expect(mockSaleService.removeDetail).toHaveBeenCalledWith('sale-draft-1', 'det-1');
    expect(component.currentSale()).toEqual(mockDraftSale);
  });

  it('should update customer on draft sale', () => {
    const updatedCustomerSale: SaleResponse = {
      ...mockDraftSale,
      customerReference: 'Cliente Especial'
    };
    mockSaleService.updateCustomer.mockReturnValue(of(updatedCustomerSale));

    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.currentSale.set(mockDraftSale);
    component.openUpdateCustomerModal();
    fixture.detectChanges();

    expect(component.showUpdateCustomerModal()).toBe(true);

    component.updateCustomerForm.setValue({
      customerType: 'walkin',
      customerId: '',
      customerReference: 'Cliente Especial'
    });

    component.onUpdateCustomerSubmit();

    expect(mockSaleService.updateCustomer).toHaveBeenCalledWith('sale-draft-1', {
      customerId: null,
      customerReference: 'Cliente Especial'
    });
    expect(component.showUpdateCustomerModal()).toBe(false);
    expect(component.currentSale()?.customerReference).toBe('Cliente Especial');
  });

  it('should confirm sale and transition view to confirmed receipt', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.currentSale.set(mockDraftSaleWithDetail);
    fixture.detectChanges();

    component.onPromptConfirmSale();
    expect(component.showConfirmPromptModal()).toBe(true);

    component.onExecuteConfirmSale();

    expect(mockSaleService.confirm).toHaveBeenCalledWith('sale-draft-1');
    expect(component.showConfirmPromptModal()).toBe(false);
    expect(component.currentSale()?.status).toBe('Confirmed');
    expect(component.successMessage()).toContain('confirmada con éxito');

    fixture.detectChanges();
    const receiptTitle = fixture.nativeElement.querySelector('.receipt-title');
    expect(receiptTitle?.textContent).toContain('COMPROBANTE DE VENTA');
  });

  it('should handle insufficient stock conflict during confirmation', () => {
    mockSaleService.confirm.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            statusText: 'Conflict',
            error: {
              type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
              title: 'Insufficient stock',
              status: 409,
              detail: "Insufficient stock for product 'PROD-01'. Available: 0, Requested: 1."
            }
          })
      )
    );

    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.currentSale.set(mockDraftSaleWithDetail);

    component.onExecuteConfirmSale();

    expect(component.confirmingSale()).toBe(false);
    expect(component.showConfirmPromptModal()).toBe(false);
    expect(component.errorMessage()).toContain("Insufficient stock for product 'PROD-01'");
    // Sale remains draft
    expect(component.currentSale()?.status).toBe('Draft');
  });

  it('should lookup existing sale by ID and load into editor', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.setTab('lookup');
    component.lookupSaleId.set('sale-draft-1');

    component.onExecuteLookup();

    expect(mockSaleService.getById).toHaveBeenCalledWith('sale-draft-1');
    expect(component.currentSale()).toEqual(mockDraftSale);
    expect(component.activeTab()).toBe('editor');
  });

  it('should load and render sales history for admin users', () => {
    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.setTab('history');
    fixture.detectChanges();

    expect(mockSaleService.listSummaryReport).toHaveBeenCalled();
    expect(component.historyReport()).toEqual(mockHistoryReport);

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('Consumidor Final');
    expect(rows[0].textContent).toContain('Confirmada');
    expect(rows[0].textContent).toContain('$58.00');
  });

  it('should display history restriction notice when user is not admin', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.setTab('history');
    fixture.detectChanges();

    expect(mockSaleService.listSummaryReport).not.toHaveBeenCalled();
    const noticeTitle = fixture.nativeElement.querySelector('.state-title');
    expect(noticeTitle?.textContent).toContain('Historial Consolidado Restringido');
  });

  it('should hide mutation buttons when user is not admin', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(SalesComponent);
    fixture.detectChanges();

    // "Nueva Venta" header button should be hidden
    const newSaleBtn = fixture.nativeElement.querySelector('.page-actions button');
    expect(newSaleBtn).toBeNull();
  });
});
