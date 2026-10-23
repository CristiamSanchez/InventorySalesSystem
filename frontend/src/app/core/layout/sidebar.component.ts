import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

export interface NavItem {
  label: string;
  path: string;
  exact?: boolean;
  icon: 'dashboard' | 'categories' | 'products' | 'customers' | 'suppliers' | 'inventory' | 'sales' | 'reports' | 'audit';
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="sidebar" [class.collapsed]="!isOpen()" [class.mobile-open]="isMobileOpen()">
      <div class="sidebar-header">
        <span class="sidebar-section-title">Menú Principal</span>
        <button
          type="button"
          class="mobile-close-btn"
          (click)="closeMobile.emit()"
          aria-label="Cerrar menú lateral"
        >
          <svg viewBox="0 0 24 24" width="20" height="20" stroke="currentColor" stroke-width="2" fill="none">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
      </div>

      <nav class="sidebar-nav" aria-label="Navegación principal">
        <ul class="nav-list">
          @for (item of navItems; track item.path) {
            <li class="nav-item">
              <a
                [routerLink]="item.path"
                routerLinkActive="active"
                [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
                class="nav-link"
                (click)="onNavigate()"
              >
                <span class="nav-icon" aria-hidden="true">
                  @switch (item.icon) {
                    @case ('dashboard') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <rect x="3" y="3" width="7" height="7"></rect>
                        <rect x="14" y="3" width="7" height="7"></rect>
                        <rect x="14" y="14" width="7" height="7"></rect>
                        <rect x="3" y="14" width="7" height="7"></rect>
                      </svg>
                    }
                    @case ('categories') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <line x1="8" y1="6" x2="21" y2="6"></line>
                        <line x1="8" y1="12" x2="21" y2="12"></line>
                        <line x1="8" y1="18" x2="21" y2="18"></line>
                        <line x1="3" y1="6" x2="3.01" y2="6"></line>
                        <line x1="3" y1="12" x2="3.01" y2="12"></line>
                        <line x1="3" y1="18" x2="3.01" y2="18"></line>
                      </svg>
                    }
                    @case ('products') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
                        <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
                        <line x1="12" y1="22.08" x2="12" y2="12"></line>
                      </svg>
                    }
                    @case ('customers') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                        <circle cx="9" cy="7" r="4"></circle>
                        <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                        <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
                      </svg>
                    }
                    @case ('suppliers') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <rect x="1" y="3" width="15" height="13"></rect>
                        <polygon points="16 8 20 8 23 11 23 16 16 16 16 8"></polygon>
                        <circle cx="5.5" cy="18.5" r="2.5"></circle>
                        <circle cx="18.5" cy="18.5" r="2.5"></circle>
                      </svg>
                    }
                    @case ('inventory') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
                        <line x1="3" y1="6" x2="21" y2="6"></line>
                        <path d="M16 10a4 4 0 0 1-8 0"></path>
                      </svg>
                    }
                    @case ('sales') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <circle cx="9" cy="21" r="1"></circle>
                        <circle cx="20" cy="21" r="1"></circle>
                        <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                      </svg>
                    }
                    @case ('reports') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <line x1="18" y1="20" x2="18" y2="10"></line>
                        <line x1="12" y1="20" x2="12" y2="4"></line>
                        <line x1="6" y1="20" x2="6" y2="14"></line>
                      </svg>
                    }
                    @case ('audit') {
                      <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
                        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                      </svg>
                    }
                  }
                </span>
                <span class="nav-label">{{ item.label }}</span>
              </a>
            </li>
          }
        </ul>
      </nav>

      <div class="sidebar-footer">
        <div class="system-version">
          <span class="version-label">SIV v1.0.0</span>
          <span class="version-env badge badge-neutral">Frontend Core</span>
        </div>
      </div>
    </aside>

    @if (isMobileOpen()) {
      <div class="sidebar-backdrop" (click)="closeMobile.emit()" aria-hidden="true"></div>
    }
  `,
  styles: [`
    .sidebar {
      width: var(--sidebar-width);
      background-color: var(--color-surface);
      border-right: 1px solid var(--color-border);
      display: flex;
      flex-direction: column;
      height: calc(100vh - var(--navbar-height));
      position: sticky;
      top: var(--navbar-height);
      transition: width var(--transition-normal), transform var(--transition-normal);
      z-index: 30;
      flex-shrink: 0;
    }

    .sidebar.collapsed {
      width: 0;
      overflow: hidden;
      border-right: none;
    }

    .sidebar-header {
      padding: var(--space-4) var(--space-4) var(--space-2);
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .sidebar-section-title {
      font-size: var(--font-size-xs);
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: var(--color-text-muted);
    }

    .mobile-close-btn {
      display: none;
      color: var(--color-text-muted);
      padding: var(--space-1);
      border-radius: var(--radius-sm);
    }

    .mobile-close-btn:hover {
      color: var(--color-text);
      background-color: var(--color-surface-hover);
    }

    .sidebar-nav {
      flex: 1;
      overflow-y: auto;
      padding: var(--space-2) var(--space-2);
    }

    .nav-list {
      list-style: none;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .nav-link {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-2) var(--space-3);
      border-radius: var(--radius-md);
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
      font-weight: 500;
      text-decoration: none;
      transition: color var(--transition-fast), background-color var(--transition-fast);
      white-space: nowrap;
    }

    .nav-link:hover {
      color: var(--color-text);
      background-color: var(--color-surface-hover);
    }

    .nav-link.active {
      color: var(--color-primary);
      background-color: var(--color-primary-light);
      font-weight: 600;
    }

    .nav-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 20px;
      height: 20px;
      flex-shrink: 0;
    }

    .nav-label {
      flex: 1;
    }

    .sidebar-footer {
      padding: var(--space-3) var(--space-4);
      border-top: 1px solid var(--color-border);
      background-color: var(--color-surface);
    }

    .system-version {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .version-label {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      font-weight: 500;
    }

    .sidebar-backdrop {
      display: none;
    }

    @media (max-width: 768px) {
      .sidebar {
        position: fixed;
        top: 0;
        left: 0;
        bottom: 0;
        height: 100vh;
        z-index: 50;
        transform: translateX(-100%);
        width: 260px;
        box-shadow: var(--shadow-lg);
      }

      .sidebar.mobile-open {
        transform: translateX(0);
      }

      .mobile-close-btn {
        display: block;
      }

      .sidebar-backdrop {
        display: block;
        position: fixed;
        inset: 0;
        background-color: rgba(0, 0, 0, 0.45);
        z-index: 45;
      }
    }
  `]
})
export class SidebarComponent {
  public readonly isOpen = input<boolean>(true);
  public readonly isMobileOpen = input<boolean>(false);
  public readonly closeMobile = output<void>();

  public readonly navItems: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', exact: true, icon: 'dashboard' },
    { label: 'Categorías', path: '/categories', icon: 'categories' },
    { label: 'Productos', path: '/products', icon: 'products' },
    { label: 'Clientes', path: '/customers', icon: 'customers' },
    { label: 'Proveedores', path: '/suppliers', icon: 'suppliers' },
    { label: 'Inventario', path: '/inventory', icon: 'inventory' },
    { label: 'Ventas', path: '/sales', icon: 'sales' },
    { label: 'Reportes', path: '/reports', icon: 'reports' },
    { label: 'Auditoría', path: '/audit', icon: 'audit' }
  ];

  public onNavigate(): void {
    if (this.isMobileOpen()) {
      this.closeMobile.emit();
    }
  }
}
