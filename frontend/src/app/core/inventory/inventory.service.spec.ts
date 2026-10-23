import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { InventoryService } from './inventory.service';
import {
  AdjustStockRequest,
  InventoryMovementReportQuery,
  InventoryMovementReportResponse,
  InventoryMovementResponse,
  InventoryResponse,
  InventoryStatusReportResponse,
  RecordStockEntryRequest,
  RecordStockExitRequest
} from './inventory.models';

describe('InventoryService', () => {
  let service: InventoryService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [InventoryService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(InventoryService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve single product inventory via GET /api/inventory/{productIdentifier}', () => {
    const mockResponse: InventoryResponse = {
      productIdentifier: 'PROD-01',
      quantity: 42
    };

    let result: InventoryResponse | undefined;
    service.getByProductIdentifier('PROD-01').subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/inventory/PROD-01');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('should retrieve product movements via GET /api/inventory/{productIdentifier}/movements', () => {
    const mockMovements: InventoryMovementResponse[] = [
      {
        id: '11111111-1111-1111-1111-111111111111',
        productIdentifier: 'PROD-01',
        movementType: 'Entry',
        quantity: 50,
        reference: 'Delivery 001',
        occurredAtUtc: '2026-10-22T10:00:00Z',
        supplierId: '22222222-2222-2222-2222-222222222222',
        supplierName: 'Acme Corp'
      }
    ];

    let result: InventoryMovementResponse[] = [];
    service.getMovementsByProduct('PROD-01').subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/inventory/PROD-01/movements');
    expect(req.request.method).toBe('GET');
    req.flush(mockMovements);

    expect(result.length).toBe(1);
    expect(result[0].reference).toBe('Delivery 001');
  });

  it('should record stock entry via POST /api/inventory/entries', () => {
    const payload: RecordStockEntryRequest = {
      productIdentifier: 'PROD-01',
      quantity: 20,
      reference: 'PO-999',
      supplierId: '33333333-3333-3333-3333-333333333333'
    };
    const mockResponse: InventoryResponse = {
      productIdentifier: 'PROD-01',
      quantity: 62
    };

    let result: InventoryResponse | undefined;
    service.recordEntry(payload).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/inventory/entries');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('should record stock exit via POST /api/inventory/exits', () => {
    const payload: RecordStockExitRequest = {
      productIdentifier: 'PROD-01',
      quantity: 5,
      reference: 'Transfer Out'
    };
    const mockResponse: InventoryResponse = {
      productIdentifier: 'PROD-01',
      quantity: 57
    };

    let result: InventoryResponse | undefined;
    service.recordExit(payload).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/inventory/exits');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('should record stock adjustment with signed delta via POST /api/inventory/adjustments', () => {
    const payload: AdjustStockRequest = {
      productIdentifier: 'PROD-01',
      quantityDelta: -3,
      reason: 'Physical inventory recount loss'
    };
    const mockResponse: InventoryResponse = {
      productIdentifier: 'PROD-01',
      quantity: 54
    };

    let result: InventoryResponse | undefined;
    service.recordAdjustment(payload).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne('/api/inventory/adjustments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('should retrieve inventory status report with query parameters via GET /api/reports/inventory-status', () => {
    const mockReport: InventoryStatusReportResponse = {
      totalProducts: 1,
      totalUnitsOnHand: 54,
      inStockProductsCount: 1,
      outOfStockProductsCount: 0,
      items: [
        {
          productIdentifier: 'PROD-01',
          productName: 'Widget Alpha',
          categoryId: '44444444-4444-4444-4444-444444444444',
          categoryName: 'Hardware',
          quantity: 54,
          hasInventoryRecord: true,
          isActive: true,
          stockStatus: 'InStock'
        }
      ]
    };

    let result: InventoryStatusReportResponse | undefined;
    service
      .getStatusReport({ categoryId: '44444444-4444-4444-4444-444444444444', inStockOnly: true })
      .subscribe(res => {
        result = res;
      });

    const req = httpTesting.expectOne(r =>
      r.url === '/api/reports/inventory-status' &&
      r.params.get('categoryId') === '44444444-4444-4444-4444-444444444444' &&
      r.params.get('inStockOnly') === 'true'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockReport);

    expect(result).toEqual(mockReport);
  });

  it('should retrieve inventory movement traceability report via GET /api/reports/inventory-movements', () => {
    const query: InventoryMovementReportQuery = {
      fromUtc: '2026-10-01T00:00:00.000Z',
      toUtc: '2026-10-22T23:59:59.999Z',
      productIdentifier: 'PROD-01',
      movementType: 'Entry',
      supplierId: '22222222-2222-2222-2222-222222222222'
    };

    const mockReport: InventoryMovementReportResponse = {
      fromUtc: query.fromUtc,
      toUtc: query.toUtc,
      totalMovements: 1,
      entryCount: 1,
      exitCount: 0,
      adjustmentCount: 0,
      totalUnitsIn: 50,
      totalUnitsOut: 0,
      netQuantityDelta: 50,
      movements: [
        {
          id: '55555555-5555-5555-5555-555555555555',
          occurredAtUtc: '2026-10-15T10:00:00Z',
          productIdentifier: 'PROD-01',
          movementType: 'Entry',
          quantity: 50,
          reference: 'Initial Load',
          supplierId: '22222222-2222-2222-2222-222222222222',
          supplierName: 'Acme Corp',
          userId: '66666666-6666-6666-6666-666666666666'
        }
      ]
    };

    let result: InventoryMovementReportResponse | undefined;
    service.getMovementReport(query).subscribe(res => {
      result = res;
    });

    const req = httpTesting.expectOne(r =>
      r.url === '/api/reports/inventory-movements' &&
      r.params.get('fromUtc') === query.fromUtc &&
      r.params.get('toUtc') === query.toUtc &&
      r.params.get('productIdentifier') === 'PROD-01' &&
      r.params.get('movementType') === 'Entry' &&
      r.params.get('supplierId') === '22222222-2222-2222-2222-222222222222'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockReport);

    expect(result).toEqual(mockReport);
  });
});
