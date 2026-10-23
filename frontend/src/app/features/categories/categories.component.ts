import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, CategoryResponse, CategoryService, parseApiError } from '@core';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Gestión de Categorías</h1>
          <p class="page-subtitle">Catálogo de categorías para la clasificación de productos</p>
        </div>
        <div class="page-actions">
          @if (authService.isAdmin()) {
            <button type="button" class="btn btn-primary" (click)="openCreateModal()">
              <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none" aria-hidden="true">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>Nueva Categoría</span>
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
          <button type="button" class="btn btn-sm btn-secondary" (click)="loadCategories()">Reintentar</button>
        </div>
      }

      <!-- Metrics & Search Toolbar -->
      <div class="card toolbar-card">
        <div class="stats-row">
          <div class="stat-pill">
            <span class="stat-num">{{ totalCount() }}</span>
            <span class="stat-label">Total</span>
          </div>
          <div class="stat-pill">
            <span class="stat-num text-success">{{ activeCount() }}</span>
            <span class="stat-label">Activas</span>
          </div>
          <div class="stat-pill">
            <span class="stat-num text-muted">{{ inactiveCount() }}</span>
            <span class="stat-label">Inactivas</span>
          </div>
        </div>

        <div class="search-box">
          <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          <input
            type="search"
            class="form-control search-input"
            placeholder="Buscar por nombre..."
            [value]="searchTerm()"
            (input)="onSearchInput($event)"
            aria-label="Buscar categorías"
          />
        </div>
      </div>

      <!-- Main Content / Table -->
      @if (isLoading()) {
        <div class="card state-card">
          <div class="spinner-large" aria-hidden="true"></div>
          <p class="state-text">Cargando catálogo de categorías...</p>
        </div>
      } @else if (filteredCategories().length === 0) {
        <div class="card state-card">
          <div class="empty-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <line x1="8" y1="6" x2="21" y2="6"></line>
              <line x1="8" y1="12" x2="21" y2="12"></line>
              <line x1="8" y1="18" x2="21" y2="18"></line>
              <line x1="3" y1="6" x2="3.01" y2="6"></line>
              <line x1="3" y1="12" x2="3.01" y2="12"></line>
              <line x1="3" y1="18" x2="3.01" y2="18"></line>
            </svg>
          </div>
          @if (searchTerm().trim().length > 0) {
            <h3 class="state-title">Sin resultados de búsqueda</h3>
            <p class="state-text">No se encontraron categorías que coincidan con "{{ searchTerm() }}".</p>
            <button type="button" class="btn btn-secondary btn-sm" (click)="searchTerm.set('')">Limpiar búsqueda</button>
          } @else {
            <h3 class="state-title">No hay categorías registradas</h3>
            <p class="state-text">Comience creando la primera categoría para organizar su catálogo de productos.</p>
            @if (authService.isAdmin()) {
              <button type="button" class="btn btn-primary btn-sm" (click)="openCreateModal()">Crear Categoría</button>
            }
          }
        </div>
      } @else {
        <div class="card table-container">
          <div class="table-responsive">
            <table class="data-table" aria-label="Listado de categorías">
              <thead>
                <tr>
                  <th scope="col">Nombre</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Productos Asociados</th>
                  @if (authService.isAdmin()) {
                    <th scope="col" class="text-right">Acciones</th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (cat of filteredCategories(); track cat.id) {
                  <tr>
                    <td class="font-semibold">{{ cat.name }}</td>
                    <td>
                      <span class="badge" [class.badge-success]="cat.isActive" [class.badge-neutral]="!cat.isActive">
                        {{ cat.isActive ? 'Activa' : 'Inactiva' }}
                      </span>
                    </td>
                    <td>
                      <span class="products-count">
                        {{ cat.products.length }} {{ cat.products.length === 1 ? 'producto' : 'productos' }}
                      </span>
                    </td>
                    @if (authService.isAdmin()) {
                      <td class="text-right">
                        <div class="actions-group">
                          <button
                            type="button"
                            class="action-btn edit-btn"
                            (click)="openEditModal(cat)"
                            aria-label="Editar categoría {{ cat.name }}"
                            title="Editar"
                          >
                            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                            </svg>
                          </button>
                          <button
                            type="button"
                            class="action-btn delete-btn"
                            (click)="openDeleteModal(cat)"
                            aria-label="Eliminar categoría {{ cat.name }}"
                            title="Eliminar"
                          >
                            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                              <polyline points="3 6 5 6 21 6"></polyline>
                              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                            </svg>
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
          <div class="modal-card" (click)="$event.stopPropagation()" role="dialog" [attr.aria-label]="editingCategory() ? 'Editar Categoría' : 'Nueva Categoría'">
            <div class="modal-header">
              <h2 class="modal-title">{{ editingCategory() ? 'Editar Categoría' : 'Nueva Categoría' }}</h2>
              <button type="button" class="modal-close-btn" (click)="closeFormModal()" aria-label="Cerrar ventana">&times;</button>
            </div>

            @if (formError()) {
              <div class="alert alert-danger modal-alert" role="alert">
                <span>{{ formError() }}</span>
              </div>
            }

            <form [formGroup]="form" (ngSubmit)="onSaveCategory()" novalidate class="modal-form">
              <div class="form-group">
                <label for="category-name" class="form-label">Nombre de la Categoría *</label>
                <input
                  id="category-name"
                  type="text"
                  formControlName="name"
                  class="form-control"
                  [class.is-invalid]="isFieldInvalid('name')"
                  placeholder="Ej. Bebidas, Alimentos, Limpieza..."
                  maxlength="100"
                  required
                />
                @if (isFieldInvalid('name')) {
                  <div class="field-error">
                    El nombre es requerido (máximo 100 caracteres).
                  </div>
                }
              </div>

              @if (editingCategory()) {
                <div class="form-group checkbox-group">
                  <label class="checkbox-label">
                    <input type="checkbox" formControlName="isActive" />
                    <span>Categoría Activa (disponible para nuevos productos)</span>
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
                    <span>{{ editingCategory() ? 'Guardar Cambios' : 'Crear Categoría' }}</span>
                  }
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Delete Confirmation Modal -->
      @if (isDeleteModalOpen() && categoryToDelete()) {
        <div class="modal-backdrop" (click)="closeDeleteModal()">
          <div class="modal-card delete-card" (click)="$event.stopPropagation()" role="alertdialog" aria-labelledby="delete-title">
            <div class="modal-header">
              <h2 id="delete-title" class="modal-title text-danger">Eliminar Categoría</h2>
              <button type="button" class="modal-close-btn" (click)="closeDeleteModal()" aria-label="Cerrar ventana">&times;</button>
            </div>

            @if (deleteError()) {
              <div class="alert alert-danger modal-alert" role="alert">
                <span>{{ deleteError() }}</span>
              </div>
            }

            <div class="delete-body">
              <p>¿Está seguro de que desea eliminar la categoría <strong>{{ categoryToDelete()?.name }}</strong>?</p>
              @if ((categoryToDelete()?.products?.length ?? 0) > 0) {
                <div class="alert alert-warning" role="alert" style="margin-top: var(--space-3);">
                  <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none" aria-hidden="true">
                    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
                    <line x1="12" y1="9" x2="12" y2="13"></line>
                    <line x1="12" y1="17" x2="12.01" y2="17"></line>
                  </svg>
                  <span>Esta categoría tiene {{ categoryToDelete()?.products?.length }} producto(s) asociado(s). La operación será rechazada por el servidor.</span>
                </div>
              }
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeDeleteModal()" [disabled]="isDeleting()">
                Cancelar
              </button>
              <button type="button" class="btn btn-danger" (click)="onConfirmDelete()" [disabled]="isDeleting()">
                @if (isDeleting()) {
                  <span class="spinner" aria-hidden="true"></span>
                  <span>Eliminando...</span>
                } @else {
                  <span>Eliminar Definitivamente</span>
                }
              </button>
            </div>
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

    .stats-row {
      display: flex;
      align-items: center;
      gap: var(--space-3);
    }

    .stat-pill {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-sm);
    }

    .stat-num {
      font-weight: 700;
      font-size: var(--font-size-base);
    }

    .stat-label {
      color: var(--color-text-muted);
      font-size: var(--font-size-xs);
      text-transform: uppercase;
      letter-spacing: 0.04em;
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

    .text-right {
      text-align: right;
    }

    .text-danger {
      color: var(--color-danger);
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

    .action-btn.delete-btn:hover {
      color: var(--color-danger);
      border-color: var(--color-danger);
      background-color: var(--color-danger-light);
    }

    .delete-body {
      padding: var(--space-5);
      font-size: var(--font-size-sm);
      color: var(--color-text);
      line-height: 1.5;
    }
  `]
})
export class CategoriesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);
  public readonly authService = inject(AuthService);

  public readonly categories = signal<CategoryResponse[]>([]);
  public readonly isLoading = signal<boolean>(false);
  public readonly errorMessage = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);
  public readonly searchTerm = signal<string>('');

  // Create / Edit modal state
  public readonly isFormModalOpen = signal<boolean>(false);
  public readonly editingCategory = signal<CategoryResponse | null>(null);
  public readonly isSubmitting = signal<boolean>(false);
  public readonly formError = signal<string | null>(null);

  // Delete modal state
  public readonly isDeleteModalOpen = signal<boolean>(false);
  public readonly categoryToDelete = signal<CategoryResponse | null>(null);
  public readonly isDeleting = signal<boolean>(false);
  public readonly deleteError = signal<string | null>(null);

  public readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    isActive: [true, [Validators.required]]
  });

  public readonly filteredCategories = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const list = this.categories();
    if (!term) {
      return list;
    }
    return list.filter((cat) => cat.name.toLowerCase().includes(term));
  });

  public readonly totalCount = computed(() => this.categories().length);
  public readonly activeCount = computed(() => this.categories().filter((c) => c.isActive).length);
  public readonly inactiveCount = computed(() => this.categories().filter((c) => !c.isActive).length);

  public ngOnInit(): void {
    this.loadCategories();
  }

  public loadCategories(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.categoryService.list().subscribe({
      next: (list) => {
        this.categories.set(list);
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
  }

  public isFieldInvalid(fieldName: 'name' | 'isActive'): boolean {
    const control = this.form.get(fieldName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  public openCreateModal(): void {
    this.editingCategory.set(null);
    this.formError.set(null);
    this.form.reset({ name: '', isActive: true });
    this.isFormModalOpen.set(true);
  }

  public openEditModal(category: CategoryResponse): void {
    this.editingCategory.set(category);
    this.formError.set(null);
    this.form.reset({
      name: category.name,
      isActive: category.isActive
    });
    this.isFormModalOpen.set(true);
  }

  public closeFormModal(): void {
    this.isFormModalOpen.set(false);
    this.editingCategory.set(null);
    this.formError.set(null);
  }

  public onSaveCategory(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);

    const { name, isActive } = this.form.getRawValue();
    const editing = this.editingCategory();

    if (editing) {
      this.categoryService.update(editing.id, { name: name.trim(), isActive }).subscribe({
        next: (updated) => {
          this.categories.update((current) =>
            current.map((c) => (c.id === updated.id ? updated : c))
          );
          this.isSubmitting.set(false);
          this.closeFormModal();
          this.showSuccess(`Categoría "${updated.name}" actualizada con éxito.`);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.formError.set(parseApiError(err));
        }
      });
    } else {
      this.categoryService.create({ name: name.trim() }).subscribe({
        next: (created) => {
          this.categories.update((current) => [...current, created]);
          this.isSubmitting.set(false);
          this.closeFormModal();
          this.showSuccess(`Categoría "${created.name}" creada con éxito.`);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.formError.set(parseApiError(err));
        }
      });
    }
  }

  public openDeleteModal(category: CategoryResponse): void {
    this.categoryToDelete.set(category);
    this.deleteError.set(null);
    this.isDeleteModalOpen.set(true);
  }

  public closeDeleteModal(): void {
    this.isDeleteModalOpen.set(false);
    this.categoryToDelete.set(null);
    this.deleteError.set(null);
  }

  public onConfirmDelete(): void {
    const category = this.categoryToDelete();
    if (!category) {
      return;
    }

    this.isDeleting.set(true);
    this.deleteError.set(null);

    this.categoryService.delete(category.id).subscribe({
      next: () => {
        this.categories.update((current) => current.filter((c) => c.id !== category.id));
        this.isDeleting.set(false);
        this.closeDeleteModal();
        this.showSuccess(`Categoría "${category.name}" eliminada.`);
      },
      error: (err) => {
        this.isDeleting.set(false);
        this.deleteError.set(parseApiError(err));
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
