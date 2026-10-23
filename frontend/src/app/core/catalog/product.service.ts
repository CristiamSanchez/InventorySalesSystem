import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import { CreateProductRequest, ProductQueryFilter, ProductResponse, UpdateProductRequest } from './catalog.models';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/products`;

  /**
   * Retrieves products with optional filters from `GET /api/products`.
   */
  public list(filter?: ProductQueryFilter): Observable<ProductResponse[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.categoryId) {
        params = params.set('categoryId', filter.categoryId);
      }
      if (filter.isActive !== undefined && filter.isActive !== null) {
        params = params.set('isActive', filter.isActive.toString());
      }
      if (filter.searchTerm && filter.searchTerm.trim().length > 0) {
        params = params.set('searchTerm', filter.searchTerm.trim());
      }
    }

    return this.http.get<ProductResponse[]>(this.baseUrl, { params });
  }

  /**
   * Retrieves a single product by identifier from `GET /api/products/{identifier}`.
   */
  public getByIdentifier(identifier: string): Observable<ProductResponse> {
    return this.http.get<ProductResponse>(`${this.baseUrl}/${encodeURIComponent(identifier)}`);
  }

  /**
   * Creates a product via `POST /api/products`. Requires Admin role.
   */
  public create(request: CreateProductRequest): Observable<ProductResponse> {
    return this.http.post<ProductResponse>(this.baseUrl, request);
  }

  /**
   * Updates an existing product via `PUT /api/products/{identifier}`. Requires Admin role.
   * Note: The product identifier cannot be modified.
   */
  public update(identifier: string, request: UpdateProductRequest): Observable<ProductResponse> {
    return this.http.put<ProductResponse>(`${this.baseUrl}/${encodeURIComponent(identifier)}`, request);
  }
}
