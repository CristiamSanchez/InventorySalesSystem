import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, CategoryResponse, CategoryService, parseApiError, ProductQueryFilter, ProductResponse, ProductService } from '@core';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Gestión de Productos</h1>
          <p class="page-subtitle">Catálogo de productos, códigos de identificación y asignación de categorías</p>
        </div>
        <div class="page-actions">
          @if (authService.isAdmin()) {
            <button type="button" class="btn btn-primary" (click)="openCreateModal()">
              <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none" aria-hidden="true">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>Nuevo Producto</span>
            </button>
          } @else {
            <span class="badge badge-neutral" title="La creación requiere rol de Administrador">Modo Consulta</span>
          }
        </div>
      </div>

      <!-- Feedback Banners -->
      @if (successMessage()) {
        <div class="alert alert-success" role="status">
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none" aria-hidden="true">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
            <polyline points="22 4 12 14.01 9 11.01"></polyline>
          </svg>
          <span class="alert-text">{{ successMessage() }}</span>
          <button type="button" class="alert-close-btn" (click)="successMessage.set(null)" aria-label="Cerrar notificación">&times;</button>
        </div>
      }

      @if (errorMessage()) {
        <div class="alert alert-danger" role="alert">
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          <span class="alert-text">{{ errorMessage() }}</span>
          <button type="button" class="btn btn-sm btn-secondary" (click)="loadProducts()">Reintentar</button>
        </div>
      }

      <!-- Filter & Search Toolbar -->
      <div class="card toolbar-card">
        <div class="filters-row">
          <div class="search-box">
            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none" class="search-icon" aria-hidden="true">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
            </svg>
            <input
              type="search"
              class="form-control search-input"
              placeholder="Buscar por código o nombre..."
              [value]="searchTerm()"
              (input)="onSearchInput($event)"
              aria-label="Buscar productos"
            />
          </div>

          <div class="select-group">
            <label for="filter-category" class="filter-label">Categoría:</label>
            <select
              id="filter-category"
              class="form-control select-control"
              [value]="selectedCategoryFilter()"
              (change)="onCategoryFilterChange($event)"
            >
              <option value="">Todas las categorías</option>
              @for (cat of categories(); track cat.id) {
                <option [value]="cat.id">{{ cat.name }}</option>
              }
            </select>
          </div>

          <div class="select-group">
            <label for="filter-status" class="filter-label">Estado:</label>
            <select
              id="filter-status"
              class="form-control select-control"
              [value]="selectedStatusFilter()"
              (change)="onStatusFilterChange($event)"
            >
              <option value="all">Todos los estados</option>
              <option value="active">Activos</option>
              <option value="inactive">Inactivos</option>
            </select>
          </div>

          @if (hasActiveFilters()) {
            <button type="button" class="btn btn-secondary btn-sm" (click)="resetFilters()">
              Limpiar filtros
            </button>
          }
        </div>

        <div class="stats-row">
          <span class="stat-pill">
            <strong>{{ products().length }}</strong> {{ products().length === 1 ? 'producto' : 'productos' }}
          </span>
        </div>
      </div>

      <!-- Main Content / Table -->
      @if (isLoading()) {
        <div class="card state-card">
          <div class="spinner-large" aria-hidden="true"></div>
          <p class="state-text">Cargando catálogo de productos...</p>
        </div>
      } @else if (products().length === 0) {
        <div class="card state-card">
          <div class="empty-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
              <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
              <line x1="12" y1="22.08" x2="12" y2="12"></line>
            </svg>
          </div>
          @if (hasActiveFilters()) {
            <h3 class="state-title">Sin resultados de búsqueda</h3>
            <p class="state-text">No se encontraron productos que coincidan con los filtros aplicados.</p>
            <button type="button" class="btn btn-secondary btn-sm" (click)="resetFilters()">Limpiar filtros</button>
          } @else {
            <h3 class="state-title">No hay productos registrados</h3>
            <p class="state-text">Registre el primer producto asignándole una categoría existente.</p>
            @if (authService.isAdmin()) {
              <button type="button" class="btn btn-primary btn-sm" (click)="openCreateModal()">Crear Producto</button>
            }
          }
        </div>
      } @else {
        <div class="card table-container">
          <div class="table-responsive">
            <table class="data-table" aria-label="Listado de productos">
              <thead>
                <tr>
                  <th scope="col">Identificador (SKU)</th>
                  <th scope="col">Nombre del Producto</th>
                  <th scope="col">Categoría</th>
                  <th scope="col">Estado</th>
                  @if (authService.isAdmin()) {
                    <th scope="col" class="text-right">Acciones</th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (prod of products(); track prod.identifier) {
                  <tr>
                    <td>
                      <span class="product-sku font-mono">{{ prod.identifier }}</span>
                    </td>
                    <td class="font-semibold">{{ prod.name }}</td>
                    <td>
                      <span class="badge badge-info">{{ prod.categoryName }}</span>
                    </td>
                    <td>
                      <span class="badge" [class.badge-success]="prod.isActive" [class.badge-neutral]="!prod.isActive">
                        {{ prod.isActive ? 'Activo' : 'Inactivo' }}
                      </span>
                    </td>
                    @if (authService.isAdmin()) {
                      <td class="text-right">
                        <div class="actions-group">
                          <button
                            type="button"
                            class="action-btn edit-btn"
                            (click)="openEditModal(prod)"
                            aria-label="Editar producto {{ prod.name }}"
                            title="Editar"
                          >
                            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                            </svg>
                          </button>
                          <button
                            type="button"
                            class="action-btn toggle-btn"
                            (click)="toggleProductStatus(prod)"
                            [attr.aria-label]="prod.isActive ? 'Desactivar producto ' + prod.name : 'Activar producto ' + prod.name"
                            [title]="prod.isActive ? 'Desactivar producto' : 'Activar producto'"
                          >
                            @if (prod.isActive) {
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

      <!-- Create / Edit Product Modal -->
      @if (isFormModalOpen()) {
        <div class="modal-backdrop" (click)="closeFormModal()">
          <div class="modal-card" (click)="$event.stopPropagation()" role="dialog" [attr.aria-label]="editingProduct() ? 'Editar Producto' : 'Nuevo Producto'">
            <div class="modal-header">
              <h2 class="modal-title">{{ editingProduct() ? 'Editar Producto' : 'Nuevo Producto' }}</h2>
              <button type="button" class="modal-close-btn" (click)="closeFormModal()" aria-label="Cerrar ventana">&times;</button>
            </div>

            @if (formError()) {
              <div class="alert alert-danger modal-alert" role="alert">
                <span>{{ formError() }}</span>
              </div>
            }

            <form [formGroup]="form" (ngSubmit)="onSaveProduct()" novalidate class="modal-form">
              <div class="form-group">
                <label for="prod-identifier" class="form-label">Identificador (SKU / Código) *</label>
                <input
                  id="prod-identifier"
                  type="text"
                  formControlName="identifier"
                  class="form-control font-mono"
                  [class.is-invalid]="isFieldInvalid('identifier')"
                  placeholder="Ej. PROD-001, BEB-COCA-350"
                  maxlength="100"
                  required
                />
                @if (isFieldInvalid('identifier')) {
                  <div class="field-error">
                    El identificador es requerido (máximo 100 caracteres).
                  </div>
                }
                @if (editingProduct()) {
                  <span class="field-hint">El identificador único no puede modificarse una vez creado.</span>
                }
              </div>

              <div class="form-group">
                <label for="prod-name" class="form-label">Nombre del Producto *</label>
                <input
                  id="prod-name"
                  type="text"
                  formControlName="name"
                  class="form-control"
                  [class.is-invalid]="isFieldInvalid('name')"
                  placeholder="Ej. Refresco Cola 350ml"
                  maxlength="200"
                  required
                />
                @if (isFieldInvalid('name')) {
                  <div class="field-error">
                    El nombre es requerido (máximo 200 caracteres).
                  </div>
                }
              </div>

              <div class="form-group">
                <label for="prod-category" class="form-label">Categoría *</label>
                <select
                  id="prod-category"
                  formControlName="categoryId"
                  class="form-control"
                  [class.is-invalid]="isFieldInvalid('categoryId')"
                  required
                >
                  <option value="" disabled>Seleccione una categoría</option>
                  @for (cat of categories(); track cat.id) {
                    <option [value]="cat.id" [disabled]="!cat.isActive && cat.id !== form.get('categoryId')?.value">
                      {{ cat.name }} {{ !cat.isActive ? '(Inactiva)' : '' }}
                    </option>
                  }
                </select>
                @if (isFieldInvalid('categoryId')) {
                  <div class="field-error">
                    Debe seleccionar una categoría válida.
                  </div>
                }
              </div>

              @if (editingProduct()) {
                <div class="form-group checkbox-group">
                  <label class="checkbox-label">
                    <input type="checkbox" formControlName="isActive" />
                    <span>Producto Activo (disponible para operaciones de inventario y ventas)</span>
                  </label>
                </div>
              }

              <div class="modal-footer">
                <button type="button" class="btn btn-secondary" (click)="closeFormModal()" [disabled]="isSubmitting()">
                  Cancelar
                </button>
                <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
                  @if (isSubmitting()) {
                    <span class="spinner" aria-hidden="true"></span>
                    <span>Guardando...</span>
                  } @else {
                    <span>{{ editingProduct() ? 'Guardar Cambios' : 'Crear Producto' }}</span>
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
    .page-container {
      display: flex;
      flex-direction: column;
      gap: var(--space-5);
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: var(--space-4);
    }

    .page-title {
      font-size: var(--font-size-2xl);
      font-weight: 700;
      color: var(--color-text);
      line-height: 1.2;
    }

    .page-subtitle {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      margin-top: var(--space-1);
    }

    .toolbar-card {
      padding: var(--space-3) var(--space-4);
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: var(--space-4);
    }

    .filters-row {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: var(--space-3);
    }

    .search-box {
      position: relative;
      display: flex;
      align-items: center;
      min-width: 260px;
    }

    .search-icon {
      position: absolute;
      left: var(--space-3);
      color: var(--color-text-muted);
      pointer-events: none;
    }

    .search-input {
      padding-left: 2.25rem;
      font-size: var(--font-size-sm);
      height: 38px;
    }

    .select-group {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    .filter-label {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      font-weight: 600;
      white-space: nowrap;
    }

    .select-control {
      height: 38px;
      font-size: var(--font-size-sm);
      padding: var(--space-1) var(--space-3);
      min-width: 170px;
    }

    .stats-row {
      display: flex;
      align-items: center;
    }

    .stat-pill {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      background-color: var(--color-surface-hover);
      padding: var(--space-1) var(--space-3);
      border-radius: var(--radius-full);
    }

    .state-card {
      padding: var(--space-10) var(--space-6);
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      gap: var(--space-3);
    }

    .state-title {
      font-size: var(--font-size-lg);
      font-weight: 600;
      color: var(--color-text);
    }

    .state-text {
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
      max-width: 480px;
      line-height: 1.5;
    }

    .empty-icon {
      color: var(--color-primary);
      margin-bottom: var(--space-1);
    }

    .table-container {
      padding: 0;
      overflow: hidden;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
      font-size: var(--font-size-sm);
    }

    .data-table th {
      background-color: var(--color-surface-hover);
      padding: var(--space-3) var(--space-4);
      font-size: var(--font-size-xs);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--color-text-muted);
      border-bottom: 1px solid var(--color-border);
    }

    .data-table td {
      padding: var(--space-3) var(--space-4);
      border-bottom: 1px solid var(--color-border);
      color: var(--color-text);
      vertical-align: middle;
    }

    .data-table tbody tr:hover {
      background-color: var(--color-surface-hover);
    }

    .product-sku {
      background-color: var(--color-surface-hover);
      padding: 0.2rem 0.45rem;
      border-radius: var(--radius-sm);
      font-size: var(--font-size-xs);
      border: 1px solid var(--color-border);
    }

    .font-mono {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    }

    .text-right {
      text-align: right;
    }

    .actions-group {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
    }

    .action-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border-radius: var(--radius-sm);
      border: 1px solid var(--color-border);
      background-color: var(--color-surface);
      color: var(--color-text-muted);
      cursor: pointer;
      transition: all var(--transition-fast);
    }

    .action-btn.edit-btn:hover {
      color: var(--color-primary);
      border-color: var(--color-primary);
      background-color: var(--color-primary-light);
    }

    .action-btn.toggle-btn:hover {
      color: var(--color-warning);
      border-color: var(--color-warning);
      background-color: var(--color-warning-light);
    }
  `]
})
export class ProductsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  public readonly authService = inject(AuthService);

  public readonly products = signal<ProductResponse[]>([]);
  public readonly categories = signal<CategoryResponse[]>([]);
  public readonly isLoading = signal<boolean>(false);
  public readonly errorMessage = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);

  // Filters
  public readonly searchTerm = signal<string>('');
  public readonly selectedCategoryFilter = signal<string>('');
  public readonly selectedStatusFilter = signal<string>('all');

  // Modal
  public readonly isFormModalOpen = signal<boolean>(false);
  public readonly editingProduct = signal<ProductResponse | null>(null);
  public readonly isSubmitting = signal<boolean>(false);
  public readonly formError = signal<string | null>(null);

  public readonly form = this.fb.nonNullable.group({
    identifier: ['', [Validators.required, Validators.maxLength(100)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    categoryId: ['', [Validators.required]],
    isActive: [true, [Validators.required]]
  });

  public readonly hasActiveFilters = computed(() => {
    return (
      this.searchTerm().trim().length > 0 ||
      this.selectedCategoryFilter().length > 0 ||
      this.selectedStatusFilter() !== 'all'
    );
  });

  public ngOnInit(): void {
    this.loadCategories();
    this.loadProducts();
  }

  public loadCategories(): void {
    this.categoryService.list().subscribe({
      next: (list) => this.categories.set(list),
      error: () => {
        // Categories list error handled gracefully without breaking products screen
      }
    });
  }

  public loadProducts(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const filter: ProductQueryFilter = {};

    if (this.selectedCategoryFilter()) {
      filter.categoryId = this.selectedCategoryFilter();
    }

    if (this.selectedStatusFilter() === 'active') {
      filter.isActive = true;
    } else if (this.selectedStatusFilter() === 'inactive') {
      filter.isActive = false;
    }

    if (this.searchTerm().trim()) {
      filter.searchTerm = this.searchTerm().trim();
    }

    this.productService.list(filter).subscribe({
      next: (list) => {
        this.products.set(list);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(parseApiError(err));
        this.isLoading.set(false);
      }
    });
  }

  public onSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm.set(target.value);
    this.loadProducts();
  }

  public onCategoryFilterChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedCategoryFilter.set(target.value);
    this.loadProducts();
  }

  public onStatusFilterChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedStatusFilter.set(target.value);
    this.loadProducts();
  }

  public resetFilters(): void {
    this.searchTerm.set('');
    this.selectedCategoryFilter.set('');
    this.selectedStatusFilter.set('all');
    this.loadProducts();
  }

  public isFieldInvalid(fieldName: 'identifier' | 'name' | 'categoryId'): boolean {
    const control = this.form.get(fieldName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  public openCreateModal(): void {
    this.editingProduct.set(null);
    this.formError.set(null);
    this.form.reset({
      identifier: '',
      name: '',
      categoryId: '',
      isActive: true
    });
    this.form.controls.identifier.enable();
    this.isFormModalOpen.set(true);
  }

  public openEditModal(product: ProductResponse): void {
    this.editingProduct.set(product);
    this.formError.set(null);
    this.form.reset({
      identifier: product.identifier,
      name: product.name,
      categoryId: product.categoryId,
      isActive: product.isActive
    });
    this.form.controls.identifier.disable();
    this.isFormModalOpen.set(true);
  }

  public closeFormModal(): void {
    this.isFormModalOpen.set(false);
    this.editingProduct.set(null);
    this.formError.set(null);
  }

  public onSaveProduct(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);

    const raw = this.form.getRawValue();
    const editing = this.editingProduct();

    if (editing) {
      this.productService
        .update(editing.identifier, {
          name: raw.name.trim(),
          categoryId: raw.categoryId,
          isActive: raw.isActive
        })
        .subscribe({
          next: (updated) => {
            this.products.update((current) =>
              current.map((p) => (p.identifier === updated.identifier ? updated : p))
            );
            this.isSubmitting.set(false);
            this.closeFormModal();
            this.showSuccess(`Producto "${updated.name}" actualizado con éxito.`);
          },
          error: (err) => {
            this.isSubmitting.set(false);
            this.formError.set(parseApiError(err));
          }
        });
    } else {
      this.productService
        .create({
          identifier: raw.identifier.trim().toUpperCase(),
          name: raw.name.trim(),
          categoryId: raw.categoryId
        })
        .subscribe({
          next: (created) => {
            this.products.update((current) => [...current, created]);
            this.isSubmitting.set(false);
            this.closeFormModal();
            this.showSuccess(`Producto "${created.name}" registrado con éxito.`);
          },
          error: (err) => {
            this.isSubmitting.set(false);
            this.formError.set(parseApiError(err));
          }
        });
    }
  }

  public toggleProductStatus(product: ProductResponse): void {
    const updatedStatus = !product.isActive;
    this.productService
      .update(product.identifier, {
        name: product.name,
        categoryId: product.categoryId,
        isActive: updatedStatus
      })
      .subscribe({
        next: (updated) => {
          this.products.update((current) =>
            current.map((p) => (p.identifier === updated.identifier ? updated : p))
          );
          const stateLabel = updatedStatus ? 'activado' : 'desactivado';
          this.showSuccess(`Producto "${product.name}" ${stateLabel}.`);
        },
        error: (err) => {
          this.errorMessage.set(parseApiError(err));
        }
      });
  }

  private showSuccess(msg: string): void {
    this.successMessage.set(msg);
    setTimeout(() => {
      this.successMessage.update((m) => (m === msg ? null : m));
    }, 4000);
  }
}
