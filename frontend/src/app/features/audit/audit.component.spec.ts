import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { AuditComponent } from './audit.component';
import { AuditEventResponse, AuditService, AuthService } from '@core';

describe('AuditComponent', () => {
  let mockAuditService: {
    list: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
  };
  let mockAuthService: {
    isAdmin: ReturnType<typeof vi.fn>;
  };

  const mockEvents: AuditEventResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      eventType: 'SaleConfirmed',
      entityName: 'Sale',
      entityId: 'sale-100',
      userId: 'admin-user-01',
      occurredAtUtc: '2026-10-22T10:00:00Z',
      metadataJson: '{"subtotal":100,"total":116}',
      hasUser: true
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      eventType: 'InventoryMovementRecorded',
      entityName: 'InventoryMovement',
      entityId: 'mov-200',
      userId: null,
      occurredAtUtc: '2026-10-22T11:00:00Z',
      metadataJson: null,
      hasUser: false
    },
    {
      id: '33333333-3333-3333-3333-333333333333',
      eventType: 'ProductDeactivated',
      entityName: 'Product',
      entityId: 'PROD-01',
      userId: 'admin-user-01',
      occurredAtUtc: '2026-10-22T12:00:00Z',
      metadataJson: '{"reason":"Descontinuado"}',
      hasUser: true
    }
  ];

  beforeEach(() => {
    mockAuditService = {
      list: vi.fn().mockReturnValue(of(mockEvents)),
      getById: vi.fn().mockReturnValue(of(mockEvents[0]))
    };

    mockAuthService = {
      isAdmin: vi.fn().mockReturnValue(true)
    };

    TestBed.configureTestingModule({
      imports: [AuditComponent],
      providers: [
        { provide: AuditService, useValue: mockAuditService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    });
  });

  it('should create component and load audit events on init for admin user', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(mockAuditService.list).toHaveBeenCalled();
    expect(component.auditEvents().length).toBe(3);
    expect(component.loading()).toBe(false);
  });

  it('should not load audit events on init if user is not admin and display restriction card', () => {
    mockAuthService.isAdmin.mockReturnValue(false);

    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(mockAuditService.list).not.toHaveBeenCalled();
    expect(component.auditEvents().length).toBe(0);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Acceso Restringido a Administradores');
  });

  it('should pass query filters to auditService.list when loadAuditEvents is executed', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.fromDate.set('2026-10-01');
    component.toDate.set('2026-10-20');
    component.selectedEventType.set('SaleConfirmed');
    component.selectedEntityName.set('Sale');
    component.entityId.set('sale-100');
    component.userId.set('admin-user-01');

    component.loadAuditEvents();

    expect(mockAuditService.list).toHaveBeenCalledWith({
      eventType: 'SaleConfirmed',
      entityName: 'Sale',
      entityId: 'sale-100',
      userId: 'admin-user-01',
      fromUtc: '2026-10-01T00:00:00.000Z',
      toUtc: '2026-10-20T23:59:59.999Z'
    });
    expect(component.errorMessage()).toBeNull();
  });

  it('should prevent query and display error when fromDate is greater than toDate', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.fromDate.set('2026-10-25');
    component.toDate.set('2026-10-10');

    mockAuditService.list.mockClear();
    component.loadAuditEvents();

    expect(mockAuditService.list).not.toHaveBeenCalled();
    expect(component.errorMessage()).toBe('La fecha inicial no puede ser posterior a la fecha final.');
  });

  it('should filter loaded audit events by client-side search term', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.filteredEvents().length).toBe(3);

    // Search by entityId
    component.searchTerm.set('PROD-01');
    expect(component.filteredEvents().length).toBe(1);
    expect(component.filteredEvents()[0].eventType).toBe('ProductDeactivated');

    // Search by text in metadataJson
    component.searchTerm.set('subtotal');
    expect(component.filteredEvents().length).toBe(1);
    expect(component.filteredEvents()[0].eventType).toBe('SaleConfirmed');

    // Search with no matches
    component.searchTerm.set('inexistente');
    expect(component.filteredEvents().length).toBe(0);
  });

  it('should open and close the detail modal with the selected audit event', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.selectedEvent()).toBeNull();

    component.openDetailModal(mockEvents[0]);
    expect(component.selectedEvent()).toEqual(mockEvents[0]);

    component.closeDetailModal();
    expect(component.selectedEvent()).toBeNull();
  });

  it('should format metadata JSON safely or return fallback text', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;

    const formatted = component.formatMetadataJson('{"key":"value"}');
    expect(formatted).toContain('"key": "value"');

    const empty = component.formatMetadataJson(null);
    expect(empty).toBe('Sin metadatos registrados.');

    const invalid = component.formatMetadataJson('invalid-json');
    expect(invalid).toBe('invalid-json');
  });

  it('should compute monitoring metrics correctly', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.userEventsCount()).toBe(2);
    expect(component.systemEventsCount()).toBe(1);
    expect(component.uniqueEntitiesCount()).toBe(3); // Sale, InventoryMovement, Product
  });

  it('should handle API errors gracefully', () => {
    mockAuditService.list.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' }))
    );

    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.errorMessage()).toContain('error en el servidor');
    expect(component.loading()).toBe(false);
  });

  it('should reset filters and reload list', () => {
    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.selectedEventType.set('SaleConfirmed');
    component.selectedEntityName.set('Sale');
    component.entityId.set('sale-100');
    component.userId.set('admin-1');
    component.searchTerm.set('filtro');

    component.resetFilters();

    expect(component.selectedEventType()).toBe('');
    expect(component.selectedEntityName()).toBe('');
    expect(component.entityId()).toBe('');
    expect(component.userId()).toBe('');
    expect(component.searchTerm()).toBe('');
    expect(mockAuditService.list).toHaveBeenCalled();
  });

  it('should invoke window.print when printAuditLog is called', () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});

    const fixture = TestBed.createComponent(AuditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.printAuditLog();
    expect(printSpy).toHaveBeenCalled();

    printSpy.mockRestore();
  });
});
