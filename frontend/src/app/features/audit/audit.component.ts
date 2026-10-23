import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AUDIT_ENTITY_NAMES,
  AUDIT_EVENT_TYPES,
  AuditEventResponse,
  AuditQuery,
  AuditService,
  AuthService,
  parseApiError
} from '@core';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Pista de Auditoría y Monitoreo</h1>
          <p class="page-subtitle">Registro cronológico inmutable de eventos, mutaciones y operaciones del sistema</p>
        </div>
        <div class="page-actions">
          <button type="button" class="btn btn-secondary btn-sm" (click)="printAuditLog()">
            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
              <polyline points="6 9 6 2 18 2 18 9"></polyline>
              <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
              <rect x="6" y="14" width="12" height="8"></rect>
            </svg>
            Imprimir Registro
          </button>
        </div>
      </div>

      <!-- Non-admin permission barrier fallback -->
      <div *ngIf="!authService.isAdmin()" class="card restriction-card">
        <div class="restriction-icon text-warning">
          <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="2" fill="none">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
            <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
          </svg>
        </div>
        <h2 class="restriction-title">Acceso Restringido a Administradores</h2>
        <p class="restriction-text">
          La pista de auditoría contiene información sensible de seguridad y trazabilidad operativa.
          Esta sección está reservada exclusivamente para administradores.
        </p>
      </div>

      <!-- Admin Content -->
      <ng-container *ngIf="authService.isAdmin()">
        <!-- Error Alert -->
        <div *ngIf="errorMessage()" class="alert alert-danger" role="alert">
          <div class="alert-content">
            <p class="alert-message">{{ errorMessage() }}</p>
          </div>
          <button type="button" class="btn-close" (click)="errorMessage.set(null)" aria-label="Cerrar alerta">×</button>
        </div>

        <!-- Filters Toolbar Card -->
        <div class="card toolbar-card">
          <form (ngSubmit)="loadAuditEvents()" class="filters-row">
            <div class="form-group">
              <label for="auditFromDate" class="form-label">Desde (UTC)</label>
              <input
                type="date"
                id="auditFromDate"
                class="form-control"
                [ngModel]="fromDate()"
                (ngModelChange)="fromDate.set($event)"
                name="fromDate" />
            </div>

            <div class="form-group">
              <label for="auditToDate" class="form-label">Hasta (UTC)</label>
              <input
                type="date"
                id="auditToDate"
                class="form-control"
                [ngModel]="toDate()"
                (ngModelChange)="toDate.set($event)"
                name="toDate" />
            </div>

            <div class="form-group">
              <label for="auditEventTypeSelect" class="form-label">Tipo de Evento</label>
              <select
                id="auditEventTypeSelect"
                class="form-control"
                [ngModel]="selectedEventType()"
                (ngModelChange)="selectedEventType.set($event)"
                name="selectedEventType">
                <option value="">Todos los eventos</option>
                <option *ngFor="let t of eventTypes" [value]="t">
                  {{ formatEventType(t) }} ({{ t }})
                </option>
              </select>
            </div>

            <div class="form-group">
              <label for="auditEntityNameSelect" class="form-label">Entidad</label>
              <select
                id="auditEntityNameSelect"
                class="form-control"
                [ngModel]="selectedEntityName()"
                (ngModelChange)="selectedEntityName.set($event)"
                name="selectedEntityName">
                <option value="">Todas las entidades</option>
                <option *ngFor="let e of entityNames" [value]="e">
                  {{ formatEntityName(e) }} ({{ e }})
                </option>
              </select>
            </div>

            <div class="form-group">
              <label for="auditEntityIdInput" class="form-label">ID de Entidad</label>
              <input
                type="text"
                id="auditEntityIdInput"
                class="form-control font-mono"
                placeholder="ID o código..."
                [ngModel]="entityId()"
                (ngModelChange)="entityId.set($event)"
                name="entityId" />
            </div>

            <div class="form-group">
              <label for="auditUserIdInput" class="form-label">ID de Usuario</label>
              <input
                type="text"
                id="auditUserIdInput"
                class="form-control font-mono"
                placeholder="GUID de usuario..."
                [ngModel]="userId()"
                (ngModelChange)="userId.set($event)"
                name="userId" />
            </div>

            <div class="actions-group" style="align-self: flex-end;">
              <button type="submit" class="btn btn-primary" [disabled]="loading()">
                <span *ngIf="loading()" class="spinner-sm" aria-hidden="true"></span>
                Filtrar
              </button>
              <button type="button" class="btn btn-secondary" (click)="resetFilters()">
                Restablecer
              </button>
            </div>
          </form>
        </div>

        <!-- Monitoring Metrics Cards -->
        <div class="state-card" style="margin-bottom: var(--space-4);">
          <div class="stat-card">
            <span class="stat-label">Eventos Auditados</span>
            <span class="stat-value font-mono">{{ auditEvents().length }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Total de registros en vista</span>
          </div>

          <div class="stat-card" style="border-left: 4px solid var(--color-primary);">
            <span class="stat-label">Acciones de Usuario</span>
            <span class="stat-value font-mono text-primary">{{ userEventsCount() }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Operaciones con operador autenticado</span>
          </div>

          <div class="stat-card" style="border-left: 4px solid var(--color-warning);">
            <span class="stat-label">Eventos de Sistema</span>
            <span class="stat-value font-mono text-warning">{{ systemEventsCount() }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Procesos automáticos o anónimos</span>
          </div>

          <div class="stat-card">
            <span class="stat-label">Entidades Afectadas</span>
            <span class="stat-value font-mono">{{ uniqueEntitiesCount() }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Agregados de negocio con actividad</span>
          </div>
        </div>

        <!-- Client-Side Quick Filter & Search -->
        <div class="card" style="padding: var(--space-3) var(--space-4); margin-bottom: var(--space-4);">
          <div class="form-row" style="align-items: center; justify-content: space-between;">
            <div class="form-group flex-1" style="margin-bottom: 0;">
              <input
                type="text"
                class="form-control"
                placeholder="Filtrar por texto en metadatos, ID de entidad o usuario..."
                [ngModel]="searchTerm()"
                (ngModelChange)="searchTerm.set($event)" />
            </div>
            <div style="font-size: var(--font-size-xs); color: var(--color-text-muted);">
              Mostrando {{ filteredEvents().length }} de {{ auditEvents().length }} registros
            </div>
          </div>
        </div>

        <!-- Immutability Security Banner -->
        <div class="card" style="padding: var(--space-3) var(--space-4); margin-bottom: var(--space-4); background-color: var(--color-surface-hover);">
          <p style="font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0;">
            <strong>Principio de Inmutabilidad:</strong> Los registros de auditoría son de solo lectura, append-only y no pueden ser modificados o eliminados ni siquiera por usuarios administradores.
          </p>
        </div>

        <!-- Loading State -->
        <div *ngIf="loading()" class="card loading-card">
          <div class="spinner"></div>
          <p>Consultando pista de auditoría...</p>
        </div>

        <!-- Audit Records Table -->
        <div *ngIf="!loading()" class="card table-card">
          <div class="table-container">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Fecha / Hora (UTC y Local)</th>
                  <th>Evento</th>
                  <th>Entidad Afectada</th>
                  <th>Identificador de Entidad</th>
                  <th>Atribución / Usuario</th>
                  <th class="text-right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let event of filteredEvents()">
                  <td>
                    <div style="display: flex; flex-direction: column;">
                      <span class="font-mono" style="font-size: var(--font-size-xs); font-weight: 600;">
                        {{ event.occurredAtUtc | date:'yyyy-MM-dd HH:mm:ss' }} UTC
                      </span>
                      <span class="text-muted" style="font-size: 11px;">
                        Local: {{ event.occurredAtUtc | date:'short' }}
                      </span>
                    </div>
                  </td>
                  <td>
                    <span
                      class="badge"
                      [ngClass]="getEventTypeBadgeClass(event.eventType)">
                      {{ formatEventType(event.eventType) }}
                    </span>
                  </td>
                  <td>
                    <span class="badge badge-neutral">
                      {{ formatEntityName(event.entityName) }}
                    </span>
                  </td>
                  <td>
                    <span *ngIf="event.entityId" class="id-pill font-mono" [title]="event.entityId">
                      {{ event.entityId }}
                    </span>
                    <span *ngIf="!event.entityId" class="text-muted" style="font-size: var(--font-size-xs);">
                      —
                    </span>
                  </td>
                  <td>
                    <div *ngIf="event.hasUser && event.userId">
                      <span class="id-pill font-mono" [title]="event.userId">
                        {{ event.userId }}
                      </span>
                    </div>
                    <span *ngIf="!event.hasUser || !event.userId" class="badge badge-neutral">
                      Sistema
                    </span>
                  </td>
                  <td class="text-right">
                    <button
                      type="button"
                      class="btn btn-secondary btn-sm"
                      (click)="openDetailModal(event)">
                      Ver Detalle
                    </button>
                  </td>
                </tr>

                <tr *ngIf="filteredEvents().length === 0">
                  <td colspan="6" class="text-center text-muted" style="padding: var(--space-8);">
                    No se encontraron registros de auditoría que coincidan con los criterios especificados.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- ================================================================= -->
        <!-- DETAIL MODAL: AUDIT EVENT INSPECTION                              -->
        <!-- ================================================================= -->
        <div *ngIf="selectedEvent() as event" class="modal-backdrop" (click)="closeDetailModal()">
          <div class="modal-card" style="max-width: 650px;" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h2 class="modal-title">Detalle del Evento de Auditoría</h2>
              <button type="button" class="btn-close" (click)="closeDetailModal()" aria-label="Cerrar">×</button>
            </div>

            <div class="modal-body" style="display: flex; flex-direction: column; gap: var(--space-4);">
              <div class="meta-grid">
                <div>
                  <span class="meta-label">ID de Evento</span>
                  <span class="id-pill font-mono" style="word-break: break-all;">{{ event.id }}</span>
                </div>
                <div>
                  <span class="meta-label">Tipo de Evento</span>
                  <span class="badge" [ngClass]="getEventTypeBadgeClass(event.eventType)">
                    {{ formatEventType(event.eventType) }} ({{ event.eventType }})
                  </span>
                </div>
                <div>
                  <span class="meta-label">Entidad Afectada</span>
                  <span class="font-mono">{{ formatEntityName(event.entityName) }} ({{ event.entityName }})</span>
                </div>
                <div>
                  <span class="meta-label">ID de la Entidad</span>
                  <span class="font-mono">{{ event.entityId || 'No aplicable' }}</span>
                </div>
                <div>
                  <span class="meta-label">Fecha y Hora UTC</span>
                  <span class="font-mono">{{ event.occurredAtUtc | date:'yyyy-MM-dd HH:mm:ss' }} UTC</span>
                </div>
                <div>
                  <span class="meta-label">Fecha y Hora Local</span>
                  <span>{{ event.occurredAtUtc | date:'medium' }}</span>
                </div>
                <div style="grid-column: span 2;">
                  <span class="meta-label">Atribución de Operador</span>
                  <span *ngIf="event.hasUser && event.userId" class="font-mono">
                    Usuario: {{ event.userId }}
                  </span>
                  <span *ngIf="!event.hasUser || !event.userId" class="badge badge-neutral">
                    Operación de Sistema / Proceso Automático
                  </span>
                </div>
              </div>

              <div>
                <span class="meta-label" style="margin-bottom: var(--space-1); display: block;">
                  Carga Útil / Metadatos de Auditoría (JSON)
                </span>
                <pre class="metadata-code font-mono">{{ formatMetadataJson(event.metadataJson) }}</pre>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeDetailModal()">
                Cerrar
              </button>
            </div>
          </div>
        </div>
      </ng-container>
    </div>
  `,
  styles: [`
    .restriction-card {
      padding: var(--space-10) var(--space-6);
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      gap: var(--space-3);
    }

    .restriction-icon {
      margin-bottom: var(--space-2);
    }

    .restriction-title {
      font-size: var(--font-size-lg);
      font-weight: 600;
      color: var(--color-text);
    }

    .restriction-text {
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
      max-width: 520px;
      line-height: 1.6;
    }

    .loading-card {
      padding: var(--space-8);
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: var(--space-3);
      color: var(--color-text-muted);
    }

    .meta-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--space-3);
    }

    .meta-label {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      display: block;
      margin-bottom: var(--space-1);
    }

    .metadata-code {
      background-color: var(--color-surface-hover);
      padding: var(--space-3);
      border-radius: var(--radius-sm);
      border: 1px solid var(--color-border);
      font-size: var(--font-size-xs);
      overflow-x: auto;
      max-height: 250px;
      color: var(--color-text);
      white-space: pre-wrap;
    }
  `]
})
export class AuditComponent implements OnInit {
  public readonly authService = inject(AuthService);
  private readonly auditService = inject(AuditService);

  public readonly eventTypes = AUDIT_EVENT_TYPES;
  public readonly entityNames = AUDIT_ENTITY_NAMES;

  // State Signals
  public readonly loading = signal<boolean>(false);
  public readonly errorMessage = signal<string | null>(null);
  public readonly auditEvents = signal<AuditEventResponse[]>([]);
  public readonly selectedEvent = signal<AuditEventResponse | null>(null);

  // Filter Signals
  public readonly fromDate = signal<string>(this.calculateDefaultFromDate());
  public readonly toDate = signal<string>(this.calculateDefaultToDate());
  public readonly selectedEventType = signal<string>('');
  public readonly selectedEntityName = signal<string>('');
  public readonly entityId = signal<string>('');
  public readonly userId = signal<string>('');
  public readonly searchTerm = signal<string>('');

  // Computed Monitoring Metrics
  public readonly userEventsCount = computed(() =>
    this.auditEvents().filter(e => e.hasUser).length
  );

  public readonly systemEventsCount = computed(() =>
    this.auditEvents().filter(e => !e.hasUser).length
  );

  public readonly uniqueEntitiesCount = computed(() => {
    const names = new Set(this.auditEvents().map(e => e.entityName));
    return names.size;
  });

  public readonly filteredEvents = computed(() => {
    const list = this.auditEvents();
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return list;

    return list.filter(
      e =>
        e.eventType.toLowerCase().includes(term) ||
        e.entityName.toLowerCase().includes(term) ||
        (e.entityId && e.entityId.toLowerCase().includes(term)) ||
        (e.userId && e.userId.toLowerCase().includes(term)) ||
        (e.metadataJson && e.metadataJson.toLowerCase().includes(term))
    );
  });

  public ngOnInit(): void {
    if (this.authService.isAdmin()) {
      this.loadAuditEvents();
    }
  }

  public loadAuditEvents(): void {
    if (!this.authService.isAdmin()) return;

    if (this.fromDate() && this.toDate() && this.fromDate() > this.toDate()) {
      this.errorMessage.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const query: AuditQuery = {
      eventType: this.selectedEventType() || null,
      entityName: this.selectedEntityName() || null,
      entityId: this.entityId().trim() || null,
      userId: this.userId().trim() || null,
      fromUtc: this.fromDate() ? `${this.fromDate()}T00:00:00.000Z` : null,
      toUtc: this.toDate() ? `${this.toDate()}T23:59:59.999Z` : null
    };

    this.auditService.list(query).subscribe({
      next: events => {
        this.auditEvents.set(events);
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public resetFilters(): void {
    this.fromDate.set(this.calculateDefaultFromDate());
    this.toDate.set(this.calculateDefaultToDate());
    this.selectedEventType.set('');
    this.selectedEntityName.set('');
    this.entityId.set('');
    this.userId.set('');
    this.searchTerm.set('');
    this.loadAuditEvents();
  }

  public openDetailModal(event: AuditEventResponse): void {
    this.selectedEvent.set(event);
  }

  public closeDetailModal(): void {
    this.selectedEvent.set(null);
  }

  public printAuditLog(): void {
    window.print();
  }

  public formatEventType(eventType: string): string {
    const translations: Record<string, string> = {
      SaleCreated: 'Venta Creada',
      SaleConfirmed: 'Venta Confirmada',
      SaleCancelled: 'Venta Cancelada',
      InventoryMovementRecorded: 'Movimiento Inventario',
      ProductActivated: 'Producto Activado',
      ProductDeactivated: 'Producto Desactivado',
      SupplierActivated: 'Proveedor Activado',
      SupplierDeactivated: 'Proveedor Desactivado',
      CustomerActivated: 'Cliente Activado',
      CustomerDeactivated: 'Cliente Desactivado',
      UserLoginSucceeded: 'Inicio Sesión Exitoso',
      UserLoginFailed: 'Inicio Sesión Fallido',
      UserActivated: 'Usuario Activado',
      UserDeactivated: 'Usuario Desactivado'
    };
    return translations[eventType] || eventType;
  }

  public formatEntityName(entityName: string): string {
    const translations: Record<string, string> = {
      Sale: 'Venta',
      InventoryMovement: 'Mov. Inventario',
      Product: 'Producto',
      Supplier: 'Proveedor',
      Customer: 'Cliente',
      User: 'Usuario'
    };
    return translations[entityName] || entityName;
  }

  public getEventTypeBadgeClass(eventType: string): string {
    if (
      eventType === 'SaleConfirmed' ||
      eventType === 'ProductActivated' ||
      eventType === 'SupplierActivated' ||
      eventType === 'CustomerActivated' ||
      eventType === 'UserLoginSucceeded' ||
      eventType === 'UserActivated'
    ) {
      return 'badge-success';
    }

    if (
      eventType === 'SaleCancelled' ||
      eventType === 'ProductDeactivated' ||
      eventType === 'SupplierDeactivated' ||
      eventType === 'CustomerDeactivated' ||
      eventType === 'UserLoginFailed' ||
      eventType === 'UserDeactivated'
    ) {
      return 'badge-danger';
    }

    return 'badge-info';
  }

  public formatMetadataJson(metadataJson: string | null): string {
    if (!metadataJson) return 'Sin metadatos registrados.';
    try {
      const parsed = JSON.parse(metadataJson);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return metadataJson;
    }
  }

  private calculateDefaultFromDate(): string {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split('T')[0];
  }

  private calculateDefaultToDate(): string {
    return new Date().toISOString().split('T')[0];
  }
}
