import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env';
import { AuditEventResponse, AuditQuery } from './audit.models';

@Injectable({
  providedIn: 'root'
})
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/audit`;

  /**
   * Retrieves audit events matching optional filters from GET /api/audit.
   * Requires Admin role.
   */
  public list(query?: AuditQuery): Observable<AuditEventResponse[]> {
    let params = new HttpParams();

    if (query) {
      if (query.eventType && query.eventType.trim().length > 0) {
        params = params.set('eventType', query.eventType.trim());
      }
      if (query.entityName && query.entityName.trim().length > 0) {
        params = params.set('entityName', query.entityName.trim());
      }
      if (query.entityId && query.entityId.trim().length > 0) {
        params = params.set('entityId', query.entityId.trim());
      }
      if (query.userId && query.userId.trim().length > 0) {
        params = params.set('userId', query.userId.trim());
      }
      if (query.fromUtc && query.fromUtc.trim().length > 0) {
        params = params.set('fromUtc', query.fromUtc.trim());
      }
      if (query.toUtc && query.toUtc.trim().length > 0) {
        params = params.set('toUtc', query.toUtc.trim());
      }
    }

    return this.http.get<AuditEventResponse[]>(this.baseUrl, { params });
  }

  /**
   * Retrieves a single audit event by ID from GET /api/audit/{id}.
   * Requires Admin role.
   */
  public getById(id: string): Observable<AuditEventResponse> {
    return this.http.get<AuditEventResponse>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }
}
