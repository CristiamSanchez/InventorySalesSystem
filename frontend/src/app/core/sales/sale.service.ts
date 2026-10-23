import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import {
  AddSaleDetailRequest,
  CreateSaleRequest,
  SaleResponse,
  SalesSummaryReportQuery,
  SalesSummaryReportResponse,
  UpdateSaleCustomerRequest,
  UpdateSaleDetailRequest
} from './sales.models';

@Injectable({
  providedIn: 'root'
})
export class SaleService {
  private readonly http = inject(HttpClient);
  private readonly salesBaseUrl = `${environment.apiUrl}/sales`;
  private readonly reportsBaseUrl = `${environment.apiUrl}/reports`;

  /**
   * Retrieves a single sale by ID from `GET /api/sales/{id}`. Accessible to all authenticated users.
   */
  public getById(id: string): Observable<SaleResponse> {
    return this.http.get<SaleResponse>(`${this.salesBaseUrl}/${encodeURIComponent(id)}`);
  }

  /**
   * Creates a draft sale via `POST /api/sales`. Requires Admin role.
   */
  public createDraft(request: CreateSaleRequest): Observable<SaleResponse> {
    return this.http.post<SaleResponse>(this.salesBaseUrl, request);
  }

  /**
   * Updates or clears the customer association on a draft sale via `PUT /api/sales/{id}/customer`. Requires Admin role.
   */
  public updateCustomer(id: string, request: UpdateSaleCustomerRequest): Observable<SaleResponse> {
    return this.http.put<SaleResponse>(`${this.salesBaseUrl}/${encodeURIComponent(id)}/customer`, request);
  }

  /**
   * Adds a detail line to a draft sale via `POST /api/sales/{id}/details`. Requires Admin role.
   */
  public addDetail(id: string, request: AddSaleDetailRequest): Observable<SaleResponse> {
    return this.http.post<SaleResponse>(`${this.salesBaseUrl}/${encodeURIComponent(id)}/details`, request);
  }

  /**
   * Updates a detail line on a draft sale via `PUT /api/sales/{id}/details/{detailIdOrProductIdentifier}`. Requires Admin role.
   */
  public updateDetail(
    id: string,
    detailIdOrProductIdentifier: string,
    request: UpdateSaleDetailRequest
  ): Observable<SaleResponse> {
    return this.http.put<SaleResponse>(
      `${this.salesBaseUrl}/${encodeURIComponent(id)}/details/${encodeURIComponent(detailIdOrProductIdentifier)}`,
      request
    );
  }

  /**
   * Removes a detail line from a draft sale via `DELETE /api/sales/{id}/details/{detailIdOrProductIdentifier}`. Requires Admin role.
   */
  public removeDetail(id: string, detailIdOrProductIdentifier: string): Observable<SaleResponse> {
    return this.http.delete<SaleResponse>(
      `${this.salesBaseUrl}/${encodeURIComponent(id)}/details/${encodeURIComponent(detailIdOrProductIdentifier)}`
    );
  }

  /**
   * Atomically confirms a draft sale and deducts inventory stock via `POST /api/sales/{id}/confirm`. Requires Admin role.
   */
  public confirm(id: string): Observable<SaleResponse> {
    return this.http.post<SaleResponse>(`${this.salesBaseUrl}/${encodeURIComponent(id)}/confirm`, {});
  }

  /**
   * Retrieves the sales summary report and transaction history via `GET /api/reports/sales-summary`. Requires Admin role.
   * `fromUtc` and `toUtc` are required parameters.
   */
  public listSummaryReport(query: SalesSummaryReportQuery): Observable<SalesSummaryReportResponse> {
    let params = new HttpParams()
      .set('fromUtc', query.fromUtc)
      .set('toUtc', query.toUtc);

    if (query.customerId) {
      params = params.set('customerId', query.customerId);
    }
    if (query.userId) {
      params = params.set('userId', query.userId);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }

    return this.http.get<SalesSummaryReportResponse>(`${this.reportsBaseUrl}/sales-summary`, { params });
  }
}
