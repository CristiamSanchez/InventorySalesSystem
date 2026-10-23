import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import {
  AdjustStockRequest,
  InventoryMovementReportQuery,
  InventoryMovementReportResponse,
  InventoryMovementResponse,
  InventoryResponse,
  InventoryStatusQuery,
  InventoryStatusReportResponse,
  RecordStockEntryRequest,
  RecordStockExitRequest
} from './inventory.models';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly inventoryBaseUrl = `${environment.apiUrl}/inventory`;
  private readonly reportsBaseUrl = `${environment.apiUrl}/reports`;

  /**
   * Retrieves the current stock level for a product from `GET /api/inventory/{productIdentifier}`.
   */
  public getByProductIdentifier(productIdentifier: string): Observable<InventoryResponse> {
    return this.http.get<InventoryResponse>(`${this.inventoryBaseUrl}/${encodeURIComponent(productIdentifier)}`);
  }

  /**
   * Retrieves the chronological movement history for a single product from `GET /api/inventory/{productIdentifier}/movements`.
   */
  public getMovementsByProduct(productIdentifier: string): Observable<InventoryMovementResponse[]> {
    return this.http.get<InventoryMovementResponse[]>(
      `${this.inventoryBaseUrl}/${encodeURIComponent(productIdentifier)}/movements`
    );
  }

  /**
   * Records a new stock entry via `POST /api/inventory/entries`. Requires Admin role.
   */
  public recordEntry(request: RecordStockEntryRequest): Observable<InventoryResponse> {
    return this.http.post<InventoryResponse>(`${this.inventoryBaseUrl}/entries`, request);
  }

  /**
   * Records a stock exit via `POST /api/inventory/exits`. Requires Admin role.
   */
  public recordExit(request: RecordStockExitRequest): Observable<InventoryResponse> {
    return this.http.post<InventoryResponse>(`${this.inventoryBaseUrl}/exits`, request);
  }

  /**
   * Records a stock adjustment (signed delta != 0) via `POST /api/inventory/adjustments`. Requires Admin role.
   */
  public recordAdjustment(request: AdjustStockRequest): Observable<InventoryResponse> {
    return this.http.post<InventoryResponse>(`${this.inventoryBaseUrl}/adjustments`, request);
  }

  /**
   * Retrieves the catalog-wide inventory stock status report from `GET /api/reports/inventory-status`.
   */
  public getStatusReport(query?: InventoryStatusQuery): Observable<InventoryStatusReportResponse> {
    let params = new HttpParams();

    if (query) {
      if (query.categoryId) {
        params = params.set('categoryId', query.categoryId);
      }
      if (query.inStockOnly !== undefined && query.inStockOnly !== null) {
        params = params.set('inStockOnly', query.inStockOnly.toString());
      }
    }

    return this.http.get<InventoryStatusReportResponse>(`${this.reportsBaseUrl}/inventory-status`, { params });
  }

  /**
   * Retrieves the inventory movement traceability report from `GET /api/reports/inventory-movements`.
   * `fromUtc` and `toUtc` are mandatory query parameters in the backend.
   */
  public getMovementReport(query: InventoryMovementReportQuery): Observable<InventoryMovementReportResponse> {
    let params = new HttpParams()
      .set('fromUtc', query.fromUtc)
      .set('toUtc', query.toUtc);

    if (query.productIdentifier && query.productIdentifier.trim().length > 0) {
      params = params.set('productIdentifier', query.productIdentifier.trim());
    }

    if (query.movementType) {
      params = params.set('movementType', query.movementType.toString());
    }

    if (query.supplierId) {
      params = params.set('supplierId', query.supplierId);
    }

    return this.http.get<InventoryMovementReportResponse>(`${this.reportsBaseUrl}/inventory-movements`, { params });
  }
}
