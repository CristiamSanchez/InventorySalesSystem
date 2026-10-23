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
  AuthService,
  CategoryResponse,
  CategoryService,
  CustomerResponse,
  CustomerService,
  InventoryMovementReportResponse,
  InventoryStatusItemResponse,
  InventoryStatusReportResponse,
  parseApiError,
  ProductResponse,
  ProductSalesReportResponse,
  ProductService,
  ReportService,
  ReportTab,
  SalesSummaryReportResponse,
  SupplierResponse,
  SupplierService
} from '@core';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Módulo de Reportes y Analítica</h1>
          <p class="page-subtitle">Informes consolidados de ventas, rendimiento de productos y trazabilidad de inventario</p>
        </div>
        <div class="page-actions">
          <button type="button" class="btn btn-secondary btn-sm" (click)="printReport()">
            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
              <polyline points="6 9 6 2 18 2 18 9"></polyline>
              <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
              <rect x="6" y="14" width="12" height="8"></rect>
            </svg>
            Imprimir Informe
          </button>
        </div>
      </div>

      <!-- Navigation Tabs -->
      <nav class="tabs-nav" aria-label="Secciones de Reportes">
        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'sales-summary'"
          (click)="setTab('sales-summary')">
          <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
            <line x1="12" y1="1" x2="12" y2="23"></line>
            <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
          </svg>
          Resumen de Ventas
          <span *ngIf="!authService.isAdmin()" class="badge badge-warning" style="font-size: 10px;">Admin</span>
        </button>

        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'product-sales'"
          (click)="setTab('product-sales')">
          <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
            <polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline>
            <polyline points="17 6 23 6 23 12"></polyline>
          </svg>
          Rendimiento de Productos
          <span *ngIf="!authService.isAdmin()" class="badge badge-warning" style="font-size: 10px;">Admin</span>
        </button>

        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'inventory-status'"
          (click)="setTab('inventory-status')">
          <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
          </svg>
          Estado de Stock
        </button>

        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'inventory-movements'"
          (click)="setTab('inventory-movements')">
          <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
            <polyline points="17 1 21 5 17 9"></polyline>
            <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
            <polyline points="7 23 3 19 7 15"></polyline>
            <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
          </svg>
          Trazabilidad de Movimientos
        </button>
      </nav>

      <!-- Global Error Alert -->
      <div *ngIf="errorMessage()" class="alert alert-danger" role="alert">
        <div class="alert-content">
          <p class="alert-message">{{ errorMessage() }}</p>
        </div>
        <button type="button" class="btn-close" (click)="errorMessage.set(null)" aria-label="Cerrar alerta">×</button>
      </div>

      <!-- =================================================================== -->
      <!-- TAB 1: SALES SUMMARY REPORT (ADMIN ONLY)                           -->
      <!-- =================================================================== -->
      <div *ngIf="activeTab() === 'sales-summary'">
        <!-- Non-admin permission barrier -->
        <div *ngIf="!authService.isAdmin()" class="card restriction-card">
          <div class="restriction-icon text-warning">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="2" fill="none">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
          </div>
          <h2 class="restriction-title">Acceso Restringido a Administradores</h2>
          <p class="restriction-text">
            El Resumen Financiero de Ventas contiene métricas confidenciales de ingresos y recaudación monetaria.
            Esta sección está reservada exclusivamente para usuarios con rol de Administrador.
          </p>
          <button type="button" class="btn btn-primary btn-sm" (click)="setTab('inventory-status')">
            Ver Estado de Stock Operativo
          </button>
        </div>

        <!-- Admin Content -->
        <ng-container *ngIf="authService.isAdmin()">
          <!-- Filters Card -->
          <div class="card toolbar-card">
            <form (ngSubmit)="loadSalesSummary()" class="filters-row">
              <div class="form-group">
                <label for="salesFromDate" class="form-label">Desde (UTC) <span class="required">*</span></label>
                <input
                  type="date"
                  id="salesFromDate"
                  class="form-control"
                  [ngModel]="salesFromDate()"
                  (ngModelChange)="salesFromDate.set($event)"
                  name="salesFromDate"
                  required />
              </div>

              <div class="form-group">
                <label for="salesToDate" class="form-label">Hasta (UTC) <span class="required">*</span></label>
                <input
                  type="date"
                  id="salesToDate"
                  class="form-control"
                  [ngModel]="salesToDate()"
                  (ngModelChange)="salesToDate.set($event)"
                  name="salesToDate"
                  required />
              </div>

              <div class="form-group">
                <label for="salesCustomerSelect" class="form-label">Cliente</label>
                <select
                  id="salesCustomerSelect"
                  class="form-control"
                  [ngModel]="salesCustomerId()"
                  (ngModelChange)="salesCustomerId.set($event)"
                  name="salesCustomerId">
                  <option value="">Todos los clientes</option>
                  <option *ngFor="let c of customers()" [value]="c.id">
                    {{ c.name }} ({{ c.identificationNumber }})
                  </option>
                </select>
              </div>

              <div class="form-group">
                <label for="salesStatusSelect" class="form-label">Estado</label>
                <select
                  id="salesStatusSelect"
                  class="form-control"
                  [ngModel]="salesStatus()"
                  (ngModelChange)="salesStatus.set($event)"
                  name="salesStatus">
                  <option value="">Todos los estados</option>
                  <option value="Confirmed">Confirmada</option>
                  <option value="Draft">Borrador</option>
                  <option value="Cancelled">Cancelada</option>
                </select>
              </div>

              <div class="form-group">
                <label for="salesUserIdInput" class="form-label">ID Cajero / Usuario</label>
                <input
                  type="text"
                  id="salesUserIdInput"
                  class="form-control font-mono"
                  placeholder="Opcional GUID..."
                  [ngModel]="salesUserId()"
                  (ngModelChange)="salesUserId.set($event)"
                  name="salesUserId" />
              </div>

              <div class="actions-group" style="align-self: flex-end;">
                <button type="submit" class="btn btn-primary" [disabled]="loadingSales()">
                  <span *ngIf="loadingSales()" class="spinner-sm" aria-hidden="true"></span>
                  Filtrar
                </button>
                <button type="button" class="btn btn-secondary" (click)="resetSalesFilters()">
                  Restablecer
                </button>
              </div>
            </form>
          </div>

          <!-- Summary Metrics Cards -->
          <div *ngIf="salesReport() as report" class="state-card" style="margin-bottom: var(--space-4);">
            <div class="stat-card">
              <span class="stat-label">Total Transacciones</span>
              <span class="stat-value font-mono">{{ report.totalTransactions }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">
                {{ report.confirmedTransactions }} confirmadas | {{ report.draftTransactions }} borradores | {{ report.cancelledTransactions }} canc.
              </span>
            </div>

            <div class="stat-card">
              <span class="stat-label">Subtotal Confirmado</span>
              <span class="stat-value font-mono">{{ report.totalSubtotal | currency:'USD' }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">Base neta sin impuestos</span>
            </div>

            <div class="stat-card">
              <span class="stat-label">Impuestos (IVA)</span>
              <span class="stat-value font-mono">{{ report.totalTax | currency:'USD' }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">Total fiscal recaudado</span>
            </div>

            <div class="stat-card" style="border-left: 4px solid var(--color-success);">
              <span class="stat-label">Ingresos Realizados</span>
              <span class="stat-value font-mono text-success">{{ report.totalRevenue | currency:'USD' }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">
                Solo ventas Confirmadas
              </span>
            </div>
          </div>

          <!-- Semantics Warning Callout -->
          <div class="card" style="padding: var(--space-3) var(--space-4); margin-bottom: var(--space-4); background-color: var(--color-surface-hover);">
            <p style="font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0;">
              <strong>Regla de Dominio:</strong> Únicamente las ventas <em>Confirmadas</em> generan ingresos monetarios realizados.
              Los borradores y ventas canceladas se contabilizan con propósitos de auditoría pero aportan \$0.00 al ingreso total.
            </p>
          </div>

          <!-- Loading State -->
          <div *ngIf="loadingSales()" class="card loading-card">
            <div class="spinner"></div>
            <p>Generando resumen de ventas...</p>
          </div>

          <!-- Sales Table Card -->
          <div *ngIf="!loadingSales() && salesReport() as report" class="card table-card">
            <div class="table-container">
              <table class="data-table">
                <thead>
                  <tr>
                    <th>Folio / ID</th>
                    <th>Fecha (UTC)</th>
                    <th>Estado</th>
                    <th>Cliente</th>
                    <th>Cajero / Usuario</th>
                    <th class="text-right">Unidades</th>
                    <th class="text-right">Líneas</th>
                    <th class="text-right">Subtotal</th>
                    <th class="text-right">IVA</th>
                    <th class="text-right">Total</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let s of report.sales">
                    <td>
                      <span class="id-pill font-mono" [title]="s.id">{{ s.id }}</span>
                    </td>
                    <td class="font-mono" style="font-size: var(--font-size-xs);">
                      {{ s.createdAtUtc | date:'yyyy-MM-dd HH:mm' }}
                    </td>
                    <td>
                      <span
                        class="badge"
                        [ngClass]="{
                          'badge-success': s.status === 'Confirmed',
                          'badge-warning': s.status === 'Draft',
                          'badge-danger': s.status === 'Cancelled'
                        }">
                        {{ s.status === 'Confirmed' ? 'CONFIRMADA' : (s.status === 'Draft' ? 'BORRADOR' : 'CANCELADA') }}
                      </span>
                    </td>
                    <td>
                      <strong>{{ s.customerReference }}</strong>
                    </td>
                    <td>
                      <span class="id-pill font-mono" [title]="s.userId">
                        {{ s.userId }}
                      </span>
                    </td>
                    <td class="text-right font-mono">{{ s.totalUnits }}</td>
                    <td class="text-right font-mono">{{ s.lineCount }}</td>
                    <td class="text-right font-mono">{{ s.subtotal | currency:'USD' }}</td>
                    <td class="text-right font-mono">{{ s.taxAmount | currency:'USD' }}</td>
                    <td class="text-right font-mono" style="font-weight: 600;">{{ s.total | currency:'USD' }}</td>
                  </tr>

                  <tr *ngIf="report.sales.length === 0">
                    <td colspan="10" class="text-center text-muted" style="padding: var(--space-8);">
                      No se encontraron transacciones de venta en el rango de fechas seleccionado.
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </ng-container>
      </div>

      <!-- =================================================================== -->
      <!-- TAB 2: PRODUCT SALES PERFORMANCE (ADMIN ONLY)                      -->
      <!-- =================================================================== -->
      <div *ngIf="activeTab() === 'product-sales'">
        <!-- Non-admin permission barrier -->
        <div *ngIf="!authService.isAdmin()" class="card restriction-card">
          <div class="restriction-icon text-warning">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="2" fill="none">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
          </div>
          <h2 class="restriction-title">Acceso Restringido a Administradores</h2>
          <p class="restriction-text">
            El informe de Rendimiento de Productos detalla recaudación monetaria y volúmenes de venta.
            Requiere rol de Administrador.
          </p>
          <button type="button" class="btn btn-primary btn-sm" (click)="setTab('inventory-status')">
            Ver Estado de Stock Operativo
          </button>
        </div>

        <!-- Admin Content -->
        <ng-container *ngIf="authService.isAdmin()">
          <!-- Filters Card -->
          <div class="card toolbar-card">
            <form (ngSubmit)="loadProductSales()" class="filters-row">
              <div class="form-group">
                <label for="prodSalesFromDate" class="form-label">Desde (UTC) <span class="required">*</span></label>
                <input
                  type="date"
                  id="prodSalesFromDate"
                  class="form-control"
                  [ngModel]="productFromDate()"
                  (ngModelChange)="productFromDate.set($event)"
                  name="productFromDate"
                  required />
              </div>

              <div class="form-group">
                <label for="prodSalesToDate" class="form-label">Hasta (UTC) <span class="required">*</span></label>
                <input
                  type="date"
                  id="prodSalesToDate"
                  class="form-control"
                  [ngModel]="productToDate()"
                  (ngModelChange)="productToDate.set($event)"
                  name="productToDate"
                  required />
              </div>

              <div class="form-group">
                <label for="prodSalesCategorySelect" class="form-label">Categoría</label>
                <select
                  id="prodSalesCategorySelect"
                  class="form-control"
                  [ngModel]="productCategoryId()"
                  (ngModelChange)="productCategoryId.set($event)"
                  name="productCategoryId">
                  <option value="">Todas las categorías</option>
                  <option *ngFor="let cat of categories()" [value]="cat.id">
                    {{ cat.name }}
                  </option>
                </select>
              </div>

              <div class="form-group">
                <label for="prodSalesProductSelect" class="form-label">Producto Específico</label>
                <select
                  id="prodSalesProductSelect"
                  class="form-control"
                  [ngModel]="productIdentifier()"
                  (ngModelChange)="productIdentifier.set($event)"
                  name="productIdentifier">
                  <option value="">Todos los productos</option>
                  <option *ngFor="let p of products()" [value]="p.identifier">
                    {{ p.name }} ({{ p.identifier }})
                  </option>
                </select>
              </div>

              <div class="actions-group" style="align-self: flex-end;">
                <button type="submit" class="btn btn-primary" [disabled]="loadingProducts()">
                  <span *ngIf="loadingProducts()" class="spinner-sm" aria-hidden="true"></span>
                  Filtrar
                </button>
                <button type="button" class="btn btn-secondary" (click)="resetProductSalesFilters()">
                  Restablecer
                </button>
              </div>
            </form>
          </div>

          <!-- Product Sales Metrics Cards -->
          <div *ngIf="productSalesReport() as report" class="state-card" style="margin-bottom: var(--space-4);">
            <div class="stat-card">
              <span class="stat-label">Productos con Venta</span>
              <span class="stat-value font-mono">{{ report.totalProductsSold }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">Artículos distintos comercializados</span>
            </div>

            <div class="stat-card">
              <span class="stat-label">Unidades Vendidas</span>
              <span class="stat-value font-mono">{{ report.totalUnitsSold }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">Volumen físico total despachado</span>
            </div>

            <div class="stat-card">
              <span class="stat-label">Subtotal Neto</span>
              <span class="stat-value font-mono">{{ report.totalSubtotal | currency:'USD' }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">Base antes de IVA</span>
            </div>

            <div class="stat-card" style="border-left: 4px solid var(--color-primary);">
              <span class="stat-label">Ingresos Generados</span>
              <span class="stat-value font-mono text-success">{{ report.totalRevenue | currency:'USD' }}</span>
              <span class="text-muted" style="font-size: var(--font-size-xs);">IVA incluido: {{ report.totalTax | currency:'USD' }}</span>
            </div>
          </div>

          <!-- Top Contributor Ranking Visualization -->
          <div *ngIf="!loadingProducts() && topProducts().length > 0" class="card" style="margin-bottom: var(--space-4); padding: var(--space-4);">
            <h3 class="card-title" style="font-size: var(--font-size-md); margin-bottom: var(--space-3);">
              Top Productos por Recaudación Monetaria
            </h3>
            <div class="ranking-list">
              <div *ngFor="let item of topProducts()" class="ranking-item">
                <div class="ranking-header">
                  <span>
                    <strong>{{ item.productName }}</strong>
                    <span class="font-mono text-muted" style="font-size: var(--font-size-xs); margin-left: var(--space-2);">
                      ({{ item.productIdentifier }}) — {{ item.categoryName }}
                    </span>
                  </span>
                  <span class="font-mono" style="font-weight: 600;">
                    {{ item.total | currency:'USD' }} ({{ item.unitsSold }} uds)
                    <span class="text-muted" style="font-size: var(--font-size-xs);">
                      [{{ getRevenuePercentage(item.total, productSalesReport()!.totalRevenue) }}%]
                    </span>
                  </span>
                </div>
                <div class="progress-track">
                  <div
                    class="progress-fill"
                    [style.width.%]="getRevenuePercentage(item.total, productSalesReport()!.totalRevenue)">
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Loading State -->
          <div *ngIf="loadingProducts()" class="card loading-card">
            <div class="spinner"></div>
            <p>Calculando rendimiento de productos...</p>
          </div>

          <!-- Product Performance Table -->
          <div *ngIf="!loadingProducts() && productSalesReport() as report" class="card table-card">
            <div class="table-container">
              <table class="data-table">
                <thead>
                  <tr>
                    <th>Código</th>
                    <th>Producto</th>
                    <th>Categoría</th>
                    <th class="text-right">Unidades Vendidas</th>
                    <th style="min-width: 140px;">% Ingreso Total</th>
                    <th class="text-right">Subtotal</th>
                    <th class="text-right">Impuestos</th>
                    <th class="text-right">Total Recaudado</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let item of report.items">
                    <td>
                      <span class="id-pill font-mono">{{ item.productIdentifier }}</span>
                    </td>
                    <td>
                      <strong>{{ item.productName }}</strong>
                    </td>
                    <td>
                      <span class="badge badge-neutral">{{ item.categoryName }}</span>
                    </td>
                    <td class="text-right font-mono">{{ item.unitsSold }}</td>
                    <td>
                      <div style="display: flex; align-items: center; gap: var(--space-2);">
                        <div class="progress-track" style="flex: 1;">
                          <div
                            class="progress-fill info"
                            [style.width.%]="getRevenuePercentage(item.total, report.totalRevenue)">
                          </div>
                        </div>
                        <span class="font-mono text-muted" style="font-size: var(--font-size-xs); min-width: 36px;">
                          {{ getRevenuePercentage(item.total, report.totalRevenue) }}%
                        </span>
                      </div>
                    </td>
                    <td class="text-right font-mono">{{ item.subtotal | currency:'USD' }}</td>
                    <td class="text-right font-mono">{{ item.taxAmount | currency:'USD' }}</td>
                    <td class="text-right font-mono" style="font-weight: 600;">{{ item.total | currency:'USD' }}</td>
                  </tr>

                  <tr *ngIf="report.items.length === 0">
                    <td colspan="8" class="text-center text-muted" style="padding: var(--space-8);">
                      No se encontraron ventas para los productos y fechas seleccionados.
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </ng-container>
      </div>

      <!-- =================================================================== -->
      <!-- TAB 3: INVENTORY STOCK STATUS REPORT (AUTHENTICATED USERS)         -->
      <!-- =================================================================== -->
      <div *ngIf="activeTab() === 'inventory-status'">
        <!-- Filters Toolbar -->
        <div class="card toolbar-card">
          <form (ngSubmit)="loadInventoryStatus()" class="filters-row">
            <div class="form-group">
              <label for="invStatusCategorySelect" class="form-label">Filtrar por Categoría</label>
              <select
                id="invStatusCategorySelect"
                class="form-control"
                [ngModel]="inventoryCategoryId()"
                (ngModelChange)="inventoryCategoryId.set($event)"
                name="inventoryCategoryId">
                <option value="">Todas las categorías</option>
                <option *ngFor="let cat of categories()" [value]="cat.id">
                  {{ cat.name }}
                </option>
              </select>
            </div>

            <div class="form-group" style="display: flex; align-items: center; gap: var(--space-2); margin-top: 1.7rem;">
              <input
                type="checkbox"
                id="inStockOnlyCheckbox"
                [ngModel]="inventoryInStockOnly()"
                (ngModelChange)="inventoryInStockOnly.set($event)"
                name="inventoryInStockOnly" />
              <label for="inStockOnlyCheckbox" class="form-label" style="margin-bottom: 0; cursor: pointer;">
                Solo productos con existencias disponibles
              </label>
            </div>

            <div class="form-group flex-1">
              <label for="invSearchInput" class="form-label">Búsqueda Rápida</label>
              <input
                type="text"
                id="invSearchInput"
                class="form-control"
                placeholder="Filtrar por código o nombre en tabla..."
                [ngModel]="inventorySearchTerm()"
                (ngModelChange)="inventorySearchTerm.set($event)"
                name="inventorySearchTerm" />
            </div>

            <div class="actions-group" style="align-self: flex-end;">
              <button type="submit" class="btn btn-primary" [disabled]="loadingInventory()">
                <span *ngIf="loadingInventory()" class="spinner-sm" aria-hidden="true"></span>
                Actualizar Estado
              </button>
              <button type="button" class="btn btn-secondary" (click)="resetInventoryStatusFilters()">
                Restablecer
              </button>
            </div>
          </form>
        </div>

        <!-- Inventory Metrics Cards -->
        <div *ngIf="inventoryStatusReport() as report" class="state-card" style="margin-bottom: var(--space-4);">
          <div class="stat-card">
            <span class="stat-label">Total Productos en Catálogo</span>
            <span class="stat-value font-mono">{{ report.totalProducts }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Definiciones de producto activas e inactivas</span>
          </div>

          <div class="stat-card">
            <span class="stat-label">Unidades Físicas en Mano</span>
            <span class="stat-value font-mono text-primary">{{ report.totalUnitsOnHand }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Existencias acumuladas en almacén</span>
          </div>

          <div class="stat-card" style="border-left: 4px solid var(--color-success);">
            <span class="stat-label">Productos con Stock</span>
            <span class="stat-value font-mono text-success">{{ report.inStockProductsCount }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Disponibles para venta inmediata</span>
          </div>

          <div class="stat-card" style="border-left: 4px solid var(--color-danger);">
            <span class="stat-label">Productos sin Stock</span>
            <span class="stat-value font-mono text-danger">{{ report.outOfStockProductsCount }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Agotados o sin registro físico</span>
          </div>
        </div>

        <!-- Visual Health Bar -->
        <div *ngIf="inventoryStatusReport() as report" class="card" style="padding: var(--space-3) var(--space-4); margin-bottom: var(--space-4);">
          <div class="ranking-header" style="margin-bottom: var(--space-2);">
            <span style="font-size: var(--font-size-xs); font-weight: 600; color: var(--color-text-muted);">
              Tasa de Disponibilidad del Catálogo
            </span>
            <span class="font-mono text-muted" style="font-size: var(--font-size-xs);">
              {{ stockHealthPercentage() }}% con existencia
            </span>
          </div>
          <div class="progress-track" style="height: 10px;">
            <div class="progress-fill success" [style.width.%]="stockHealthPercentage()"></div>
          </div>
        </div>

        <!-- Loading State -->
        <div *ngIf="loadingInventory()" class="card loading-card">
          <div class="spinner"></div>
          <p>Consultando existencias de almacén...</p>
        </div>

        <!-- Inventory Status Table -->
        <div *ngIf="!loadingInventory() && inventoryStatusReport()" class="card table-card">
          <div class="table-container">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Producto</th>
                  <th>Categoría</th>
                  <th class="text-right">Unidades Físicas</th>
                  <th>Registro de Inventario</th>
                  <th>Estado de Catálogo</th>
                  <th>Disponibilidad</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of filteredInventoryItems()">
                  <td>
                    <span class="id-pill font-mono">{{ item.productIdentifier }}</span>
                  </td>
                  <td>
                    <strong>{{ item.productName }}</strong>
                  </td>
                  <td>
                    <span class="badge badge-neutral">{{ item.categoryName }}</span>
                  </td>
                  <td class="text-right font-mono" style="font-weight: 600; font-size: var(--font-size-base);">
                    {{ item.quantity }}
                  </td>
                  <td>
                    <span
                      class="badge"
                      [ngClass]="item.hasInventoryRecord ? 'badge-success' : 'badge-neutral'"
                      [title]="item.hasInventoryRecord ? 'Registro inicializado en almacén' : 'Sin fila en tabla de inventario'">
                      {{ item.hasInventoryRecord ? 'Inicializado' : 'Sin registro' }}
                    </span>
                  </td>
                  <td>
                    <span class="badge" [ngClass]="item.isActive ? 'badge-success' : 'badge-neutral'">
                      {{ item.isActive ? 'Activo' : 'Inactivo' }}
                    </span>
                  </td>
                  <td>
                    <span
                      class="badge"
                      [ngClass]="item.stockStatus === 'InStock' ? 'badge-success' : 'badge-danger'">
                      {{ item.stockStatus === 'InStock' ? 'Disponible' : 'Agotado' }}
                    </span>
                  </td>
                </tr>

                <tr *ngIf="filteredInventoryItems().length === 0">
                  <td colspan="7" class="text-center text-muted" style="padding: var(--space-8);">
                    No se encontraron productos coincidentes con los filtros seleccionados.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- =================================================================== -->
      <!-- TAB 4: INVENTORY MOVEMENT TRACEABILITY (AUTHENTICATED USERS)       -->
      <!-- =================================================================== -->
      <div *ngIf="activeTab() === 'inventory-movements'">
        <!-- Movement Filters Toolbar -->
        <div class="card toolbar-card">
          <form (ngSubmit)="loadInventoryMovements()" class="filters-row">
            <div class="form-group">
              <label for="movFromDate" class="form-label">Desde (UTC) <span class="required">*</span></label>
              <input
                type="date"
                id="movFromDate"
                class="form-control"
                [ngModel]="movementFromDate()"
                (ngModelChange)="movementFromDate.set($event)"
                name="movementFromDate"
                required />
            </div>

            <div class="form-group">
              <label for="movToDate" class="form-label">Hasta (UTC) <span class="required">*</span></label>
              <input
                type="date"
                id="movToDate"
                class="form-control"
                [ngModel]="movementToDate()"
                (ngModelChange)="movementToDate.set($event)"
                name="movementToDate"
                required />
            </div>

            <div class="form-group">
              <label for="movTypeSelect" class="form-label">Tipo de Movimiento</label>
              <select
                id="movTypeSelect"
                class="form-control"
                [ngModel]="movementType()"
                (ngModelChange)="movementType.set($event)"
                name="movementType">
                <option value="">Todos los tipos</option>
                <option value="Entry">Entrada (+)</option>
                <option value="Exit">Salida (-)</option>
                <option value="Adjustment">Ajuste (+/-)</option>
              </select>
            </div>

            <div class="form-group">
              <label for="movSupplierSelect" class="form-label">Proveedor</label>
              <select
                id="movSupplierSelect"
                class="form-control"
                [ngModel]="movementSupplierId()"
                (ngModelChange)="movementSupplierId.set($event)"
                name="movementSupplierId">
                <option value="">Todos los proveedores</option>
                <option *ngFor="let s of suppliers()" [value]="s.id">
                  {{ s.name }}
                </option>
              </select>
            </div>

            <div class="form-group">
              <label for="movProductInput" class="form-label">Código de Producto</label>
              <input
                type="text"
                id="movProductInput"
                class="form-control font-mono"
                placeholder="Ej. PROD-01..."
                [ngModel]="movementProductIdentifier()"
                (ngModelChange)="movementProductIdentifier.set($event)"
                name="movementProductIdentifier" />
            </div>

            <div class="actions-group" style="align-self: flex-end;">
              <button type="submit" class="btn btn-primary" [disabled]="loadingMovements()">
                <span *ngIf="loadingMovements()" class="spinner-sm" aria-hidden="true"></span>
                Filtrar
              </button>
              <button type="button" class="btn btn-secondary" (click)="resetMovementFilters()">
                Restablecer
              </button>
            </div>
          </form>
        </div>

        <!-- Movement Metrics Cards -->
        <div *ngIf="inventoryMovementReport() as report" class="state-card" style="margin-bottom: var(--space-4);">
          <div class="stat-card">
            <span class="stat-label">Total Movimientos</span>
            <span class="stat-value font-mono">{{ report.totalMovements }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">
              {{ report.entryCount }} entradas | {{ report.exitCount }} salidas | {{ report.adjustmentCount }} ajustes
            </span>
          </div>

          <div class="stat-card" style="border-left: 4px solid var(--color-success);">
            <span class="stat-label">Entradas Registradas</span>
            <span class="stat-value font-mono text-success">+{{ report.totalUnitsIn }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Unidades ingresadas por compras</span>
          </div>

          <div class="stat-card" style="border-left: 4px solid var(--color-danger);">
            <span class="stat-label">Salidas Registradas</span>
            <span class="stat-value font-mono text-danger">-{{ report.totalUnitsOut }}</span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Unidades despachadas o mermadas</span>
          </div>

          <div class="stat-card">
            <span class="stat-label">Balance Neto (Delta)</span>
            <span
              class="stat-value font-mono"
              [ngClass]="report.netQuantityDelta >= 0 ? 'text-success' : 'text-danger'">
              {{ report.netQuantityDelta >= 0 ? '+' : '' }}{{ report.netQuantityDelta }}
            </span>
            <span class="text-muted" style="font-size: var(--font-size-xs);">Variación neta en almacén</span>
          </div>
        </div>

        <!-- Note about Audit Attribution -->
        <div class="card" style="padding: var(--space-3) var(--space-4); margin-bottom: var(--space-4); background-color: var(--color-surface-hover);">
          <p style="font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0;">
            <strong>Trazabilidad y Auditoría:</strong> La columna <em>Usuario Responsable</em> expone la atribución inmutable del operador que originó cada transacción de stock. Los registros son históricos e inmutables.
          </p>
        </div>

        <!-- Loading State -->
        <div *ngIf="loadingMovements()" class="card loading-card">
          <div class="spinner"></div>
          <p>Cargando trazabilidad de movimientos...</p>
        </div>

        <!-- Movements Table -->
        <div *ngIf="!loadingMovements() && inventoryMovementReport() as report" class="card table-card">
          <div class="table-container">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Fecha (UTC)</th>
                  <th>Código</th>
                  <th>Tipo</th>
                  <th class="text-right">Cantidad</th>
                  <th>Referencia / Motivo</th>
                  <th>Proveedor Snapshot</th>
                  <th>Usuario Responsable</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let m of report.movements">
                  <td class="font-mono" style="font-size: var(--font-size-xs);">
                    {{ m.occurredAtUtc | date:'yyyy-MM-dd HH:mm' }}
                  </td>
                  <td>
                    <span class="id-pill font-mono">{{ m.productIdentifier }}</span>
                  </td>
                  <td>
                    <span
                      class="badge"
                      [ngClass]="{
                        'badge-success': m.movementType === 'Entry',
                        'badge-danger': m.movementType === 'Exit',
                        'badge-warning': m.movementType === 'Adjustment'
                      }">
                      {{ m.movementType === 'Entry' ? 'ENTRADA' : (m.movementType === 'Exit' ? 'SALIDA' : 'AJUSTE') }}
                    </span>
                  </td>
                  <td
                    class="text-right font-mono"
                    style="font-weight: 600;"
                    [ngClass]="{
                      'text-success': m.movementType === 'Entry' || (m.movementType === 'Adjustment' && m.quantity > 0),
                      'text-danger': m.movementType === 'Exit' || (m.movementType === 'Adjustment' && m.quantity < 0)
                    }">
                    {{ m.movementType === 'Entry' ? '+' : '' }}{{ m.quantity }}
                  </td>
                  <td>{{ m.reference }}</td>
                  <td>
                    <span *ngIf="m.supplierName" class="badge badge-neutral">
                      {{ m.supplierName }}
                    </span>
                    <span *ngIf="!m.supplierName" class="text-muted" style="font-size: var(--font-size-xs);">—</span>
                  </td>
                  <td>
                    <span *ngIf="m.userId" class="id-pill font-mono" [title]="m.userId">
                      {{ m.userId }}
                    </span>
                    <span *ngIf="!m.userId" class="text-muted" style="font-size: var(--font-size-xs);">Sistema</span>
                  </td>
                </tr>

                <tr *ngIf="report.movements.length === 0">
                  <td colspan="7" class="text-center text-muted" style="padding: var(--space-8);">
                    No se encontraron movimientos de inventario en el período seleccionado.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
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
  `]
})
export class ReportsComponent implements OnInit {
  public readonly authService = inject(AuthService);
  private readonly reportService = inject(ReportService);
  private readonly categoryService = inject(CategoryService);
  private readonly customerService = inject(CustomerService);
  private readonly supplierService = inject(SupplierService);
  private readonly productService = inject(ProductService);

  // Active Tab
  public readonly activeTab = signal<ReportTab>('sales-summary');

  // Loading States
  public readonly loadingSales = signal<boolean>(false);
  public readonly loadingProducts = signal<boolean>(false);
  public readonly loadingInventory = signal<boolean>(false);
  public readonly loadingMovements = signal<boolean>(false);

  // Error Message
  public readonly errorMessage = signal<string | null>(null);

  // Reference Catalogs
  public readonly categories = signal<CategoryResponse[]>([]);
  public readonly customers = signal<CustomerResponse[]>([]);
  public readonly suppliers = signal<SupplierResponse[]>([]);
  public readonly products = signal<ProductResponse[]>([]);

  // Report Data Signals
  public readonly salesReport = signal<SalesSummaryReportResponse | null>(null);
  public readonly productSalesReport = signal<ProductSalesReportResponse | null>(null);
  public readonly inventoryStatusReport = signal<InventoryStatusReportResponse | null>(null);
  public readonly inventoryMovementReport = signal<InventoryMovementReportResponse | null>(null);

  // Tab 1 Filters: Sales Summary
  public readonly salesFromDate = signal<string>(this.calculateDefaultFromDate());
  public readonly salesToDate = signal<string>(this.calculateDefaultToDate());
  public readonly salesCustomerId = signal<string>('');
  public readonly salesStatus = signal<string>('');
  public readonly salesUserId = signal<string>('');

  // Tab 2 Filters: Product Sales Performance
  public readonly productFromDate = signal<string>(this.calculateDefaultFromDate());
  public readonly productToDate = signal<string>(this.calculateDefaultToDate());
  public readonly productIdentifier = signal<string>('');
  public readonly productCategoryId = signal<string>('');

  // Tab 3 Filters: Inventory Stock Status
  public readonly inventoryCategoryId = signal<string>('');
  public readonly inventoryInStockOnly = signal<boolean>(false);
  public readonly inventorySearchTerm = signal<string>('');

  // Tab 4 Filters: Inventory Movement Traceability
  public readonly movementFromDate = signal<string>(this.calculateDefaultFromDate());
  public readonly movementToDate = signal<string>(this.calculateDefaultToDate());
  public readonly movementProductIdentifier = signal<string>('');
  public readonly movementType = signal<string>('');
  public readonly movementSupplierId = signal<string>('');

  // Computed signals
  public readonly topProducts = computed(() => {
    const report = this.productSalesReport();
    if (!report || report.items.length === 0) return [];
    return [...report.items]
      .sort((a, b) => b.total - a.total)
      .slice(0, 5);
  });

  public readonly filteredInventoryItems = computed(() => {
    const report = this.inventoryStatusReport();
    if (!report) return [];
    const term = this.inventorySearchTerm().trim().toLowerCase();
    if (!term) return report.items;
    return report.items.filter(
      i =>
        i.productIdentifier.toLowerCase().includes(term) ||
        i.productName.toLowerCase().includes(term) ||
        i.categoryName.toLowerCase().includes(term)
    );
  });

  public readonly stockHealthPercentage = computed(() => {
    const report = this.inventoryStatusReport();
    if (!report || report.totalProducts === 0) return 0;
    return Math.round((report.inStockProductsCount / report.totalProducts) * 100);
  });

  public ngOnInit(): void {
    // If the authenticated user is not an Admin, set default tab to operational inventory status
    if (!this.authService.isAdmin()) {
      this.activeTab.set('inventory-status');
    }

    this.loadCatalogDependencies();
    this.loadCurrentTabReport();
  }

  public setTab(tab: ReportTab): void {
    this.activeTab.set(tab);
    this.errorMessage.set(null);
    this.loadCurrentTabReport();
  }

  public printReport(): void {
    window.print();
  }

  public getRevenuePercentage(itemTotal: number, totalRevenue: number): number {
    if (!totalRevenue || totalRevenue <= 0) return 0;
    const pct = (itemTotal / totalRevenue) * 100;
    return Math.min(100, Math.round(pct * 10) / 10);
  }

  public loadSalesSummary(): void {
    if (!this.authService.isAdmin()) return;

    if (this.salesFromDate() > this.salesToDate()) {
      this.errorMessage.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }

    this.loadingSales.set(true);
    this.errorMessage.set(null);

    const fromIso = `${this.salesFromDate()}T00:00:00.000Z`;
    const toIso = `${this.salesToDate()}T23:59:59.999Z`;

    this.reportService
      .getSalesSummary({
        fromUtc: fromIso,
        toUtc: toIso,
        customerId: this.salesCustomerId() || null,
        status: this.salesStatus() || null,
        userId: this.salesUserId().trim() || null
      })
      .subscribe({
        next: report => {
          this.salesReport.set(report);
          this.loadingSales.set(false);
        },
        error: err => {
          this.loadingSales.set(false);
          this.errorMessage.set(parseApiError(err));
        }
      });
  }

  public resetSalesFilters(): void {
    this.salesFromDate.set(this.calculateDefaultFromDate());
    this.salesToDate.set(this.calculateDefaultToDate());
    this.salesCustomerId.set('');
    this.salesStatus.set('');
    this.salesUserId.set('');
    this.loadSalesSummary();
  }

  public loadProductSales(): void {
    if (!this.authService.isAdmin()) return;

    if (this.productFromDate() > this.productToDate()) {
      this.errorMessage.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }

    this.loadingProducts.set(true);
    this.errorMessage.set(null);

    const fromIso = `${this.productFromDate()}T00:00:00.000Z`;
    const toIso = `${this.productToDate()}T23:59:59.999Z`;

    this.reportService
      .getProductSales({
        fromUtc: fromIso,
        toUtc: toIso,
        productIdentifier: this.productIdentifier() || null,
        categoryId: this.productCategoryId() || null
      })
      .subscribe({
        next: report => {
          this.productSalesReport.set(report);
          this.loadingProducts.set(false);
        },
        error: err => {
          this.loadingProducts.set(false);
          this.errorMessage.set(parseApiError(err));
        }
      });
  }

  public resetProductSalesFilters(): void {
    this.productFromDate.set(this.calculateDefaultFromDate());
    this.productToDate.set(this.calculateDefaultToDate());
    this.productIdentifier.set('');
    this.productCategoryId.set('');
    this.loadProductSales();
  }

  public loadInventoryStatus(): void {
    this.loadingInventory.set(true);
    this.errorMessage.set(null);

    this.reportService
      .getInventoryStatus({
        categoryId: this.inventoryCategoryId() || null,
        inStockOnly: this.inventoryInStockOnly() ? true : null
      })
      .subscribe({
        next: report => {
          this.inventoryStatusReport.set(report);
          this.loadingInventory.set(false);
        },
        error: err => {
          this.loadingInventory.set(false);
          this.errorMessage.set(parseApiError(err));
        }
      });
  }

  public resetInventoryStatusFilters(): void {
    this.inventoryCategoryId.set('');
    this.inventoryInStockOnly.set(false);
    this.inventorySearchTerm.set('');
    this.loadInventoryStatus();
  }

  public loadInventoryMovements(): void {
    if (this.movementFromDate() > this.movementToDate()) {
      this.errorMessage.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }

    this.loadingMovements.set(true);
    this.errorMessage.set(null);

    const fromIso = `${this.movementFromDate()}T00:00:00.000Z`;
    const toIso = `${this.movementToDate()}T23:59:59.999Z`;

    this.reportService
      .getInventoryMovements({
        fromUtc: fromIso,
        toUtc: toIso,
        productIdentifier: this.movementProductIdentifier() || null,
        movementType: this.movementType() || null,
        supplierId: this.movementSupplierId() || null
      })
      .subscribe({
        next: report => {
          this.inventoryMovementReport.set(report);
          this.loadingMovements.set(false);
        },
        error: err => {
          this.loadingMovements.set(false);
          this.errorMessage.set(parseApiError(err));
        }
      });
  }

  public resetMovementFilters(): void {
    this.movementFromDate.set(this.calculateDefaultFromDate());
    this.movementToDate.set(this.calculateDefaultToDate());
    this.movementProductIdentifier.set('');
    this.movementType.set('');
    this.movementSupplierId.set('');
    this.loadInventoryMovements();
  }

  private loadCurrentTabReport(): void {
    switch (this.activeTab()) {
      case 'sales-summary':
        if (this.authService.isAdmin() && !this.salesReport()) {
          this.loadSalesSummary();
        }
        break;
      case 'product-sales':
        if (this.authService.isAdmin() && !this.productSalesReport()) {
          this.loadProductSales();
        }
        break;
      case 'inventory-status':
        if (!this.inventoryStatusReport()) {
          this.loadInventoryStatus();
        }
        break;
      case 'inventory-movements':
        if (!this.inventoryMovementReport()) {
          this.loadInventoryMovements();
        }
        break;
    }
  }

  private loadCatalogDependencies(): void {
    this.categoryService.list().subscribe({
      next: cats => this.categories.set(cats),
      error: () => {}
    });

    if (this.authService.isAdmin()) {
      this.customerService.list().subscribe({
        next: custs => this.customers.set(custs),
        error: () => {}
      });

      this.productService.list().subscribe({
        next: prods => this.products.set(prods),
        error: () => {}
      });
    }

    this.supplierService.list().subscribe({
      next: sups => this.suppliers.set(sups),
      error: () => {}
    });
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
