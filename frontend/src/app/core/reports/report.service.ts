import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import { SaleService } from '../sales/sale.service';
import { InventoryService } from '../inventory/inventory.service';
import {
  InventoryMovementReportQuery,
  InventoryMovementReportResponse,
  InventoryStatusQuery,
  InventoryStatusReportResponse,
  ProductSalesReportQuery,
  ProductSalesReportResponse,
  SalesSummaryReportQuery,
  SalesSummaryReportResponse
} from './report.models';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly saleService = inject(SaleService);
  private readonly inventoryService = inject(InventoryService);
  private readonly reportsBaseUrl = `${environment.apiUrl}/reports`;

  /**
   * Retrieves the sales summary and transaction history report via GET /api/reports/sales-summary.
   * Requires Admin role. Reuses SaleService.listSummaryReport.
   */
  public getSalesSummary(query: SalesSummaryReportQuery): Observable<SalesSummaryReportResponse> {
    return this.saleService.listSummaryReport(query);
  }

  /**
   * Retrieves product sales performance via GET /api/reports/product-sales.
   * Requires Admin role.
   * fromUtc and toUtc are mandatory query parameters in the backend.
   */
  public getProductSales(query: ProductSalesReportQuery): Observable<ProductSalesReportResponse> {
    let params = new HttpParams()
      .set('fromUtc', query.fromUtc)
      .set('toUtc', query.toUtc);

    if (query.productIdentifier && query.productIdentifier.trim().length > 0) {
      params = params.set('productIdentifier', query.productIdentifier.trim());
    }

    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }

    return this.http.get<ProductSalesReportResponse>(`${this.reportsBaseUrl}/product-sales`, { params });
  }

  /**
   * Retrieves the catalog-wide inventory stock status report via GET /api/reports/inventory-status.
   * Accessible to all authenticated users. Reuses InventoryService.getStatusReport.
   */
  public getInventoryStatus(query?: InventoryStatusQuery): Observable<InventoryStatusReportResponse> {
    return this.inventoryService.getStatusReport(query);
  }

  /**
   * Retrieves the inventory movement traceability report via GET /api/reports/inventory-movements.
   * Accessible to all authenticated users. Reuses InventoryService.getMovementReport.
   * fromUtc and toUtc are mandatory query parameters in the backend.
   */
  public getInventoryMovements(query: InventoryMovementReportQuery): Observable<InventoryMovementReportResponse> {
    return this.inventoryService.getMovementReport(query);
  }
}
