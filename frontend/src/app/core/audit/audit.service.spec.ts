import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuditService } from './audit.service';
import { AuditEventResponse, AuditQuery } from './audit.models';

describe('AuditService', () => {
  let service: AuditService;
  let httpTesting: HttpTestingController;

  const mockAuditEvent: AuditEventResponse = {
    id: '11111111-2222-3333-4444-555555555555',
    eventType: 'SaleConfirmed',
    entityName: 'Sale',
    entityId: 'sale-100',
    userId: 'user-admin',
    occurredAtUtc: '2026-10-22T12:00:00Z',
    metadataJson: '{"subtotal":100,"total":116}',
    hasUser: true
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuditService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(AuditService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should retrieve audit events list via GET /api/audit without params', () => {
    let result: AuditEventResponse[] | undefined;
    service.list().subscribe(res => (result = res));

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/audit'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);

    req.flush([mockAuditEvent]);
    expect(result).toEqual([mockAuditEvent]);
  });

  it('should pass query filter parameters to GET /api/audit', () => {
    const query: AuditQuery = {
      eventType: 'SaleConfirmed',
      entityName: 'Sale',
      entityId: 'sale-100',
      userId: 'user-admin',
      fromUtc: '2026-10-01T00:00:00.000Z',
      toUtc: '2026-10-22T23:59:59.999Z'
    };

    let result: AuditEventResponse[] | undefined;
    service.list(query).subscribe(res => (result = res));

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/audit'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('eventType')).toBe('SaleConfirmed');
    expect(req.request.params.get('entityName')).toBe('Sale');
    expect(req.request.params.get('entityId')).toBe('sale-100');
    expect(req.request.params.get('userId')).toBe('user-admin');
    expect(req.request.params.get('fromUtc')).toBe('2026-10-01T00:00:00.000Z');
    expect(req.request.params.get('toUtc')).toBe('2026-10-22T23:59:59.999Z');

    req.flush([mockAuditEvent]);
    expect(result).toEqual([mockAuditEvent]);
  });

  it('should retrieve a single audit event by ID via GET /api/audit/{id}', () => {
    let result: AuditEventResponse | undefined;
    service.getById('11111111-2222-3333-4444-555555555555').subscribe(res => (result = res));

    const req = httpTesting.expectOne(r =>
      r.url.endsWith('/api/audit/11111111-2222-3333-4444-555555555555')
    );
    expect(req.request.method).toBe('GET');

    req.flush(mockAuditEvent);
    expect(result).toEqual(mockAuditEvent);
  });
});
