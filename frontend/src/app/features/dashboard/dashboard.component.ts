import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Panel de Control</h1>
          <p class="page-subtitle">Visión general y estado operativo del sistema</p>
        </div>
        <div class="page-actions">
          <span class="badge badge-info">Fase 8.2 — Shell Activo</span>
        </div>
      </div>

      <div class="dashboard-grid">
        <div class="card metric-card">
          <span class="metric-label">Ventas del Mes</span>
          <span class="metric-value">--</span>
          <span class="badge badge-neutral">Pendiente Fase 8.4</span>
        </div>

        <div class="card metric-card">
          <span class="metric-label">Productos Activos</span>
          <span class="metric-value">--</span>
          <span class="badge badge-neutral">Pendiente Fase 8.4</span>
        </div>

        <div class="card metric-card">
          <span class="metric-label">Alertas de Stock Bajo</span>
          <span class="metric-value">--</span>
          <span class="badge badge-neutral">Pendiente Fase 8.4</span>
        </div>

        <div class="card metric-card">
          <span class="metric-label">Transacciones Registradas</span>
          <span class="metric-value">--</span>
          <span class="badge badge-neutral">Pendiente Fase 8.4</span>
        </div>
      </div>

      <div class="card welcome-card">
        <h2 class="card-title">Bienvenido al Sistema de Inventario y Ventas</h2>
        <p class="card-text">
          La arquitectura base del frontend Angular ha sido inicializada exitosamente con componentes standalone,
          enrutamiento modular lazy-loaded, paleta de colores CSS tokens y shell responsivo.
        </p>
      </div>
    </div>
  `,
  styles: [`
    .page-container {
      display: flex;
      flex-direction: column;
      gap: var(--space-6);
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

    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: var(--space-4);
    }

    .metric-card {
      padding: var(--space-5);
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
    }

    .metric-label {
      font-size: var(--font-size-xs);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--color-text-muted);
    }

    .metric-value {
      font-size: var(--font-size-2xl);
      font-weight: 700;
      color: var(--color-text);
    }

    .welcome-card {
      padding: var(--space-6);
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
    }

    .card-title {
      font-size: var(--font-size-lg);
      font-weight: 600;
      color: var(--color-text);
    }

    .card-text {
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
      line-height: 1.6;
    }
  `]
})
export class DashboardComponent {}
