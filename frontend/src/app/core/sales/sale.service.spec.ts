import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SaleService } from './sale.service';
import {
  AddSaleDetailRequest,
  CreateSaleRequest,
  SaleResponse,
  SalesSummaryReportQuery,
  SalesSummaryReportResponse,
  UpdateSaleCustomerRequest,
  UpdateSaleDetailRequest
} from './sales.models';

describe('SaleService', () => {
  let service: SaleService;
  let httpTesting: HttpTestingController;

  const mockSale: SaleResponse = {
    id: '11111111-1111-1111-1111-111111111111',
    customerReference: 'Cliente Prueba S.A.',
    userId: 'user-001',
    status: 'Draft',
    subtotal: 100.0,
    taxAmount: 16.0,
    total: 116.0,
    createdAtUtc: '2026-10-22T10:00:00Z',
    confirmedAtUtc: null,
    details: [
      {
        id: 'det-1',
        saleId: '11111111-1111-1111-1111-111111111111',
        productIdentifier: 'PROD-01',
        quantity: 2,
        unitPrice: 50.0,
        taxRate: 0.16,
        subtotal: 100.0,
        taxAmount: 16.0,
        total: 116.0
      }
    ],
    customerId: 'cust-01'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SaleService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(SaleService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve a sale by ID via GET /api/sales/{id}', () => {
    let result: SaleResponse | undefined;
    service.getById('11111111-1111-1111-1111-111111111111').subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/sales/11111111-1111-1111-1111-111111111111');
    expect(req.request.method).toBe('GET');
    req.flush(mockSale);

    expect(result).toEqual(mockSale);
  });

  it('should create a draft sale via POST /api/sales', () => {
    const payload: CreateSaleRequest = {
      customerId: 'cust-01',
      customerReference: null
    };

    let result: SaleResponse | undefined;
    service.createDraft(payload).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/sales');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(mockSale);

    expect(result).toEqual(mockSale);
  });

  it('should update customer via PUT /api/sales/{id}/customer', () => {
    const payload: UpdateSaleCustomerRequest = {
      customerId: null,
      customerReference: 'Nuevo Mostrador'
    };

    let result: SaleResponse | undefined;
    service.updateCustomer('11111111-1111-1111-1111-111111111111', payload).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/sales/11111111-1111-1111-1111-111111111111/customer');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ ...mockSale, customerReference: 'Nuevo Mostrador', customerId: null });

    expect(result?.customerReference).toBe('Nuevo Mostrador');
  });

  it('should add detail line via POST /api/sales/{id}/details', () => {
    const payload: AddSaleDetailRequest = {
      productIdentifier: 'PROD-02',
      quantity: 3,
      unitPrice: 20.0,
      taxRate: 0.16
    };

    let result: SaleResponse | undefined;
    service.addDetail('11111111-1111-1111-1111-111111111111', payload).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/sales/11111111-1111-1111-1111-111111111111/details');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(mockSale);

    expect(result).toEqual(mockSale);
  });

  it('should update detail line via PUT /api/sales/{id}/details/{detailId}', () => {
    const payload: UpdateSaleDetailRequest = {
      quantity: 5,
      unitPrice: 45.0,
      taxRate: 0.16
    };

    let result: SaleResponse | undefined;
    service.updateDetail('11111111-1111-1111-1111-111111111111', 'det-1', payload).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/sales/11111111-1111-1111-1111-111111111111/details/det-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush(mockSale);

    expect(result).toEqual(mockSale);
  });

  it('should remove detail line via DELETE /api/sales/{id}/details/{detailId}', () => {
    let result: SaleResponse | undefined;
    service.removeDetail('11111111-1111-1111-1111-111111111111', 'det-1').subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/sales/11111111-1111-1111-1111-111111111111/details/det-1');
    expect(req.request.method).toBe('DELETE');
    req.flush({ ...mockSale, details: [] });

    expect(result?.details.length).toBe(0);
  });

  it('should confirm sale via POST /api/sales/{id}/confirm', () => {
    const confirmedSale: SaleResponse = {
      ...mockSale,
      status: 'Confirmed',
      confirmedAtUtc: '2026-10-22T10:30:00Z'
    };

    let result: SaleResponse | undefined;
    service.confirm('11111111-1111-1111-1111-111111111111').subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/sales/11111111-1111-1111-1111-111111111111/confirm');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(confirmedSale);

    expect(result?.status).toBe('Confirmed');
    expect(result?.confirmedAtUtc).toBe('2026-10-22T10:30:00Z');
  });

  it('should retrieve sales summary report via GET /api/reports/sales-summary', () => {
    const query: SalesSummaryReportQuery = {
      fromUtc: '2026-10-01T00:00:00.000Z',
      toUtc: '2026-10-22T23:59:59.999Z',
      customerId: 'cust-01',
      status: 'Confirmed'
    };

    const mockReport: SalesSummaryReportResponse = {
      fromUtc: query.fromUtc,
      toUtc: query.toUtc,
      totalTransactions: 1,
      confirmedTransactions: 1,
      draftTransactions: 0,
      cancelledTransactions: 0,
      totalSubtotal: 100.0,
      totalTax: 16.0,
      totalRevenue: 116.0,
      sales: [
        {
          id: '11111111-1111-1111-1111-111111111111',
          createdAtUtc: '2026-10-22T10:00:00Z',
          confirmedAtUtc: '2026-10-22T10:30:00Z',
          status: 'Confirmed',
          customerId: 'cust-01',
          customerReference: 'Cliente Prueba S.A.',
          userId: 'user-001',
          totalUnits: 2,
          lineCount: 1,
          subtotal: 100.0,
          taxAmount: 16.0,
          total: 116.0
        }
      ]
    };

    let result: SalesSummaryReportResponse | undefined;
    service.listSummaryReport(query).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne(r =>
      r.url === '/api/reports/sales-summary' &&
      r.params.get('fromUtc') === query.fromUtc &&
      r.params.get('toUtc') === query.toUtc &&
      r.params.get('customerId') === 'cust-01' &&
      r.params.get('status') === 'Confirmed'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockReport);

    expect(result).toEqual(mockReport);
  });
});
