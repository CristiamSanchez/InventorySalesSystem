import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import {
  CreateSupplierRequest,
  SupplierQueryFilter,
  SupplierResponse,
  UpdateSupplierRequest
} from './supplier.models';

@Injectable({
  providedIn: 'root'
})
export class SupplierService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/suppliers`;

  /**
   * Retrieves suppliers with optional filters from `GET /api/suppliers`.
   */
  public list(filter?: SupplierQueryFilter): Observable<SupplierResponse[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.isActive !== undefined && filter.isActive !== null) {
        params = params.set('isActive', filter.isActive.toString());
      }
      if (filter.searchTerm && filter.searchTerm.trim().length > 0) {
        params = params.set('searchTerm', filter.searchTerm.trim());
      }
    }

    return this.http.get<SupplierResponse[]>(this.baseUrl, { params });
  }

  /**
   * Retrieves a single supplier by ID from `GET /api/suppliers/{id}`.
   */
  public getById(id: string): Observable<SupplierResponse> {
    return this.http.get<SupplierResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * Creates a supplier via `POST /api/suppliers`. Requires Admin role.
   */
  public create(request: CreateSupplierRequest): Observable<SupplierResponse> {
    return this.http.post<SupplierResponse>(this.baseUrl, request);
  }

  /**
   * Updates an existing supplier's details via `PUT /api/suppliers/{id}`. Requires Admin role.
   */
  public update(id: string, request: UpdateSupplierRequest): Observable<SupplierResponse> {
    return this.http.put<SupplierResponse>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Activates a supplier via `POST /api/suppliers/{id}/activate`. Requires Admin role.
   */
  public activate(id: string): Observable<SupplierResponse> {
    return this.http.post<SupplierResponse>(`${this.baseUrl}/${id}/activate`, {});
  }

  /**
   * Deactivates a supplier via `POST /api/suppliers/{id}/deactivate`. Requires Admin role.
   */
  public deactivate(id: string): Observable<SupplierResponse> {
    return this.http.post<SupplierResponse>(`${this.baseUrl}/${id}/deactivate`, {});
  }
}
