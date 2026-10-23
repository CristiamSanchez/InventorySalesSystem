import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AuthService,
  CategoryResponse,
  CategoryService,
  InventoryMovementReportItemResponse,
  InventoryMovementReportResponse,
  InventoryMovementResponse,
  InventoryOperationType,
  InventoryService,
  InventoryStatusItemResponse,
  InventoryStatusReportResponse,
  parseApiError,
  SupplierResponse,
  SupplierService
} from '@core';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Control de Inventario</h1>
          <p class="page-subtitle">Estado de existencias en tiempo real y trazabilidad inmutable de movimientos</p>
        </div>
        <div class="page-actions" *ngIf="authService.isAdmin()">
          <button type="button" class="btn btn-primary" (click)="openOperationModal('entry')">
            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            Nueva Operación
          </button>
        </div>
      </div>

      <!-- Global Alerts -->
      <div *ngIf="successMessage()" class="alert alert-success" role="alert">
        <span>{{ successMessage() }}</span>
        <button type="button" class="alert-close" (click)="successMessage.set(null)">×</button>
      </div>

      <div *ngIf="errorMessage()" class="alert alert-danger" role="alert">
        <span>{{ errorMessage() }}</span>
        <button type="button" class="alert-close" (click)="errorMessage.set(null)">×</button>
      </div>

      <!-- Navigation Tabs -->
      <nav class="tabs-nav" aria-label="Secciones de Inventario">
        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'stock'"
          (click)="setTab('stock')">
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
            <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
            <line x1="12" y1="22.08" x2="12" y2="12"></line>
          </svg>
          Existencias de Catálogo
        </button>

        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'movements'"
          (click)="setTab('movements')">
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
          </svg>
          Trazabilidad de Movimientos
        </button>
      </nav>

      <!-- TAB 1: Stock Status -->
      <ng-container *ngIf="activeTab() === 'stock'">
        <!-- Metrics Row -->
        <div class="stats-row" *ngIf="stockReport() as report">
          <div class="stat-card">
            <span class="stat-value">{{ report.totalProducts }}</span>
            <span class="stat-label">Total Productos</span>
          </div>
          <div class="stat-card">
            <span class="stat-value text-success">{{ report.totalUnitsOnHand }}</span>
            <span class="stat-label">Unidades en Existencia</span>
          </div>
          <div class="stat-card">
            <span class="stat-value text-success">{{ report.inStockProductsCount }}</span>
            <span class="stat-label">Con Existencias</span>
          </div>
          <div class="stat-card">
            <span class="stat-value text-danger">{{ report.outOfStockProductsCount }}</span>
            <span class="stat-label">Sin Existencias</span>
          </div>
        </div>

        <!-- Filters Toolbar -->
        <div class="toolbar-card">
          <div class="filters-row">
            <div class="search-box">
              <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                <circle cx="11" cy="11" r="8"></circle>
                <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
              </svg>
              <input
                type="text"
                class="form-control"
                placeholder="Buscar por código o producto..."
                [ngModel]="stockSearchTerm()"
                (ngModelChange)="stockSearchTerm.set($event)" />
            </div>

            <div class="select-group">
              <label for="categoryFilter" class="select-label">Categoría:</label>
              <select
                id="categoryFilter"
                class="form-control"
                [ngModel]="selectedCategory()"
                (ngModelChange)="onCategoryFilterChange($event)">
                <option value="">Todas las categorías</option>
                <option *ngFor="let cat of categories()" [value]="cat.id">{{ cat.name }}</option>
              </select>
            </div>

            <div class="checkbox-group">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  [ngModel]="inStockOnly()"
                  (ngModelChange)="onInStockOnlyChange($event)" />
                Solo con stock disponible
              </label>
            </div>

            <button type="button" class="btn btn-secondary btn-sm" (click)="loadStockReport()" [disabled]="loadingStock()">
              <svg viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" stroke-width="2" fill="none">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              Actualizar
            </button>
          </div>
        </div>

        <!-- Loading State -->
        <div *ngIf="loadingStock()" class="card state-card">
          <div class="spinner"></div>
          <p class="state-text">Cargando estado de inventario...</p>
        </div>

        <!-- Stock Table -->
        <div *ngIf="!loadingStock() && filteredStockItems().length > 0" class="card table-container">
          <div class="table-responsive">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Producto</th>
                  <th>Categoría</th>
                  <th class="text-right">Cantidad</th>
                  <th>Estado</th>
                  <th>Registro</th>
                  <th class="text-right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of filteredStockItems()">
                  <td>
                    <span class="id-pill font-mono">{{ item.productIdentifier }}</span>
                  </td>
                  <td>
                    <strong>{{ item.productName }}</strong>
                  </td>
                  <td>
                    <span class="badge badge-neutral">{{ item.categoryName }}</span>
                  </td>
                  <td class="text-right">
                    <span
                      class="font-mono"
                      [class.text-success]="item.quantity > 0"
                      [class.text-danger]="item.quantity === 0"
                      style="font-weight: 700; font-size: 1rem;">
                      {{ item.quantity }}
                    </span>
                  </td>
                  <td>
                    <span
                      class="badge"
                      [class.badge-success]="item.stockStatus === 'InStock'"
                      [class.badge-danger]="item.stockStatus === 'OutOfStock'">
                      {{ item.stockStatus === 'InStock' ? 'Disponible' : 'Agotado' }}
                    </span>
                  </td>
                  <td>
                    <span
                      class="badge"
                      [class.badge-info]="item.hasInventoryRecord"
                      [class.badge-neutral]="!item.hasInventoryRecord">
                      {{ item.hasInventoryRecord ? 'Inicializado' : 'Sin registro' }}
                    </span>
                  </td>
                  <td class="text-right">
                    <div class="actions-group">
                      <button
                        type="button"
                        class="action-btn edit-btn"
                        title="Ver historial de movimientos"
                        (click)="viewProductMovements(item.productIdentifier)">
                        <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                          <polyline points="12 8 12 12 14 14"></polyline>
                          <path d="M3.05 11a9 9 0 1 1 .5 4m-.5 5v-5h5"></path>
                        </svg>
                      </button>

                      <button
                        *ngIf="authService.isAdmin()"
                        type="button"
                        class="action-btn edit-btn"
                        title="Registrar operación"
                        (click)="openOperationModal('entry', item.productIdentifier)">
                        <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                          <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Empty Stock State -->
        <div *ngIf="!loadingStock() && filteredStockItems().length === 0" class="card state-card">
          <div class="empty-icon">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
              <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
              <line x1="12" y1="22.08" x2="12" y2="12"></line>
            </svg>
          </div>
          <h2 class="state-title">No se encontraron productos</h2>
          <p class="state-text">
            No hay existencias que coincidan con los criterios seleccionados. Verifique el filtro de categoría o texto.
          </p>
        </div>
      </ng-container>

      <!-- TAB 2: Movements Traceability -->
      <ng-container *ngIf="activeTab() === 'movements'">
        <!-- Traceability Metrics Row -->
        <div class="stats-row" *ngIf="movementReport() as report">
          <div class="stat-card">
            <span class="stat-value">{{ report.totalMovements }}</span>
            <span class="stat-label">Total Movimientos</span>
          </div>
          <div class="stat-card">
            <span class="stat-value text-success">+{{ report.totalUnitsIn }}</span>
            <span class="stat-label">Unidades Ingresadas</span>
          </div>
          <div class="stat-card">
            <span class="stat-value text-danger">-{{ report.totalUnitsOut }}</span>
            <span class="stat-label">Unidades Egresadas</span>
          </div>
          <div class="stat-card">
            <span
              class="stat-value"
              [class.text-success]="report.netQuantityDelta >= 0"
              [class.text-danger]="report.netQuantityDelta < 0">
              {{ report.netQuantityDelta >= 0 ? '+' : '' }}{{ report.netQuantityDelta }}
            </span>
            <span class="stat-label">Balance Neto</span>
          </div>
        </div>

        <!-- Traceability Filters Toolbar -->
        <div class="toolbar-card">
          <div class="filters-row">
            <div class="select-group">
              <label for="fromUtcInput" class="select-label">Desde:</label>
              <input
                id="fromUtcInput"
                type="date"
                class="form-control"
                [ngModel]="fromDateInput()"
                (ngModelChange)="fromDateInput.set($event)" />
            </div>

            <div class="select-group">
              <label for="toUtcInput" class="select-label">Hasta:</label>
              <input
                id="toUtcInput"
                type="date"
                class="form-control"
                [ngModel]="toDateInput()"
                (ngModelChange)="toDateInput.set($event)" />
            </div>

            <div class="select-group">
              <label for="movementTypeFilter" class="select-label">Tipo:</label>
              <select
                id="movementTypeFilter"
                class="form-control"
                [ngModel]="movementType()"
                (ngModelChange)="movementType.set($event)">
                <option value="">Todos los tipos</option>
                <option value="Entry">Entrada</option>
                <option value="Exit">Salida</option>
                <option value="Adjustment">Ajuste</option>
              </select>
            </div>

            <div class="select-group">
              <label for="movementSupplierFilter" class="select-label">Proveedor:</label>
              <select
                id="movementSupplierFilter"
                class="form-control"
                [ngModel]="movementSupplierId()"
                (ngModelChange)="movementSupplierId.set($event)">
                <option value="">Todos los proveedores</option>
                <option *ngFor="let sup of suppliers()" [value]="sup.id">{{ sup.name }}</option>
              </select>
            </div>

            <div class="search-box">
              <input
                type="text"
                class="form-control"
                placeholder="Filtrar por código de producto..."
                [ngModel]="movementProduct()"
                (ngModelChange)="movementProduct.set($event)" />
            </div>

            <button type="button" class="btn btn-secondary btn-sm" (click)="loadMovementsReport()" [disabled]="loadingMovements()">
              <svg viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" stroke-width="2" fill="none">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              Filtrar
            </button>
          </div>
        </div>

        <!-- Loading State -->
        <div *ngIf="loadingMovements()" class="card state-card">
          <div class="spinner"></div>
          <p class="state-text">Cargando trazabilidad de movimientos...</p>
        </div>

        <!-- Movements Table -->
        <div *ngIf="!loadingMovements() && (movementReport()?.movements?.length ?? 0) > 0" class="card table-container">
          <div class="table-responsive">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Fecha UTC</th>
                  <th>Código Producto</th>
                  <th>Tipo</th>
                  <th class="text-right">Cantidad</th>
                  <th>Referencia / Motivo</th>
                  <th>Proveedor Snapshot</th>
                  <th>Usuario Responsable</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let m of movementReport()?.movements">
                  <td>
                    <span class="font-mono text-muted">{{ m.occurredAtUtc | date:'yyyy-MM-dd HH:mm' }} UTC</span>
                  </td>
                  <td>
                    <span class="id-pill font-mono">{{ m.productIdentifier }}</span>
                  </td>
                  <td>
                    <span
                      class="badge"
                      [class.badge-success]="m.movementType === 'Entry'"
                      [class.badge-warning]="m.movementType === 'Exit'"
                      [class.badge-info]="m.movementType === 'Adjustment'">
                      {{ m.movementType === 'Entry' ? 'Entrada' : m.movementType === 'Exit' ? 'Salida' : 'Ajuste' }}
                    </span>
                  </td>
                  <td class="text-right">
                    <span
                      class="font-mono"
                      [class.text-success]="m.movementType === 'Entry' || (m.movementType === 'Adjustment' && m.quantity > 0)"
                      [class.text-danger]="m.movementType === 'Exit' || (m.movementType === 'Adjustment' && m.quantity < 0)"
                      style="font-weight: 700;">
                      {{ m.movementType === 'Entry' ? '+' : m.movementType === 'Exit' ? '-' : (m.quantity > 0 ? '+' : '') }}{{ m.quantity }}
                    </span>
                  </td>
                  <td>{{ m.reference }}</td>
                  <td>
                    <span *ngIf="m.supplierName" class="badge badge-neutral">{{ m.supplierName }}</span>
                    <span *ngIf="!m.supplierName" class="text-muted">—</span>
                  </td>
                  <td>
                    <span *ngIf="m.userId" class="id-pill font-mono" [title]="m.userId">{{ m.userId.slice(0, 8) }}...</span>
                    <span *ngIf="!m.userId" class="text-muted">Sistema</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Empty Movements State -->
        <div *ngIf="!loadingMovements() && (movementReport()?.movements?.length ?? 0) === 0" class="card state-card">
          <div class="empty-icon">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
            </svg>
          </div>
          <h2 class="state-title">No hay movimientos registrados</h2>
          <p class="state-text">
            No se encontraron movimientos de inventario dentro del rango de fechas y filtros seleccionados.
          </p>
        </div>
      </ng-container>

      <!-- MODAL: Product Movements History -->
      <div *ngIf="showProductMovementsModal()" class="dialog-backdrop" (click)="closeProductMovementsModal()">
        <div class="dialog-card modal-card" (click)="$event.stopPropagation()" role="dialog" aria-modal="true">
          <div class="dialog-header">
            <div>
              <h2 class="dialog-title">Historial de Movimientos</h2>
              <p class="dialog-subtitle">
                Producto: <span class="id-pill font-mono">{{ selectedProductIdentifier() }}</span>
              </p>
            </div>
            <button type="button" class="btn-icon" (click)="closeProductMovementsModal()">×</button>
          </div>

          <div class="dialog-body">
            <div *ngIf="loadingProductMovements()" class="state-card" style="padding: var(--space-4);">
              <div class="spinner"></div>
              <p class="state-text">Consultando historial del producto...</p>
            </div>

            <div *ngIf="!loadingProductMovements() && (selectedProductMovements()?.length ?? 0) > 0" class="table-responsive">
              <table class="data-table">
                <thead>
                  <tr>
                    <th>Fecha UTC</th>
                    <th>Tipo</th>
                    <th class="text-right">Cantidad</th>
                    <th>Referencia</th>
                    <th>Proveedor</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let mv of selectedProductMovements()">
                    <td class="font-mono text-muted">{{ mv.occurredAtUtc | date:'yyyy-MM-dd HH:mm' }}</td>
                    <td>
                      <span
                        class="badge"
                        [class.badge-success]="mv.movementType === 'Entry'"
                        [class.badge-warning]="mv.movementType === 'Exit'"
                        [class.badge-info]="mv.movementType === 'Adjustment'">
                        {{ mv.movementType === 'Entry' ? 'Entrada' : mv.movementType === 'Exit' ? 'Salida' : 'Ajuste' }}
                      </span>
                    </td>
                    <td class="text-right">
                      <span
                        class="font-mono"
                        [class.text-success]="mv.movementType === 'Entry' || (mv.movementType === 'Adjustment' && mv.quantity > 0)"
                        [class.text-danger]="mv.movementType === 'Exit' || (mv.movementType === 'Adjustment' && mv.quantity < 0)"
                        style="font-weight: 700;">
                        {{ mv.movementType === 'Entry' ? '+' : mv.movementType === 'Exit' ? '-' : (mv.quantity > 0 ? '+' : '') }}{{ mv.quantity }}
                      </span>
                    </td>
                    <td>{{ mv.reference }}</td>
                    <td>
                      <span *ngIf="mv.supplierName" class="badge badge-neutral">{{ mv.supplierName }}</span>
                      <span *ngIf="!mv.supplierName" class="text-muted">—</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div *ngIf="!loadingProductMovements() && (selectedProductMovements()?.length ?? 0) === 0" class="state-card">
              <p class="state-text">Este producto no cuenta con movimientos registrados en el inventario.</p>
            </div>
          </div>

          <div class="dialog-footer">
            <button type="button" class="btn btn-secondary" (click)="closeProductMovementsModal()">Cerrar</button>
          </div>
        </div>
      </div>

      <!-- MODAL: Inventory Operation (Entry / Exit / Adjustment) -->
      <div *ngIf="showOperationModal()" class="dialog-backdrop" (click)="closeOperationModal()">
        <div class="dialog-card modal-card" (click)="$event.stopPropagation()" role="dialog" aria-modal="true">
          <div class="dialog-header">
            <div>
              <h2 class="dialog-title">Registrar Operación de Inventario</h2>
              <p class="dialog-subtitle">Actualice el inventario físico mediante operaciones auditadas</p>
            </div>
            <button type="button" class="btn-icon" (click)="closeOperationModal()">×</button>
          </div>

          <form [formGroup]="operationForm" (ngSubmit)="submitOperation()">
            <div class="dialog-body">
              <div *ngIf="operationError()" class="alert alert-danger" role="alert">
                <span>{{ operationError() }}</span>
              </div>

              <!-- Operation Type Switcher -->
              <div class="operation-switcher">
                <button
                  type="button"
                  class="op-type-btn"
                  [class.active]="operationType() === 'entry'"
                  (click)="setOperationType('entry')">
                  Entrada (+)
                </button>
                <button
                  type="button"
                  class="op-type-btn"
                  [class.active]="operationType() === 'exit'"
                  (click)="setOperationType('exit')">
                  Salida (-)
                </button>
                <button
                  type="button"
                  class="op-type-btn"
                  [class.active]="operationType() === 'adjustment'"
                  (click)="setOperationType('adjustment')">
                  Ajuste (±)
                </button>
              </div>

              <!-- Product Identifier -->
              <div class="form-group">
                <label for="opProductIdentifier" class="form-label">
                  Código de Producto <span class="required">*</span>
                </label>
                <input
                  id="opProductIdentifier"
                  type="text"
                  class="form-control"
                  formControlName="productIdentifier"
                  placeholder="Ej: PROD-101"
                  [class.is-invalid]="isFieldInvalid('productIdentifier')" />
                <span *ngIf="isFieldInvalid('productIdentifier')" class="field-error">
                  El código del producto es obligatorio.
                </span>
              </div>

              <!-- Quantity Input -->
              <div class="form-row">
                <div class="form-group flex-1">
                  <label for="opQuantity" class="form-label">
                    {{ operationType() === 'adjustment' ? 'Magnitud de Unidades' : 'Cantidad' }}
                    <span class="required">*</span>
                  </label>
                  <input
                    id="opQuantity"
                    type="number"
                    min="1"
                    class="form-control"
                    formControlName="quantity"
                    placeholder="Ej: 10"
                    [class.is-invalid]="isFieldInvalid('quantity')" />
                  <span *ngIf="isFieldInvalid('quantity')" class="field-error">
                    La cantidad debe ser un entero mayor a cero.
                  </span>
                </div>

                <!-- Adjustment Direction Selector -->
                <div *ngIf="operationType() === 'adjustment'" class="form-group flex-1">
                  <label for="adjustmentDirection" class="form-label">Dirección del Ajuste</label>
                  <select
                    id="adjustmentDirection"
                    class="form-control"
                    formControlName="adjustmentDirection">
                    <option value="increase">Aumento (+ sobrante en almacén)</option>
                    <option value="decrease">Disminución (- merma o pérdida)</option>
                  </select>
                </div>
              </div>

              <!-- Supplier Selector (Entry Only) -->
              <div *ngIf="operationType() === 'entry'" class="form-group">
                <label for="opSupplierId" class="form-label">Proveedor (Opcional)</label>
                <select id="opSupplierId" class="form-control" formControlName="supplierId">
                  <option value="">Sin proveedor asociado</option>
                  <option *ngFor="let sup of activeSuppliers()" [value]="sup.id">
                    {{ sup.name }} ({{ sup.identificationNumber || 'S/N' }})
                  </option>
                </select>
                <span class="field-hint">
                  Solo los proveedores activos pueden registrarse en recepciones de inventario.
                </span>
              </div>

              <!-- Reference / Document / Reason -->
              <div class="form-group">
                <label for="opReference" class="form-label">
                  {{ operationType() === 'adjustment' ? 'Motivo del Ajuste' : 'Documento / Referencia' }}
                  <span class="required">*</span>
                </label>
                <input
                  id="opReference"
                  type="text"
                  class="form-control"
                  formControlName="reference"
                  [placeholder]="operationType() === 'adjustment' ? 'Ej: Conteo físico semestral' : 'Ej: Factura F-1234 / Guía 0098'"
                  [class.is-invalid]="isFieldInvalid('reference')" />
                <span *ngIf="isFieldInvalid('reference')" class="field-error">
                  {{ operationType() === 'adjustment' ? 'El motivo del ajuste es obligatorio.' : 'La referencia es obligatoria.' }}
                </span>
              </div>
            </div>

            <div class="dialog-footer">
              <button type="button" class="btn btn-secondary" (click)="closeOperationModal()" [disabled]="submittingOperation()">
                Cancelar
              </button>
              <button
                type="submit"
                class="btn"
                [class.btn-primary]="operationType() === 'entry'"
                [class.btn-danger]="operationType() === 'exit'"
                [class.btn-warning]="operationType() === 'adjustment'"
                [disabled]="submittingOperation() || operationForm.invalid">
                <span *ngIf="submittingOperation()" class="spinner-sm"></span>
                <span>
                  {{ operationType() === 'entry' ? 'Registrar Entrada' : operationType() === 'exit' ? 'Registrar Salida' : 'Aplicar Ajuste' }}
                </span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .checkbox-group {
      display: inline-flex;
      align-items: center;
      padding: var(--space-2) 0;
    }

    .checkbox-label {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-sm);
      color: var(--color-text);
      cursor: pointer;
      user-select: none;
    }

    .operation-switcher {
      display: flex;
      gap: var(--space-2);
      margin-bottom: var(--space-4);
      background-color: var(--color-surface-hover);
      padding: var(--space-1);
      border-radius: var(--radius-md);
    }

    .op-type-btn {
      flex: 1;
      padding: var(--space-2) var(--space-3);
      border: none;
      background: transparent;
      font-size: var(--font-size-sm);
      font-weight: 500;
      color: var(--color-text-muted);
      border-radius: var(--radius-sm);
      cursor: pointer;
      transition: all var(--transition-fast);
      text-align: center;
    }

    .op-type-btn.active {
      background-color: var(--color-surface);
      color: var(--color-text);
      box-shadow: var(--shadow-sm);
      font-weight: 600;
    }

    .field-hint {
      display: block;
      margin-top: var(--space-1);
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
    }
  `]
})
export class InventoryComponent implements OnInit {
  public readonly authService = inject(AuthService);
  private readonly inventoryService = inject(InventoryService);
  private readonly categoryService = inject(CategoryService);
  private readonly supplierService = inject(SupplierService);
  private readonly fb = inject(FormBuilder);

  // Tab State
  public readonly activeTab = signal<'stock' | 'movements'>('stock');

  // Stock Status State
  public readonly stockReport = signal<InventoryStatusReportResponse | null>(null);
  public readonly loadingStock = signal<boolean>(false);
  public readonly selectedCategory = signal<string>('');
  public readonly inStockOnly = signal<boolean>(false);
  public readonly stockSearchTerm = signal<string>('');
  public readonly categories = signal<CategoryResponse[]>([]);

  public readonly filteredStockItems = computed<InventoryStatusItemResponse[]>(() => {
    const report = this.stockReport();
    if (!report) return [];

    const term = this.stockSearchTerm().trim().toLowerCase();
    if (!term) return report.items;

    return report.items.filter(
      item =>
        item.productIdentifier.toLowerCase().includes(term) ||
        item.productName.toLowerCase().includes(term)
    );
  });

  // Movement Traceability State
  public readonly movementReport = signal<InventoryMovementReportResponse | null>(null);
  public readonly loadingMovements = signal<boolean>(false);
  public readonly fromDateInput = signal<string>(this.calculateDefaultFromDate());
  public readonly toDateInput = signal<string>(this.calculateDefaultToDate());
  public readonly movementProduct = signal<string>('');
  public readonly movementType = signal<string>('');
  public readonly movementSupplierId = signal<string>('');
  public readonly suppliers = signal<SupplierResponse[]>([]);

  public readonly activeSuppliers = computed<SupplierResponse[]>(() =>
    this.suppliers().filter(s => s.isActive)
  );

  // Product Movement Modal State
  public readonly showProductMovementsModal = signal<boolean>(false);
  public readonly selectedProductIdentifier = signal<string | null>(null);
  public readonly selectedProductMovements = signal<InventoryMovementResponse[] | null>(null);
  public readonly loadingProductMovements = signal<boolean>(false);

  // Operation Modal State
  public readonly showOperationModal = signal<boolean>(false);
  public readonly operationType = signal<InventoryOperationType>('entry');
  public readonly submittingOperation = signal<boolean>(false);
  public readonly operationError = signal<string | null>(null);

  // Notifications
  public readonly successMessage = signal<string | null>(null);
  public readonly errorMessage = signal<string | null>(null);

  // Operation Form
  public readonly operationForm: FormGroup = this.fb.group({
    productIdentifier: ['', [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]],
    adjustmentDirection: ['increase'],
    reference: ['', [Validators.required]],
    supplierId: ['']
  });

  public ngOnInit(): void {
    this.loadCategories();
    this.loadSuppliers();
    this.loadStockReport();
  }

  public setTab(tab: 'stock' | 'movements'): void {
    this.activeTab.set(tab);
    if (tab === 'movements' && !this.movementReport()) {
      this.loadMovementsReport();
    }
  }

  public loadCategories(): void {
    this.categoryService.list().subscribe({
      next: cats => this.categories.set(cats),
      error: () => {}
    });
  }

  public loadSuppliers(): void {
    this.supplierService.list().subscribe({
      next: sups => this.suppliers.set(sups),
      error: () => {}
    });
  }

  public loadStockReport(): void {
    this.loadingStock.set(true);
    this.errorMessage.set(null);

    const query = {
      categoryId: this.selectedCategory() || null,
      inStockOnly: this.inStockOnly() || null
    };

    this.inventoryService.getStatusReport(query).subscribe({
      next: report => {
        this.stockReport.set(report);
        this.loadingStock.set(false);
      },
      error: err => {
        this.errorMessage.set(parseApiError(err));
        this.loadingStock.set(false);
      }
    });
  }

  public onCategoryFilterChange(categoryId: string): void {
    this.selectedCategory.set(categoryId);
    this.loadStockReport();
  }

  public onInStockOnlyChange(inStock: boolean): void {
    this.inStockOnly.set(inStock);
    this.loadStockReport();
  }

  public loadMovementsReport(): void {
    this.loadingMovements.set(true);
    this.errorMessage.set(null);

    // Convert local dates to UTC boundaries
    const fromIso = `${this.fromDateInput()}T00:00:00.000Z`;
    const toIso = `${this.toDateInput()}T23:59:59.999Z`;

    this.inventoryService
      .getMovementReport({
        fromUtc: fromIso,
        toUtc: toIso,
        productIdentifier: this.movementProduct() || null,
        movementType: this.movementType() || null,
        supplierId: this.movementSupplierId() || null
      })
      .subscribe({
        next: report => {
          this.movementReport.set(report);
          this.loadingMovements.set(false);
        },
        error: err => {
          this.errorMessage.set(parseApiError(err));
          this.loadingMovements.set(false);
        }
      });
  }

  public viewProductMovements(productIdentifier: string): void {
    this.selectedProductIdentifier.set(productIdentifier);
    this.selectedProductMovements.set(null);
    this.loadingProductMovements.set(true);
    this.showProductMovementsModal.set(true);

    this.inventoryService.getMovementsByProduct(productIdentifier).subscribe({
      next: movements => {
        this.selectedProductMovements.set(movements);
        this.loadingProductMovements.set(false);
      },
      error: err => {
        this.errorMessage.set(parseApiError(err));
        this.loadingProductMovements.set(false);
      }
    });
  }

  public closeProductMovementsModal(): void {
    this.showProductMovementsModal.set(false);
    this.selectedProductIdentifier.set(null);
    this.selectedProductMovements.set(null);
  }

  public openOperationModal(type: InventoryOperationType = 'entry', productIdentifier?: string): void {
    this.operationType.set(type);
    this.operationError.set(null);
    this.operationForm.reset({
      productIdentifier: productIdentifier || '',
      quantity: 1,
      adjustmentDirection: 'increase',
      reference: '',
      supplierId: ''
    });
    this.showOperationModal.set(true);
  }

  public setOperationType(type: InventoryOperationType): void {
    this.operationType.set(type);
    this.operationError.set(null);
  }

  public closeOperationModal(): void {
    this.showOperationModal.set(false);
    this.operationError.set(null);
    this.submittingOperation.set(false);
  }

  public isFieldInvalid(fieldName: string): boolean {
    const control = this.operationForm.get(fieldName);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  public submitOperation(): void {
    if (this.operationForm.invalid) {
      this.operationForm.markAllAsTouched();
      return;
    }

    this.submittingOperation.set(true);
    this.operationError.set(null);

    const formVal = this.operationForm.value;
    const prodId = formVal.productIdentifier.trim();
    const qty = Number(formVal.quantity);
    const ref = formVal.reference.trim();

    if (this.operationType() === 'entry') {
      const supplierId = formVal.supplierId ? formVal.supplierId : null;
      this.inventoryService
        .recordEntry({
          productIdentifier: prodId,
          quantity: qty,
          reference: ref,
          supplierId
        })
        .subscribe({
          next: resp => {
            this.handleOperationSuccess(
              `Entrada registrada con éxito para "${resp.productIdentifier}". Nuevo stock: ${resp.quantity} unidades.`
            );
          },
          error: err => this.handleOperationError(err)
        });
    } else if (this.operationType() === 'exit') {
      this.inventoryService
        .recordExit({
          productIdentifier: prodId,
          quantity: qty,
          reference: ref
        })
        .subscribe({
          next: resp => {
            this.handleOperationSuccess(
              `Salida registrada con éxito para "${resp.productIdentifier}". Nuevo stock: ${resp.quantity} unidades.`
            );
          },
          error: err => this.handleOperationError(err)
        });
    } else if (this.operationType() === 'adjustment') {
      const delta = formVal.adjustmentDirection === 'decrease' ? -Math.abs(qty) : Math.abs(qty);
      this.inventoryService
        .recordAdjustment({
          productIdentifier: prodId,
          quantityDelta: delta,
          reason: ref
        })
        .subscribe({
          next: resp => {
            this.handleOperationSuccess(
              `Ajuste aplicado con éxito para "${resp.productIdentifier}". Nuevo stock: ${resp.quantity} unidades.`
            );
          },
          error: err => this.handleOperationError(err)
        });
    }
  }

  private handleOperationSuccess(message: string): void {
    this.submittingOperation.set(false);
    this.closeOperationModal();
    this.successMessage.set(message);

    // Refresh state without optimistic updates
    this.loadStockReport();
    if (this.activeTab() === 'movements') {
      this.loadMovementsReport();
    }
  }

  private handleOperationError(err: unknown): void {
    this.submittingOperation.set(false);
    this.operationError.set(parseApiError(err));
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
