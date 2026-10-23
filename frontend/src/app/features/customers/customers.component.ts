import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AuthService,
  CreateCustomerRequest,
  CustomerResponse,
  CustomerService,
  parseApiError,
  UpdateCustomerRequest
} from '@core';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Gestión de Clientes</h1>
          <p class="page-subtitle">Directorio de clientes comerciales y registro de contactos</p>
        </div>
        @if (authService.isAdmin()) {
          <div class="page-actions">
            <button
              type="button"
              class="btn btn-primary"
              (click)="openCreateModal()"
              aria-label="Registrar nuevo cliente"
            >
              <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>Nuevo Cliente</span>
            </button>
          </div>
        }
      </div>

      <!-- Feedback Alerts -->
      @if (successMessage()) {
        <div class="alert alert-success" role="status">
          <span class="alert-text">{{ successMessage() }}</span>
          <button type="button" class="alert-close-btn" (click)="successMessage.set(null)" aria-label="Cerrar notificación">&times;</button>
        </div>
      }

      @if (errorMessage()) {
        <div class="alert alert-danger" role="alert">
          <span class="alert-text">{{ errorMessage() }}</span>
          <button type="button" class="alert-close-btn" (click)="errorMessage.set(null)" aria-label="Cerrar alerta">&times;</button>
        </div>
      }

      <!-- Toolbar / Filters -->
      <div class="card toolbar-card">
        <div class="filters-row">
          <div class="search-box">
            <span class="search-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                <circle cx="11" cy="11" r="8"></circle>
                <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
              </svg>
            </span>
            <input
              type="text"
              class="form-control search-input"
              placeholder="Buscar por nombre, cédula o email..."
              [value]="searchTerm()"
              (input)="onSearchChange($event)"
              aria-label="Buscar clientes"
            />
          </div>

          <div class="select-group">
            <label for="status-filter" class="filter-label">Estado:</label>
            <select
              id="status-filter"
              class="form-control select-control"
              [value]="selectedStatusFilter()"
              (change)="onStatusFilterChange($event)"
            >
              <option value="all">Todos los estados</option>
              <option value="active">Solo Activos</option>
              <option value="inactive">Solo Inactivos</option>
            </select>
          </div>

          @if (hasActiveFilters()) {
            <button type="button" class="btn btn-secondary btn-sm" (click)="resetFilters()">
              Limpiar Filtros
            </button>
          }
        </div>

        <div class="stats-row">
          <span class="stat-pill">
            <span class="stat-num">{{ totalCount() }}</span>
            <span class="stat-label">Total</span>
          </span>
          <span class="stat-pill">
            <span class="stat-num text-success">{{ activeCount() }}</span>
            <span class="stat-label">Activos</span>
          </span>
          <span class="stat-pill">
            <span class="stat-num text-muted">{{ inactiveCount() }}</span>
            <span class="stat-label">Inactivos</span>
          </span>
        </div>
      </div>

      <!-- State: Loading -->
      @if (isLoading()) {
        <div class="card state-card">
          <div class="spinner-large" aria-hidden="true"></div>
          <p class="state-text">Cargando directorio de clientes...</p>
        </div>
      }

      <!-- State: Empty Directory -->
      @if (!isLoading() && customers().length === 0) {
        <div class="card state-card">
          <div class="empty-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
              <circle cx="9" cy="7" r="4"></circle>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
              <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
            </svg>
          </div>
          <h2 class="state-title">No hay clientes registrados</h2>
          <p class="state-text">El directorio de clientes está vacío. Comience agregando el primer cliente.</p>
          @if (authService.isAdmin()) {
            <button type="button" class="btn btn-primary" (click)="openCreateModal()">
              Registrar Primer Cliente
            </button>
          }
        </div>
      }

      <!-- State: Empty Filtered Results -->
      @if (!isLoading() && customers().length > 0 && filteredCustomers().length === 0) {
        <div class="card state-card">
          <div class="empty-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
            </svg>
          </div>
          <h2 class="state-title">Sin resultados coincidentes</h2>
          <p class="state-text">No se encontraron clientes que coincidan con los criterios de búsqueda.</p>
          <button type="button" class="btn btn-secondary" (click)="resetFilters()">
            Restablecer Filtros
          </button>
        </div>
      }

      <!-- Table Listing -->
      @if (!isLoading() && filteredCustomers().length > 0) {
        <div class="card table-container">
          <div class="table-responsive">
            <table class="data-table" aria-label="Directorio de Clientes">
              <thead>
                <tr>
                  <th scope="col">Cliente</th>
                  <th scope="col">Identificación</th>
                  <th scope="col">Contacto</th>
                  <th scope="col">Dirección</th>
                  <th scope="col">Estado</th>
                  @if (authService.isAdmin()) {
                    <th scope="col" class="text-right">Acciones</th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (cust of filteredCustomers(); track cust.id) {
                  <tr>
                    <td>
                      <div class="customer-info">
                        <span class="customer-name">{{ cust.name }}</span>
                        <span class="customer-date">Registrado: {{ cust.createdAtUtc | date:'shortDate' }}</span>
                      </div>
                    </td>
                    <td>
                      @if (cust.identificationNumber) {
                        <span class="id-pill font-mono">{{ cust.identificationNumber }}</span>
                      } @else {
                        <span class="text-muted">—</span>
                      }
                    </td>
                    <td>
                      <div class="contact-details">
                        @if (cust.email) {
                          <a class="contact-link" [href]="'mailto:' + cust.email" title="Enviar correo">
                            <svg viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2" fill="none">
                              <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
                              <polyline points="22,6 12,13 2,6"></polyline>
                            </svg>
                            <span>{{ cust.email }}</span>
                          </a>
                        }
                        @if (cust.phone) {
                          <a class="contact-link" [href]="'tel:' + cust.phone" title="Llamar">
                            <svg viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2" fill="none">
                              <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path>
                            </svg>
                            <span>{{ cust.phone }}</span>
                          </a>
                        }
                        @if (!cust.email && !cust.phone) {
                          <span class="text-muted">—</span>
                        }
                      </div>
                    </td>
                    <td>
                      <span class="address-text">{{ cust.address || '—' }}</span>
                    </td>
                    <td>
                      <span class="badge" [class.badge-success]="cust.isActive" [class.badge-danger]="!cust.isActive">
                        {{ cust.isActive ? 'Activo' : 'Inactivo' }}
                      </span>
                    </td>
                    @if (authService.isAdmin()) {
                      <td class="text-right">
                        <div class="actions-group">
                          <button
                            type="button"
                            class="action-btn edit-btn"
                            (click)="openEditModal(cust)"
                            aria-label="Editar cliente {{ cust.name }}"
                            title="Editar datos"
                          >
                            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                            </svg>
                          </button>
                          <button
                            type="button"
                            class="action-btn toggle-btn"
                            (click)="onToggleActive(cust)"
                            [attr.aria-label]="cust.isActive ? 'Desactivar cliente ' + cust.name : 'Activar cliente ' + cust.name"
                            [title]="cust.isActive ? 'Desactivar cliente' : 'Activar cliente'"
                          >
                            @if (cust.isActive) {
                              <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                                <circle cx="12" cy="12" r="10"></circle>
                                <line x1="4.93" y1="4.93" x2="19.07" y2="19.07"></line>
                              </svg>
                            } @else {
                              <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                                <polyline points="22 4 12 14.01 9 11.01"></polyline>
                              </svg>
                            }
                          </button>
                        </div>
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Create / Edit Modal -->
      @if (isFormModalOpen()) {
        <div class="modal-backdrop" (click)="closeFormModal()">
          <div class="modal-card" (click)="$event.stopPropagation()" role="dialog" [attr.aria-label]="editingCustomer() ? 'Editar Cliente' : 'Nuevo Cliente'">
            <div class="modal-header">
              <h2 class="modal-title">{{ editingCustomer() ? 'Editar Cliente' : 'Nuevo Cliente' }}</h2>
              <button type="button" class="modal-close-btn" (click)="closeFormModal()" aria-label="Cerrar ventana">&times;</button>
            </div>

            @if (formError()) {
              <div class="alert alert-danger modal-alert" role="alert">
                <span>{{ formError() }}</span>
              </div>
            }

            <form [formGroup]="form" (ngSubmit)="onSaveCustomer()" novalidate class="modal-form">
              <!-- Name -->
              <div class="form-group">
                <label for="cust-name" class="form-label">Nombre o Razón Social *</label>
                <input
                  id="cust-name"
                  type="text"
                  formControlName="name"
                  class="form-control"
                  [class.is-invalid]="isFieldInvalid('name')"
                  placeholder="Ej. Distribuidora Central S.A."
                  maxlength="200"
                  required
                />
                @if (isFieldInvalid('name')) {
                  <div class="field-error">
                    El nombre es requerido (1 a 200 caracteres).
                  </div>
                }
              </div>

              <!-- Identification Number -->
              <div class="form-group">
                <label for="cust-id-num" class="form-label">Cédula / Identificación Fiscal</label>
                <input
                  id="cust-id-num"
                  type="text"
                  formControlName="identificationNumber"
                  class="form-control font-mono"
                  [class.is-invalid]="isFieldInvalid('identificationNumber')"
                  placeholder="Ej. J-12345678-0, 1-1234-5678"
                  maxlength="50"
                />
                <span class="field-hint">Número de identificación único (máx. 50 caracteres).</span>
                @if (isFieldInvalid('identificationNumber')) {
                  <div class="field-error">
                    La identificación no puede exceder 50 caracteres.
                  </div>
                }
              </div>

              <!-- Email and Phone Row -->
              <div class="form-row">
                <div class="form-group flex-1">
                  <label for="cust-email" class="form-label">Correo Electrónico</label>
                  <input
                    id="cust-email"
                    type="email"
                    formControlName="email"
                    class="form-control"
                    [class.is-invalid]="isFieldInvalid('email')"
                    placeholder="contacto@empresa.com"
                    maxlength="320"
                  />
                  @if (isFieldInvalid('email')) {
                    <div class="field-error">
                      Formato de correo electrónico inválido.
                    </div>
                  }
                </div>

                <div class="form-group flex-1">
                  <label for="cust-phone" class="form-label">Teléfono</label>
                  <input
                    id="cust-phone"
                    type="tel"
                    formControlName="phone"
                    class="form-control"
                    [class.is-invalid]="isFieldInvalid('phone')"
                    placeholder="+506 2222-0000"
                    maxlength="30"
                  />
                  @if (isFieldInvalid('phone')) {
                    <div class="field-error">
                      El teléfono no puede exceder 30 caracteres.
                    </div>
                  }
                </div>
              </div>

              <!-- Address -->
              <div class="form-group">
                <label for="cust-address" class="form-label">Dirección Física</label>
                <textarea
                  id="cust-address"
                  formControlName="address"
                  class="form-control"
                  rows="2"
                  [class.is-invalid]="isFieldInvalid('address')"
                  placeholder="Provincia, cantón, señas particulares..."
                  maxlength="300"
                ></textarea>
                @if (isFieldInvalid('address')) {
                  <div class="field-error">
                    La dirección no puede exceder 300 caracteres.
                  </div>
                }
              </div>

              <div class="modal-footer">
                <button type="button" class="btn btn-secondary" (click)="closeFormModal()" [disabled]="isSubmitting()">
                  Cancelar
                </button>
                <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
                  @if (isSubmitting()) {
                    <span class="spinner" aria-hidden="true"></span>
                    <span>Guardando...</span>
                  } @else {
                    <span>{{ editingCustomer() ? 'Guardar Cambios' : 'Crear Cliente' }}</span>
                  }
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .customer-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .customer-name {
      font-weight: 600;
      color: var(--color-text);
    }

    .customer-date {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
    }

    .contact-details {
      display: flex;
      flex-direction: column;
      gap: 3px;
    }

    .contact-link {
      display: inline-flex;
      align-items: center;
      gap: var(--space-1);
      font-size: var(--font-size-xs);
      color: var(--color-primary);
      text-decoration: none;
    }

    .contact-link:hover {
      text-decoration: underline;
    }

    .address-text {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      max-width: 220px;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
  `]
})
export class CustomersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  public readonly authService = inject(AuthService);

  public readonly customers = signal<CustomerResponse[]>([]);
  public readonly isLoading = signal<boolean>(false);
  public readonly errorMessage = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);

  // Filters
  public readonly searchTerm = signal<string>('');
  public readonly selectedStatusFilter = signal<string>('all');

  // Modal
  public readonly isFormModalOpen = signal<boolean>(false);
  public readonly editingCustomer = signal<CustomerResponse | null>(null);
  public readonly isSubmitting = signal<boolean>(false);
  public readonly formError = signal<string | null>(null);

  public readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    identificationNumber: ['', [Validators.maxLength(50)]],
    email: ['', [Validators.email, Validators.maxLength(320)]],
    phone: ['', [Validators.maxLength(30)]],
    address: ['', [Validators.maxLength(300)]]
  });

  public readonly hasActiveFilters = computed(() => {
    return this.searchTerm().trim().length > 0 || this.selectedStatusFilter() !== 'all';
  });

  public readonly filteredCustomers = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const status = this.selectedStatusFilter();

    return this.customers().filter((customer) => {
      // Status filter
      if (status === 'active' && !customer.isActive) {
        return false;
      }
      if (status === 'inactive' && customer.isActive) {
        return false;
      }

      // Search term filter
      if (!term) {
        return true;
      }

      const matchName = customer.name.toLowerCase().includes(term);
      const matchId = customer.identificationNumber?.toLowerCase().includes(term) ?? false;
      const matchEmail = customer.email?.toLowerCase().includes(term) ?? false;
      const matchPhone = customer.phone?.toLowerCase().includes(term) ?? false;

      return matchName || matchId || matchEmail || matchPhone;
    });
  });

  public readonly totalCount = computed(() => this.customers().length);
  public readonly activeCount = computed(() => this.customers().filter((c) => c.isActive).length);
  public readonly inactiveCount = computed(() => this.customers().filter((c) => !c.isActive).length);

  public ngOnInit(): void {
    this.loadCustomers();
  }

  public loadCustomers(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.customerService.list().subscribe({
      next: (list) => {
        this.customers.set(list);
        this.isLoading.set(false);
      },
      error: (err) => {
        const parsed = parseApiError(err);
        this.errorMessage.set(parsed);
        this.isLoading.set(false);
      }
    });
  }

  public onSearchChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }

  public onStatusFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedStatusFilter.set(value);
  }

  public resetFilters(): void {
    this.searchTerm.set('');
    this.selectedStatusFilter.set('all');
  }

  public openCreateModal(): void {
    this.editingCustomer.set(null);
    this.formError.set(null);
    this.form.reset({
      name: '',
      identificationNumber: '',
      email: '',
      phone: '',
      address: ''
    });
    this.isFormModalOpen.set(true);
  }

  public openEditModal(customer: CustomerResponse): void {
    this.editingCustomer.set(customer);
    this.formError.set(null);
    this.form.reset({
      name: customer.name,
      identificationNumber: customer.identificationNumber ?? '',
      email: customer.email ?? '',
      phone: customer.phone ?? '',
      address: customer.address ?? ''
    });
    this.isFormModalOpen.set(true);
  }

  public closeFormModal(): void {
    this.isFormModalOpen.set(false);
    this.editingCustomer.set(null);
    this.formError.set(null);
  }

  public isFieldInvalid(fieldName: 'name' | 'identificationNumber' | 'email' | 'phone' | 'address'): boolean {
    const field = this.form.get(fieldName);
    return !!field && field.invalid && (field.dirty || field.touched);
  }

  public onSaveCustomer(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload = {
      name: raw.name.trim(),
      identificationNumber: raw.identificationNumber.trim() || null,
      email: raw.email.trim() || null,
      phone: raw.phone.trim() || null,
      address: raw.address.trim() || null
    };

    this.isSubmitting.set(true);
    this.formError.set(null);

    const targetCustomer = this.editingCustomer();

    if (targetCustomer) {
      // Update
      const req: UpdateCustomerRequest = { ...payload };
      this.customerService.update(targetCustomer.id, req).subscribe({
        next: (updated) => {
          this.customers.update((current) =>
            current.map((c) => (c.id === updated.id ? updated : c))
          );
          this.isSubmitting.set(false);
          this.closeFormModal();
          this.successMessage.set(`Cliente "${updated.name}" actualizado con éxito.`);
        },
        error: (err) => {
          const parsed = parseApiError(err);
          this.formError.set(parsed);
          this.isSubmitting.set(false);
        }
      });
    } else {
      // Create
      const req: CreateCustomerRequest = { ...payload };
      this.customerService.create(req).subscribe({
        next: (created) => {
          this.customers.update((current) => [created, ...current]);
          this.isSubmitting.set(false);
          this.closeFormModal();
          this.successMessage.set(`Cliente "${created.name}" registrado con éxito.`);
        },
        error: (err) => {
          const parsed = parseApiError(err);
          this.formError.set(parsed);
          this.isSubmitting.set(false);
        }
      });
    }
  }

  public onToggleActive(customer: CustomerResponse): void {
    const operation = customer.isActive
      ? this.customerService.deactivate(customer.id)
      : this.customerService.activate(customer.id);

    operation.subscribe({
      next: (updated) => {
        this.customers.update((current) =>
          current.map((c) => (c.id === updated.id ? updated : c))
        );
        const actionText = updated.isActive ? 'activado' : 'desactivado';
        this.successMessage.set(`Cliente "${updated.name}" ${actionText} con éxito.`);
      },
      error: (err) => {
        const parsed = parseApiError(err);
        this.errorMessage.set(parsed);
      }
    });
  }
}
