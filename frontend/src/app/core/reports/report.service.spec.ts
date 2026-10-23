import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ReportService } from './report.service';
import { SaleService } from '../sales/sale.service';
import { InventoryService } from '../inventory/inventory.service';
import {
  InventoryMovementReportResponse,
  InventoryStatusReportResponse,
  ProductSalesReportResponse,
  SalesSummaryReportResponse
} from './report.models';

describe('ReportService', () => {
  let service: ReportService;
  let httpTesting: HttpTestingController;

  const mockSalesSummary: SalesSummaryReportResponse = {
    fromUtc: '2026-10-01T00:00:00Z',
    toUtc: '2026-10-22T23:59:59Z',
    totalTransactions: 2,
    confirmedTransactions: 1,
    draftTransactions: 1,
    cancelledTransactions: 0,
    totalSubtotal: 100,
    totalTax: 16,
    totalRevenue: 116,
    sales: []
  };

  const mockProductSales: ProductSalesReportResponse = {
    fromUtc: '2026-10-01T00:00:00Z',
    toUtc: '2026-10-22T23:59:59Z',
    totalProductsSold: 1,
    totalUnitsSold: 5,
    totalSubtotal: 250,
    totalTax: 40,
    totalRevenue: 290,
    items: [
      {
        productIdentifier: 'PROD-A',
        productName: 'Producto A',
        categoryId: 'cat-1',
        categoryName: 'Bebidas',
        unitsSold: 5,
        subtotal: 250,
        taxAmount: 40,
        total: 290
      }
    ]
  };

  const mockInventoryStatus: InventoryStatusReportResponse = {
    totalProducts: 2,
    totalUnitsOnHand: 15,
    inStockProductsCount: 1,
    outOfStockProductsCount: 1,
    items: [
      {
        productIdentifier: 'PROD-A',
        productName: 'Producto A',
        categoryId: 'cat-1',
        categoryName: 'Bebidas',
        quantity: 15,
        hasInventoryRecord: true,
        isActive: true,
        stockStatus: 'InStock'
      }
    ]
  };

  const mockInventoryMovements: InventoryMovementReportResponse = {
    fromUtc: '2026-10-01T00:00:00Z',
    toUtc: '2026-10-22T23:59:59Z',
    totalMovements: 1,
    entryCount: 1,
    exitCount: 0,
    adjustmentCount: 0,
    totalUnitsIn: 10,
    totalUnitsOut: 0,
    netQuantityDelta: 10,
    movements: [
      {
        id: 'mov-1',
        occurredAtUtc: '2026-10-15T12:00:00Z',
        productIdentifier: 'PROD-A',
        movementType: 'Entry',
        quantity: 10,
        reference: 'Compra inicial',
        supplierId: 'sup-1',
        supplierName: 'Distribuidora Central',
        userId: 'user-1'
      }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ReportService,
        SaleService,
        InventoryService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ReportService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve sales summary report via GET /api/reports/sales-summary', () => {
    let result: SalesSummaryReportResponse | undefined;
    service
      .getSalesSummary({
        fromUtc: '2026-10-01T00:00:00Z',
        toUtc: '2026-10-22T23:59:59Z',
        customerId: 'cust-1',
        status: 'Confirmed'
      })
      .subscribe(res => (result = res));

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/reports/sales-summary'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('fromUtc')).toBe('2026-10-01T00:00:00Z');
    expect(req.request.params.get('toUtc')).toBe('2026-10-22T23:59:59Z');
    expect(req.request.params.get('customerId')).toBe('cust-1');
    expect(req.request.params.get('status')).toBe('Confirmed');

    req.flush(mockSalesSummary);
    expect(result).toEqual(mockSalesSummary);
  });

  it('should retrieve product sales performance report via GET /api/reports/product-sales', () => {
    let result: ProductSalesReportResponse | undefined;
    service
      .getProductSales({
        fromUtc: '2026-10-01T00:00:00Z',
        toUtc: '2026-10-22T23:59:59Z',
        productIdentifier: 'PROD-A',
        categoryId: 'cat-1'
      })
      .subscribe(res => (result = res));

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/reports/product-sales'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('fromUtc')).toBe('2026-10-01T00:00:00Z');
    expect(req.request.params.get('toUtc')).toBe('2026-10-22T23:59:59Z');
    expect(req.request.params.get('productIdentifier')).toBe('PROD-A');
    expect(req.request.params.get('categoryId')).toBe('cat-1');

    req.flush(mockProductSales);
    expect(result).toEqual(mockProductSales);
  });

  it('should retrieve inventory status report via GET /api/reports/inventory-status', () => {
    let result: InventoryStatusReportResponse | undefined;
    service
      .getInventoryStatus({
        categoryId: 'cat-1',
        inStockOnly: true
      })
      .subscribe(res => (result = res));

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/reports/inventory-status'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('categoryId')).toBe('cat-1');
    expect(req.request.params.get('inStockOnly')).toBe('true');

    req.flush(mockInventoryStatus);
    expect(result).toEqual(mockInventoryStatus);
  });

  it('should retrieve inventory movements traceability report via GET /api/reports/inventory-movements', () => {
    let result: InventoryMovementReportResponse | undefined;
    service
      .getInventoryMovements({
        fromUtc: '2026-10-01T00:00:00Z',
        toUtc: '2026-10-22T23:59:59Z',
        productIdentifier: 'PROD-A',
        movementType: 'Entry',
        supplierId: 'sup-1'
      })
      .subscribe(res => (result = res));

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/reports/inventory-movements'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('fromUtc')).toBe('2026-10-01T00:00:00Z');
    expect(req.request.params.get('toUtc')).toBe('2026-10-22T23:59:59Z');
    expect(req.request.params.get('productIdentifier')).toBe('PROD-A');
    expect(req.request.params.get('movementType')).toBe('Entry');
    expect(req.request.params.get('supplierId')).toBe('sup-1');

    req.flush(mockInventoryMovements);
    expect(result).toEqual(mockInventoryMovements);
  });
});
