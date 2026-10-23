import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import {
  CreateCustomerRequest,
  CustomerQueryFilter,
  CustomerResponse,
  UpdateCustomerRequest
} from './customer.models';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/customers`;

  /**
   * Retrieves customers with optional filters from `GET /api/customers`.
   */
  public list(filter?: CustomerQueryFilter): Observable<CustomerResponse[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.isActive !== undefined && filter.isActive !== null) {
        params = params.set('isActive', filter.isActive.toString());
      }
      if (filter.searchTerm && filter.searchTerm.trim().length > 0) {
        params = params.set('searchTerm', filter.searchTerm.trim());
      }
    }

    return this.http.get<CustomerResponse[]>(this.baseUrl, { params });
  }

  /**
   * Retrieves a single customer by ID from `GET /api/customers/{id}`.
   */
  public getById(id: string): Observable<CustomerResponse> {
    return this.http.get<CustomerResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * Creates a customer via `POST /api/customers`. Requires Admin role.
   */
  public create(request: CreateCustomerRequest): Observable<CustomerResponse> {
    return this.http.post<CustomerResponse>(this.baseUrl, request);
  }

  /**
   * Updates an existing customer's details via `PUT /api/customers/{id}`. Requires Admin role.
   */
  public update(id: string, request: UpdateCustomerRequest): Observable<CustomerResponse> {
    return this.http.put<CustomerResponse>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Activates a customer via `POST /api/customers/{id}/activate`. Requires Admin role.
   */
  public activate(id: string): Observable<CustomerResponse> {
    return this.http.post<CustomerResponse>(`${this.baseUrl}/${id}/activate`, {});
  }

  /**
   * Deactivates a customer via `POST /api/customers/{id}/deactivate`. Requires Admin role.
   */
  public deactivate(id: string): Observable<CustomerResponse> {
    return this.http.post<CustomerResponse>(`${this.baseUrl}/${id}/deactivate`, {});
  }
}
