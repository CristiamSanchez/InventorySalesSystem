import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AddSaleDetailRequest,
  AuthService,
  CustomerResponse,
  CustomerService,
  parseApiError,
  ProductResponse,
  ProductService,
  SaleDetailResponse,
  SaleResponse,
  SaleService,
  SalesSummaryReportResponse,
  SalesSummarySaleItemResponse
} from '@core';

@Component({
  selector: 'app-sales',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Punto de Venta y Facturación</h1>
          <p class="page-subtitle">Emisión atómica de ventas, cálculo de impuestos, confirmación y trazabilidad</p>
        </div>
        <div class="page-actions" *ngIf="authService.isAdmin()">
          <button
            *ngIf="currentSale()"
            type="button"
            class="btn btn-secondary"
            (click)="clearActiveSale()">
            Cerrar Venta Activa
          </button>
          <button
            type="button"
            class="btn btn-primary"
            (click)="startNewSaleModal()">
            <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            Nueva Venta
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
      <nav class="tabs-nav" aria-label="Secciones de Ventas">
        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'editor'"
          (click)="setTab('editor')">
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
            <circle cx="9" cy="21" r="1"></circle>
            <circle cx="20" cy="21" r="1"></circle>
            <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
          </svg>
          {{ currentSale() ? (currentSale()!.status === 'Confirmed' ? 'Comprobante de Venta' : 'Editor de Venta') : 'Terminal de Venta' }}
        </button>

        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'history'"
          (click)="setTab('history')">
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
          </svg>
          Historial de Transacciones
        </button>

        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'lookup'"
          (click)="setTab('lookup')">
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          Consultar por ID
        </button>
      </nav>

      <!-- TAB 1: Editor & POS & Receipt -->
      <ng-container *ngIf="activeTab() === 'editor'">
        <!-- No Active Sale State -->
        <div *ngIf="!currentSale()" class="card state-card">
          <div class="empty-icon">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <circle cx="9" cy="21" r="1"></circle>
              <circle cx="20" cy="21" r="1"></circle>
              <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
            </svg>
          </div>
          <h2 class="state-title">No hay una venta en curso</h2>
          <p class="state-text">
            Inicie un nuevo borrador de venta para registrar líneas de productos, o consulte una venta previa por su identificador.
          </p>
          <div class="actions-group" style="margin-top: var(--space-4);" *ngIf="authService.isAdmin()">
            <button type="button" class="btn btn-primary" (click)="startNewSaleModal()">
              Iniciar Nueva Venta
            </button>
          </div>
        </div>

        <!-- Active Sale: DRAFT STATE -->
        <ng-container *ngIf="currentSale() as sale">
          <div *ngIf="sale.status === 'Draft'" class="draft-workspace">
            <!-- Sale Metadata Banner -->
            <div class="card sale-meta-card">
              <div class="sale-meta-header">
                <div>
                  <div class="meta-row">
                    <span class="badge badge-warning">BORRADOR</span>
                    <span class="id-pill font-mono" [title]="sale.id">ID: {{ sale.id }}</span>
                    <span class="text-muted font-mono" style="font-size: var(--font-size-xs);">
                      {{ sale.createdAtUtc | date:'yyyy-MM-dd HH:mm' }} UTC
                    </span>
                  </div>
                  <h2 class="sale-customer-title">
                    Cliente: <strong>{{ sale.customerReference }}</strong>
                    <span *ngIf="sale.customerId" class="badge badge-neutral" style="margin-left: var(--space-2);">Registrado</span>
                    <span *ngIf="!sale.customerId" class="badge badge-neutral" style="margin-left: var(--space-2);">Mostrador</span>
                  </h2>
                </div>

                <div class="actions-group" *ngIf="authService.isAdmin()">
                  <button type="button" class="btn btn-secondary btn-sm" (click)="openUpdateCustomerModal()">
                    Cambiar Cliente
                  </button>
                </div>
              </div>
            </div>

            <!-- Add Line Item Card (Admin Only) -->
            <div class="card add-line-card" *ngIf="authService.isAdmin()">
              <h3 class="card-title" style="margin-bottom: var(--space-3); font-size: var(--font-size-md);">
                Agregar Producto al Borrador
              </h3>

              <form [formGroup]="detailForm" (ngSubmit)="onAddDetail()">
                <div class="form-row">
                  <div class="form-group flex-1">
                    <label for="detailProductSelect" class="form-label">
                      Producto de Catálogo <span class="required">*</span>
                    </label>
                    <select
                      id="detailProductSelect"
                      class="form-control"
                      formControlName="productIdentifier"
                      (change)="onProductSelectChange()">
                      <option value="">Seleccione un producto...</option>
                      <option *ngFor="let p of activeProducts()" [value]="p.identifier">
                        {{ p.identifier }} — {{ p.name }} ({{ p.categoryName }})
                      </option>
                    </select>
                  </div>

                  <div class="form-group" style="width: 120px;">
                    <label for="detailQuantity" class="form-label">
                      Cantidad <span class="required">*</span>
                    </label>
                    <input
                      id="detailQuantity"
                      type="number"
                      min="1"
                      class="form-control"
                      formControlName="quantity" />
                  </div>

                  <div class="form-group" style="width: 150px;">
                    <label for="detailUnitPrice" class="form-label">
                      Precio Unitario <span class="required">*</span>
                    </label>
                    <input
                      id="detailUnitPrice"
                      type="number"
                      step="0.01"
                      min="0"
                      class="form-control"
                      formControlName="unitPrice" />
                  </div>

                  <div class="form-group" style="width: 130px;">
                    <label for="detailTaxRate" class="form-label">
                      Tasa IVA <span class="required">*</span>
                    </label>
                    <select id="detailTaxRate" class="form-control" formControlName="taxRate">
                      <option [value]="0">0% (Exento)</option>
                      <option [value]="0.08">8% (Reducido)</option>
                      <option [value]="0.13">13% (General)</option>
                      <option [value]="0.16">16% (Estándar)</option>
                    </select>
                  </div>

                  <div class="form-group" style="align-self: flex-end;">
                    <button
                      type="submit"
                      class="btn btn-primary"
                      [disabled]="detailForm.invalid || addingDetail()">
                      <span *ngIf="addingDetail()" class="spinner-sm"></span>
                      <span>Agregar Línea</span>
                    </button>
                  </div>
                </div>
              </form>
            </div>

            <!-- Details Table -->
            <div class="card table-container">
              <div class="table-responsive">
                <table class="data-table">
                  <thead>
                    <tr>
                      <th>Código Producto</th>
                      <th class="text-right">Cantidad</th>
                      <th class="text-right">Precio Unit.</th>
                      <th class="text-right">Tasa IVA</th>
                      <th class="text-right">Subtotal</th>
                      <th class="text-right">Impuesto</th>
                      <th class="text-right">Total Línea</th>
                      <th class="text-right" *ngIf="authService.isAdmin()">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let d of sale.details">
                      <td>
                        <span class="id-pill font-mono">{{ d.productIdentifier }}</span>
                      </td>
                      <td class="text-right font-mono" style="font-weight: 600;">{{ d.quantity }}</td>
                      <td class="text-right font-mono">{{ d.unitPrice | currency }}</td>
                      <td class="text-right font-mono">{{ (d.taxRate * 100).toFixed(0) }}%</td>
                      <td class="text-right font-mono">{{ d.subtotal | currency }}</td>
                      <td class="text-right font-mono text-muted">{{ d.taxAmount | currency }}</td>
                      <td class="text-right font-mono text-success" style="font-weight: 700;">
                        {{ d.total | currency }}
                      </td>
                      <td class="text-right" *ngIf="authService.isAdmin()">
                        <button
                          type="button"
                          class="action-btn toggle-btn"
                          title="Eliminar línea"
                          (click)="onRemoveDetail(d.id)">
                          <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                            <polyline points="3 6 5 6 21 6"></polyline>
                            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                          </svg>
                        </button>
                      </td>
                    </tr>
                    <tr *ngIf="sale.details.length === 0">
                      <td colspan="8" style="text-align: center; padding: var(--space-6); color: var(--color-text-muted);">
                        El borrador no tiene líneas de detalle. Agregue al menos un producto antes de confirmar.
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <!-- Financial Summary & Confirm Bar -->
            <div class="sale-summary-card card">
              <div class="summary-breakdown">
                <div class="summary-item">
                  <span class="summary-label">Subtotal:</span>
                  <span class="summary-val font-mono">{{ sale.subtotal | currency }}</span>
                </div>
                <div class="summary-item">
                  <span class="summary-label">Impuestos (IVA):</span>
                  <span class="summary-val font-mono">{{ sale.taxAmount | currency }}</span>
                </div>
                <div class="summary-item total-item">
                  <span class="summary-label">TOTAL GENERAL:</span>
                  <span class="summary-val font-mono text-success">{{ sale.total | currency }}</span>
                </div>
              </div>

              <div class="confirm-actions" *ngIf="authService.isAdmin()">
                <button
                  type="button"
                  class="btn btn-primary"
                  style="padding: var(--space-3) var(--space-6); font-size: 1rem;"
                  [disabled]="sale.details.length === 0 || confirmingSale()"
                  (click)="onPromptConfirmSale()">
                  <span *ngIf="confirmingSale()" class="spinner-sm"></span>
                  <span>Confirmar Venta y Descontar Inventario</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Active Sale: CONFIRMED RECEIPT STATE -->
          <div *ngIf="sale.status === 'Confirmed'" class="receipt-container">
            <div class="card receipt-card">
              <div class="receipt-header">
                <div class="receipt-badge-container">
                  <span class="badge badge-success" style="font-size: var(--font-size-sm); padding: var(--space-1) var(--space-3);">
                    VENTA CONFIRMADA
                  </span>
                </div>
                <h2 class="receipt-title">COMPROBANTE DE VENTA</h2>
                <p class="receipt-folio font-mono">Folio: {{ sale.id }}</p>
                <p class="receipt-date font-mono text-muted">
                  Fecha Confirmación: {{ sale.confirmedAtUtc | date:'yyyy-MM-dd HH:mm:ss' }} UTC
                </p>
              </div>

              <div class="receipt-meta-grid">
                <div>
                  <span class="stat-label">Cliente / Receptor:</span>
                  <p style="font-weight: 600; margin-top: var(--space-1);">{{ sale.customerReference }}</p>
                </div>
                <div>
                  <span class="stat-label">Usuario Emisor:</span>
                  <p class="font-mono text-muted" style="margin-top: var(--space-1);">{{ sale.userId }}</p>
                </div>
              </div>

              <!-- Lines Table -->
              <div class="table-responsive" style="margin: var(--space-4) 0;">
                <table class="data-table">
                  <thead>
                    <tr>
                      <th>Código</th>
                      <th class="text-right">Cant.</th>
                      <th class="text-right">Precio Unit.</th>
                      <th class="text-right">IVA</th>
                      <th class="text-right">Subtotal</th>
                      <th class="text-right">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let item of sale.details">
                      <td><span class="id-pill font-mono">{{ item.productIdentifier }}</span></td>
                      <td class="text-right font-mono">{{ item.quantity }}</td>
                      <td class="text-right font-mono">{{ item.unitPrice | currency }}</td>
                      <td class="text-right font-mono">{{ (item.taxRate * 100).toFixed(0) }}%</td>
                      <td class="text-right font-mono">{{ item.subtotal | currency }}</td>
                      <td class="text-right font-mono" style="font-weight: 700;">{{ item.total | currency }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Receipt Financial Totals -->
              <div class="receipt-totals">
                <div class="receipt-total-row">
                  <span>Subtotal Neto:</span>
                  <span class="font-mono">{{ sale.subtotal | currency }}</span>
                </div>
                <div class="receipt-total-row">
                  <span>Impuesto (IVA):</span>
                  <span class="font-mono">{{ sale.taxAmount | currency }}</span>
                </div>
                <div class="receipt-total-row grand-total">
                  <span>TOTAL PAGADO:</span>
                  <span class="font-mono text-success">{{ sale.total | currency }}</span>
                </div>
              </div>

              <div class="receipt-footer-actions">
                <button type="button" class="btn btn-secondary" (click)="printReceipt()">
                  <svg viewBox="0 0 24 24" width="16" height="16" stroke="currentColor" stroke-width="2" fill="none">
                    <polyline points="6 9 6 2 18 2 18 9"></polyline>
                    <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
                    <rect x="6" y="14" width="12" height="8"></rect>
                  </svg>
                  Imprimir Comprobante
                </button>
                <button type="button" class="btn btn-primary" (click)="clearActiveSale()">
                  Iniciar Nueva Venta
                </button>
              </div>
            </div>
          </div>
        </ng-container>
      </ng-container>

      <!-- TAB 2: Sales History -->
      <ng-container *ngIf="activeTab() === 'history'">
        <!-- Admin History View -->
        <ng-container *ngIf="authService.isAdmin()">
          <!-- Summary Metrics Cards -->
          <div class="stats-row" *ngIf="historyReport() as rep">
            <div class="stat-card">
              <span class="stat-value">{{ rep.totalTransactions }}</span>
              <span class="stat-label">Total Transacciones</span>
            </div>
            <div class="stat-card">
              <span class="stat-value text-success">{{ rep.confirmedTransactions }}</span>
              <span class="stat-label">Confirmadas</span>
            </div>
            <div class="stat-card">
              <span class="stat-value text-warning">{{ rep.draftTransactions }}</span>
              <span class="stat-label">Borradores</span>
            </div>
            <div class="stat-card">
              <span class="stat-value text-success font-mono">{{ rep.totalRevenue | currency }}</span>
              <span class="stat-label">Ingresos Realizados</span>
            </div>
          </div>

          <!-- History Filters -->
          <div class="toolbar-card">
            <div class="filters-row">
              <div class="select-group">
                <label for="historyFromDate" class="select-label">Desde:</label>
                <input
                  id="historyFromDate"
                  type="date"
                  class="form-control"
                  [ngModel]="historyFromDate()"
                  (ngModelChange)="historyFromDate.set($event)" />
              </div>

              <div class="select-group">
                <label for="historyToDate" class="select-label">Hasta:</label>
                <input
                  id="historyToDate"
                  type="date"
                  class="form-control"
                  [ngModel]="historyToDate()"
                  (ngModelChange)="historyToDate.set($event)" />
              </div>

              <div class="select-group">
                <label for="historyStatus" class="select-label">Estado:</label>
                <select
                  id="historyStatus"
                  class="form-control"
                  [ngModel]="historyStatus()"
                  (ngModelChange)="historyStatus.set($event)">
                  <option value="">Todos los estados</option>
                  <option value="Confirmed">Confirmadas</option>
                  <option value="Draft">Borradores</option>
                  <option value="Cancelled">Canceladas</option>
                </select>
              </div>

              <div class="select-group">
                <label for="historyCustomer" class="select-label">Cliente:</label>
                <select
                  id="historyCustomer"
                  class="form-control"
                  [ngModel]="historyCustomerId()"
                  (ngModelChange)="historyCustomerId.set($event)">
                  <option value="">Todos los clientes</option>
                  <option *ngFor="let c of customers()" [value]="c.id">{{ c.name }}</option>
                </select>
              </div>

              <button type="button" class="btn btn-secondary btn-sm" (click)="loadHistoryReport()" [disabled]="loadingHistory()">
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
          <div *ngIf="loadingHistory()" class="card state-card">
            <div class="spinner"></div>
            <p class="state-text">Cargando historial de ventas...</p>
          </div>

          <!-- History Table -->
          <div *ngIf="!loadingHistory() && (historyReport()?.sales?.length ?? 0) > 0" class="card table-container">
            <div class="table-responsive">
              <table class="data-table">
                <thead>
                  <tr>
                    <th>Fecha UTC</th>
                    <th>Estado</th>
                    <th>Cliente</th>
                    <th class="text-right">Líneas</th>
                    <th class="text-right">Unidades</th>
                    <th class="text-right">Total</th>
                    <th class="text-right">Acción</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let s of historyReport()?.sales">
                    <td class="font-mono text-muted">{{ s.createdAtUtc | date:'yyyy-MM-dd HH:mm' }}</td>
                    <td>
                      <span
                        class="badge"
                        [class.badge-success]="s.status === 'Confirmed'"
                        [class.badge-warning]="s.status === 'Draft'"
                        [class.badge-danger]="s.status === 'Cancelled'">
                        {{ s.status === 'Confirmed' ? 'Confirmada' : s.status === 'Draft' ? 'Borrador' : 'Cancelada' }}
                      </span>
                    </td>
                    <td><strong>{{ s.customerReference }}</strong></td>
                    <td class="text-right font-mono">{{ s.lineCount }}</td>
                    <td class="text-right font-mono">{{ s.totalUnits }}</td>
                    <td class="text-right font-mono" style="font-weight: 700;">{{ s.total | currency }}</td>
                    <td class="text-right">
                      <button
                        type="button"
                        class="btn btn-secondary btn-sm"
                        (click)="loadSaleById(s.id)">
                        Ver Detalle
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Empty History State -->
          <div *ngIf="!loadingHistory() && (historyReport()?.sales?.length ?? 0) === 0" class="card state-card">
            <div class="empty-icon">
              <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="12" y1="8" x2="12" y2="12"></line>
                <line x1="12" y1="16" x2="12.01" y2="16"></line>
              </svg>
            </div>
            <h2 class="state-title">No hay ventas registradas</h2>
            <p class="state-text">No se encontraron ventas para los filtros y fechas seleccionadas.</p>
          </div>
        </ng-container>

        <!-- Non-Admin History Limitation Notice -->
        <div *ngIf="!authService.isAdmin()" class="card state-card">
          <div class="empty-icon">
            <svg viewBox="0 0 24 24" width="48" height="48" stroke="currentColor" stroke-width="1.5" fill="none">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
          </div>
          <h2 class="state-title">Historial Consolidado Restringido</h2>
          <p class="state-text">
            El reporte y listado consolidado de ventas está reservado exclusivamente para usuarios con rol de Administrador.
            Los usuarios estándar pueden consultar comprobantes y detalles de ventas individuales mediante la pestaña <strong>Consultar por ID</strong>.
          </p>
          <div style="margin-top: var(--space-4);">
            <button type="button" class="btn btn-secondary" (click)="setTab('lookup')">
              Ir a Consultar por ID
            </button>
          </div>
        </div>
      </ng-container>

      <!-- TAB 3: Sale Lookup by ID -->
      <ng-container *ngIf="activeTab() === 'lookup'">
        <div class="card" style="max-width: 600px; margin: 0 auto;">
          <h2 class="card-title" style="margin-bottom: var(--space-2);">Consultar Venta por Identificador</h2>
          <p class="text-muted" style="font-size: var(--font-size-sm); margin-bottom: var(--space-4);">
            Ingrese el UUID de la venta para recuperar sus detalles autorizados, estado y líneas de producto.
          </p>

          <div class="form-row">
            <div class="form-group flex-1">
              <label for="lookupSaleIdInput" class="form-label">Identificador de Venta (GUID) <span class="required">*</span></label>
              <input
                id="lookupSaleIdInput"
                type="text"
                class="form-control font-mono"
                placeholder="Ej: 11111111-1111-1111-1111-111111111111"
                [ngModel]="lookupSaleId()"
                (ngModelChange)="lookupSaleId.set($event)" />
            </div>
            <div class="form-group" style="align-self: flex-end;">
              <button
                type="button"
                class="btn btn-primary"
                [disabled]="!lookupSaleId().trim() || loadingLookup()"
                (click)="onExecuteLookup()">
                <span *ngIf="loadingLookup()" class="spinner-sm"></span>
                <span>Buscar</span>
              </button>
            </div>
          </div>
        </div>
      </ng-container>

      <!-- MODAL: Start New Sale (Admin Only) -->
      <div *ngIf="showNewSaleModal()" class="dialog-backdrop" (click)="closeNewSaleModal()">
        <div class="dialog-card modal-card" (click)="$event.stopPropagation()" role="dialog" aria-modal="true">
          <div class="dialog-header">
            <div>
              <h2 class="dialog-title">Nueva Venta</h2>
              <p class="dialog-subtitle">Cree un borrador para agregar productos y confirmar</p>
            </div>
            <button type="button" class="btn-icon" (click)="closeNewSaleModal()">×</button>
          </div>

          <form [formGroup]="newSaleForm" (ngSubmit)="onCreateDraftSale()">
            <div class="dialog-body">
              <div class="form-group">
                <label class="form-label">Tipo de Cliente</label>
                <div class="radio-row">
                  <label class="radio-label">
                    <input
                      type="radio"
                      value="registered"
                      formControlName="customerType"
                      (change)="onCustomerTypeChange()" />
                    Cliente Registrado
                  </label>
                  <label class="radio-label">
                    <input
                      type="radio"
                      value="walkin"
                      formControlName="customerType"
                      (change)="onCustomerTypeChange()" />
                    Mostrador / Consumidor Final
                  </label>
                </div>
              </div>

              <!-- Registered Customer Select -->
              <div *ngIf="newSaleForm.get('customerType')?.value === 'registered'" class="form-group">
                <label for="newSaleCustomerSelect" class="form-label">
                  Cliente <span class="required">*</span>
                </label>
                <select id="newSaleCustomerSelect" class="form-control" formControlName="customerId">
                  <option value="">Seleccione un cliente registrado...</option>
                  <option *ngFor="let c of activeCustomers()" [value]="c.id">
                    {{ c.name }} ({{ c.identificationNumber || 'S/N' }})
                  </option>
                </select>
              </div>

              <!-- Walk-in Customer Reference Input -->
              <div *ngIf="newSaleForm.get('customerType')?.value === 'walkin'" class="form-group">
                <label for="newSaleCustomerRef" class="form-label">
                  Referencia / Nombre de Cliente <span class="required">*</span>
                </label>
                <input
                  id="newSaleCustomerRef"
                  type="text"
                  class="form-control"
                  formControlName="customerReference"
                  placeholder="Ej: Consumidor Final / Juan Pérez" />
              </div>
            </div>

            <div class="dialog-footer">
              <button type="button" class="btn btn-secondary" (click)="closeNewSaleModal()">Cancelar</button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="creatingDraft() || newSaleForm.invalid">
                <span *ngIf="creatingDraft()" class="spinner-sm"></span>
                <span>Crear Borrador</span>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- MODAL: Update Customer on Draft -->
      <div *ngIf="showUpdateCustomerModal()" class="dialog-backdrop" (click)="closeUpdateCustomerModal()">
        <div class="dialog-card modal-card" (click)="$event.stopPropagation()" role="dialog" aria-modal="true">
          <div class="dialog-header">
            <div>
              <h2 class="dialog-title">Cambiar Cliente de la Venta</h2>
              <p class="dialog-subtitle">Actualice la asociación del cliente en este borrador</p>
            </div>
            <button type="button" class="btn-icon" (click)="closeUpdateCustomerModal()">×</button>
          </div>

          <form [formGroup]="updateCustomerForm" (ngSubmit)="onUpdateCustomerSubmit()">
            <div class="dialog-body">
              <div class="form-group">
                <label class="form-label">Tipo de Cliente</label>
                <div class="radio-row">
                  <label class="radio-label">
                    <input
                      type="radio"
                      value="registered"
                      formControlName="customerType" />
                    Cliente Registrado
                  </label>
                  <label class="radio-label">
                    <input
                      type="radio"
                      value="walkin"
                      formControlName="customerType" />
                    Mostrador / Sin Registro
                  </label>
                </div>
              </div>

              <div *ngIf="updateCustomerForm.get('customerType')?.value === 'registered'" class="form-group">
                <label for="updCustomerSelect" class="form-label">Cliente Registrado <span class="required">*</span></label>
                <select id="updCustomerSelect" class="form-control" formControlName="customerId">
                  <option value="">Seleccione un cliente...</option>
                  <option *ngFor="let c of activeCustomers()" [value]="c.id">
                    {{ c.name }} ({{ c.identificationNumber || 'S/N' }})
                  </option>
                </select>
              </div>

              <div *ngIf="updateCustomerForm.get('customerType')?.value === 'walkin'" class="form-group">
                <label for="updCustomerRef" class="form-label">Referencia de Cliente <span class="required">*</span></label>
                <input
                  id="updCustomerRef"
                  type="text"
                  class="form-control"
                  formControlName="customerReference"
                  placeholder="Ej: Cliente Mostrador" />
              </div>
            </div>

            <div class="dialog-footer">
              <button type="button" class="btn btn-secondary" (click)="closeUpdateCustomerModal()">Cancelar</button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="updatingCustomer() || updateCustomerForm.invalid">
                <span *ngIf="updatingCustomer()" class="spinner-sm"></span>
                <span>Guardar Cambios</span>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- MODAL: Confirm Sale Prompt Warning -->
      <div *ngIf="showConfirmPromptModal()" class="dialog-backdrop" (click)="closeConfirmPromptModal()">
        <div class="dialog-card modal-card" (click)="$event.stopPropagation()" role="dialog" aria-modal="true">
          <div class="dialog-header">
            <div>
              <h2 class="dialog-title">Confirmar Venta Definitiva</h2>
              <p class="dialog-subtitle">Verifique los datos antes de procesar el inventario</p>
            </div>
            <button type="button" class="btn-icon" (click)="closeConfirmPromptModal()">×</button>
          </div>

          <div class="dialog-body">
            <p style="line-height: 1.6;">
              ¿Está seguro de confirmar esta venta por un total de
              <strong class="text-success font-mono">{{ currentSale()?.total | currency }}</strong>?
            </p>
            <div class="alert alert-warning" style="margin-top: var(--space-3); font-size: var(--font-size-sm);">
              <strong>Atención:</strong> Esta operación descontará automáticamente las unidades del inventario de forma atómica. Una vez confirmada, la venta no podrá ser modificada ni cancelada.
            </div>
          </div>

          <div class="dialog-footer">
            <button type="button" class="btn btn-secondary" (click)="closeConfirmPromptModal()" [disabled]="confirmingSale()">
              Cancelar
            </button>
            <button
              type="button"
              class="btn btn-primary"
              [disabled]="confirmingSale()"
              (click)="onExecuteConfirmSale()">
              <span *ngIf="confirmingSale()" class="spinner-sm"></span>
              <span>Confirmar Venta Ahora</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .sale-meta-card {
      margin-bottom: var(--space-4);
      padding: var(--space-4) var(--space-6);
    }

    .sale-meta-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: var(--space-3);
    }

    .meta-row {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      margin-bottom: var(--space-2);
    }

    .sale-customer-title {
      font-size: var(--font-size-lg);
      font-weight: 600;
      color: var(--color-text);
    }

    .add-line-card {
      margin-bottom: var(--space-4);
      padding: var(--space-4) var(--space-6);
      background-color: var(--color-surface);
    }

    .sale-summary-card {
      margin-top: var(--space-4);
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: var(--space-4);
      padding: var(--space-4) var(--space-6);
    }

    .summary-breakdown {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }

    .summary-item {
      display: flex;
      justify-content: space-between;
      gap: var(--space-6);
      font-size: var(--font-size-sm);
    }

    .summary-item.total-item {
      font-size: var(--font-size-lg);
      font-weight: 700;
      border-top: 1px solid var(--color-border);
      padding-top: var(--space-2);
      margin-top: var(--space-1);
    }

    .summary-label {
      color: var(--color-text-muted);
    }

    .radio-row {
      display: flex;
      gap: var(--space-4);
      margin-top: var(--space-2);
    }

    .radio-label {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-sm);
      cursor: pointer;
    }

    .receipt-container {
      max-width: 700px;
      margin: 0 auto;
    }

    .receipt-card {
      padding: var(--space-8);
      background-color: var(--color-surface);
      border: 2px dashed var(--color-border);
      border-radius: var(--radius-md);
    }

    .receipt-header {
      text-align: center;
      margin-bottom: var(--space-6);
      border-bottom: 1px solid var(--color-border-subtle);
      padding-bottom: var(--space-4);
    }

    .receipt-title {
      font-size: var(--font-size-xl);
      font-weight: 700;
      margin-top: var(--space-2);
      letter-spacing: 0.05em;
    }

    .receipt-folio {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      margin-top: var(--space-1);
    }

    .receipt-meta-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--space-4);
      margin-bottom: var(--space-4);
    }

    .receipt-totals {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      max-width: 300px;
      margin-left: auto;
      margin-top: var(--space-4);
    }

    .receipt-total-row {
      display: flex;
      justify-content: space-between;
      font-size: var(--font-size-sm);
    }

    .receipt-total-row.grand-total {
      font-size: var(--font-size-lg);
      font-weight: 700;
      border-top: 2px solid var(--color-border);
      padding-top: var(--space-2);
    }

    .receipt-footer-actions {
      display: flex;
      justify-content: flex-end;
      gap: var(--space-3);
      margin-top: var(--space-6);
      border-top: 1px solid var(--color-border-subtle);
      padding-top: var(--space-4);
    }
  `]
})
export class SalesComponent implements OnInit {
  public readonly authService = inject(AuthService);
  private readonly saleService = inject(SaleService);
  private readonly customerService = inject(CustomerService);
  private readonly productService = inject(ProductService);
  private readonly fb = inject(FormBuilder);

  // Active Tab
  public readonly activeTab = signal<'editor' | 'history' | 'lookup'>('editor');

  // Catalogs
  public readonly customers = signal<CustomerResponse[]>([]);
  public readonly products = signal<ProductResponse[]>([]);

  public readonly activeCustomers = computed(() => this.customers().filter(c => c.isActive));
  public readonly activeProducts = computed(() => this.products().filter(p => p.isActive));

  // Current Active Sale
  public readonly currentSale = signal<SaleResponse | null>(null);

  // Feedback Messages
  public readonly successMessage = signal<string | null>(null);
  public readonly errorMessage = signal<string | null>(null);

  // Detail Line Form
  public readonly addingDetail = signal<boolean>(false);
  public readonly detailForm: FormGroup = this.fb.group({
    productIdentifier: ['', [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]],
    unitPrice: [10.0, [Validators.required, Validators.min(0)]],
    taxRate: [0.16, [Validators.required, Validators.min(0)]]
  });

  // New Sale Modal State
  public readonly showNewSaleModal = signal<boolean>(false);
  public readonly creatingDraft = signal<boolean>(false);
  public readonly newSaleForm: FormGroup = this.fb.group({
    customerType: ['walkin', [Validators.required]],
    customerId: [''],
    customerReference: ['Consumidor Final']
  });

  // Update Customer Modal State
  public readonly showUpdateCustomerModal = signal<boolean>(false);
  public readonly updatingCustomer = signal<boolean>(false);
  public readonly updateCustomerForm: FormGroup = this.fb.group({
    customerType: ['walkin', [Validators.required]],
    customerId: [''],
    customerReference: ['']
  });

  // Confirm Sale Prompt Modal State
  public readonly showConfirmPromptModal = signal<boolean>(false);
  public readonly confirmingSale = signal<boolean>(false);

  // Sales History State (Admin Only)
  public readonly historyReport = signal<SalesSummaryReportResponse | null>(null);
  public readonly loadingHistory = signal<boolean>(false);
  public readonly historyFromDate = signal<string>(this.calculateDefaultFromDate());
  public readonly historyToDate = signal<string>(this.calculateDefaultToDate());
  public readonly historyStatus = signal<string>('');
  public readonly historyCustomerId = signal<string>('');

  // Lookup State
  public readonly lookupSaleId = signal<string>('');
  public readonly loadingLookup = signal<boolean>(false);

  public ngOnInit(): void {
    this.loadCatalogDependencies();
  }

  public setTab(tab: 'editor' | 'history' | 'lookup'): void {
    this.activeTab.set(tab);
    if (tab === 'history' && this.authService.isAdmin() && !this.historyReport()) {
      this.loadHistoryReport();
    }
  }

  public loadCatalogDependencies(): void {
    this.customerService.list().subscribe({
      next: custs => this.customers.set(custs),
      error: () => {}
    });

    this.productService.list().subscribe({
      next: prods => this.products.set(prods),
      error: () => {}
    });
  }

  public clearActiveSale(): void {
    this.currentSale.set(null);
    this.detailForm.reset({
      productIdentifier: '',
      quantity: 1,
      unitPrice: 10.0,
      taxRate: 0.16
    });
  }

  public startNewSaleModal(): void {
    this.newSaleForm.reset({
      customerType: 'walkin',
      customerId: '',
      customerReference: 'Consumidor Final'
    });
    this.showNewSaleModal.set(true);
  }

  public closeNewSaleModal(): void {
    this.showNewSaleModal.set(false);
    this.creatingDraft.set(false);
  }

  public onCustomerTypeChange(): void {
    const type = this.newSaleForm.get('customerType')?.value;
    if (type === 'registered') {
      this.newSaleForm.patchValue({ customerReference: '' });
      this.newSaleForm.get('customerId')?.setValidators([Validators.required]);
      this.newSaleForm.get('customerReference')?.clearValidators();
    } else {
      this.newSaleForm.patchValue({ customerId: '', customerReference: 'Consumidor Final' });
      this.newSaleForm.get('customerReference')?.setValidators([Validators.required]);
      this.newSaleForm.get('customerId')?.clearValidators();
    }
    this.newSaleForm.get('customerId')?.updateValueAndValidity();
    this.newSaleForm.get('customerReference')?.updateValueAndValidity();
  }

  public onCreateDraftSale(): void {
    if (this.newSaleForm.invalid) {
      this.newSaleForm.markAllAsTouched();
      return;
    }

    this.creatingDraft.set(true);
    this.errorMessage.set(null);

    const formVal = this.newSaleForm.value;
    const req = {
      customerId: formVal.customerType === 'registered' ? formVal.customerId : null,
      customerReference: formVal.customerType === 'walkin' ? formVal.customerReference?.trim() : null
    };

    this.saleService.createDraft(req).subscribe({
      next: sale => {
        this.creatingDraft.set(false);
        this.showNewSaleModal.set(false);
        this.currentSale.set(sale);
        this.setTab('editor');
        this.successMessage.set(`Borrador de venta iniciado para "${sale.customerReference}".`);
      },
      error: err => {
        this.creatingDraft.set(false);
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public openUpdateCustomerModal(): void {
    const sale = this.currentSale();
    if (!sale) return;

    this.updateCustomerForm.reset({
      customerType: sale.customerId ? 'registered' : 'walkin',
      customerId: sale.customerId || '',
      customerReference: sale.customerId ? '' : sale.customerReference
    });
    this.showUpdateCustomerModal.set(true);
  }

  public closeUpdateCustomerModal(): void {
    this.showUpdateCustomerModal.set(false);
    this.updatingCustomer.set(false);
  }

  public onUpdateCustomerSubmit(): void {
    const sale = this.currentSale();
    if (!sale || this.updateCustomerForm.invalid) return;

    this.updatingCustomer.set(true);
    this.errorMessage.set(null);

    const formVal = this.updateCustomerForm.value;
    const req = {
      customerId: formVal.customerType === 'registered' ? formVal.customerId : null,
      customerReference: formVal.customerType === 'walkin' ? formVal.customerReference?.trim() : null
    };

    this.saleService.updateCustomer(sale.id, req).subscribe({
      next: updatedSale => {
        this.updatingCustomer.set(false);
        this.showUpdateCustomerModal.set(false);
        this.currentSale.set(updatedSale);
        this.successMessage.set(`Cliente actualizado a "${updatedSale.customerReference}".`);
      },
      error: err => {
        this.updatingCustomer.set(false);
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public onProductSelectChange(): void {
    // When a product is selected, we keep current price or defaults
  }

  public onAddDetail(): void {
    const sale = this.currentSale();
    if (!sale || this.detailForm.invalid) {
      this.detailForm.markAllAsTouched();
      return;
    }

    this.addingDetail.set(true);
    this.errorMessage.set(null);

    const formVal = this.detailForm.value;
    const req: AddSaleDetailRequest = {
      productIdentifier: formVal.productIdentifier.trim(),
      quantity: Number(formVal.quantity),
      unitPrice: Number(formVal.unitPrice),
      taxRate: Number(formVal.taxRate)
    };

    this.saleService.addDetail(sale.id, req).subscribe({
      next: updatedSale => {
        this.addingDetail.set(false);
        this.currentSale.set(updatedSale);
        this.detailForm.patchValue({ quantity: 1 });
        this.successMessage.set(`Línea agregada: ${req.productIdentifier} x${req.quantity}.`);
      },
      error: err => {
        this.addingDetail.set(false);
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public onRemoveDetail(detailId: string): void {
    const sale = this.currentSale();
    if (!sale) return;

    this.errorMessage.set(null);
    this.saleService.removeDetail(sale.id, detailId).subscribe({
      next: updatedSale => {
        this.currentSale.set(updatedSale);
        this.successMessage.set('Línea eliminada del borrador.');
      },
      error: err => {
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public onPromptConfirmSale(): void {
    this.showConfirmPromptModal.set(true);
  }

  public closeConfirmPromptModal(): void {
    this.showConfirmPromptModal.set(false);
    this.confirmingSale.set(false);
  }

  public onExecuteConfirmSale(): void {
    const sale = this.currentSale();
    if (!sale) return;

    this.confirmingSale.set(true);
    this.errorMessage.set(null);

    this.saleService.confirm(sale.id).subscribe({
      next: confirmedSale => {
        this.confirmingSale.set(false);
        this.showConfirmPromptModal.set(false);
        this.currentSale.set(confirmedSale);
        this.successMessage.set(`Venta ${confirmedSale.id} confirmada con éxito. Inventario descontado.`);
      },
      error: err => {
        this.confirmingSale.set(false);
        this.showConfirmPromptModal.set(false);
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public printReceipt(): void {
    window.print();
  }

  public loadSaleById(saleId: string): void {
    this.errorMessage.set(null);
    this.saleService.getById(saleId).subscribe({
      next: sale => {
        this.currentSale.set(sale);
        this.setTab('editor');
      },
      error: err => {
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public onExecuteLookup(): void {
    const id = this.lookupSaleId().trim();
    if (!id) return;

    this.loadingLookup.set(true);
    this.errorMessage.set(null);

    this.saleService.getById(id).subscribe({
      next: sale => {
        this.loadingLookup.set(false);
        this.currentSale.set(sale);
        this.setTab('editor');
        this.successMessage.set(`Venta cargada con éxito: ${sale.id}`);
      },
      error: err => {
        this.loadingLookup.set(false);
        this.errorMessage.set(parseApiError(err));
      }
    });
  }

  public loadHistoryReport(): void {
    this.loadingHistory.set(true);
    this.errorMessage.set(null);

    const fromIso = `${this.historyFromDate()}T00:00:00.000Z`;
    const toIso = `${this.historyToDate()}T23:59:59.999Z`;

    this.saleService
      .listSummaryReport({
        fromUtc: fromIso,
        toUtc: toIso,
        customerId: this.historyCustomerId() || null,
        status: this.historyStatus() || null
      })
      .subscribe({
        next: report => {
          this.historyReport.set(report);
          this.loadingHistory.set(false);
        },
        error: err => {
          this.historyReport.set(null);
          this.loadingHistory.set(false);
          this.errorMessage.set(parseApiError(err));
        }
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
