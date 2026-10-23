import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import { CategoryResponse, CreateCategoryRequest, UpdateCategoryRequest } from './catalog.models';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/categories`;

  /**
   * Retrieves all categories from `GET /api/categories`.
   */
  public list(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(this.baseUrl);
  }

  /**
   * Retrieves a single category by UUID from `GET /api/categories/{id}`.
   */
  public getById(id: string): Observable<CategoryResponse> {
    return this.http.get<CategoryResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * Creates a new category via `POST /api/categories`. Requires Admin role.
   */
  public create(request: CreateCategoryRequest): Observable<CategoryResponse> {
    return this.http.post<CategoryResponse>(this.baseUrl, request);
  }

  /**
   * Updates an existing category via `PUT /api/categories/{id}`. Requires Admin role.
   */
  public update(id: string, request: UpdateCategoryRequest): Observable<CategoryResponse> {
    return this.http.put<CategoryResponse>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Deletes a category via `DELETE /api/categories/{id}`. Requires Admin role.
   * Fails with 409 Conflict if category has associated products.
   */
  public delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
