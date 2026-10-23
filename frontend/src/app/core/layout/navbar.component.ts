import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="navbar">
      <div class="navbar-left">
        <button
          type="button"
          class="menu-toggle-btn"
          (click)="toggleSidebar.emit()"
          aria-label="Alternar menú de navegación"
        >
          <svg viewBox="0 0 24 24" width="22" height="22" stroke="currentColor" stroke-width="2" fill="none">
            <line x1="3" y1="12" x2="21" y2="12"></line>
            <line x1="3" y1="6" x2="21" y2="6"></line>
            <line x1="3" y1="18" x2="21" y2="18"></line>
          </svg>
        </button>
        <a routerLink="/" class="navbar-brand">
          <span class="brand-badge">SIV</span>
          <span class="brand-title">Sistema de Inventario y Ventas</span>
        </a>
      </div>

      <div class="navbar-right">
        <div class="user-profile">
          <div class="user-avatar" aria-hidden="true">
            {{ userInitial() }}
          </div>
          <div class="user-info">
            <span class="user-name">{{ userDisplayName() }}</span>
            <span class="user-role badge" [class.badge-primary]="authService.isAdmin()" [class.badge-info]="!authService.isAdmin()">
              {{ roleDisplayName() }}
            </span>
          </div>
        </div>

        <button
          type="button"
          class="btn-logout"
          (click)="onLogout()"
          aria-label="Cerrar sesión"
          title="Cerrar sesión"
        >
          <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
            <polyline points="16 17 21 12 16 7"></polyline>
            <line x1="21" y1="12" x2="9" y2="12"></line>
          </svg>
          <span class="logout-label">Salir</span>
        </button>
      </div>
    </header>
  `,
  styles: [`
    .navbar {
      height: var(--navbar-height);
      background-color: var(--color-surface);
      border-bottom: 1px solid var(--color-border);
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 var(--space-4);
      position: sticky;
      top: 0;
      z-index: 40;
    }

    .navbar-left, .navbar-right {
      display: flex;
      align-items: center;
      gap: var(--space-3);
    }

    .menu-toggle-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 38px;
      height: 38px;
      border-radius: var(--radius-md);
      color: var(--color-text);
      transition: background-color var(--transition-fast);
    }

    .menu-toggle-btn:hover {
      background-color: var(--color-surface-hover);
    }

    .navbar-brand {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-weight: 700;
      color: var(--color-text);
      font-size: var(--font-size-base);
    }

    .brand-badge {
      background-color: var(--color-primary);
      color: var(--color-primary-contrast);
      font-size: var(--font-size-xs);
      font-weight: 800;
      padding: 0.2rem 0.45rem;
      border-radius: var(--radius-sm);
      letter-spacing: 0.05em;
    }

    .user-profile {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    .user-avatar {
      width: 34px;
      height: 34px;
      border-radius: var(--radius-full);
      background-color: var(--color-primary-light);
      color: var(--color-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: var(--font-size-xs);
      font-weight: 700;
    }

    .user-info {
      display: flex;
      flex-direction: column;
      line-height: 1.2;
    }

    .user-name {
      font-size: var(--font-size-xs);
      font-weight: 600;
      color: var(--color-text);
    }

    .btn-logout {
      display: flex;
      align-items: center;
      gap: var(--space-1);
      padding: var(--space-1) var(--space-2);
      border-radius: var(--radius-md);
      color: var(--color-text-muted);
      font-size: var(--font-size-xs);
      font-weight: 500;
      border: 1px solid var(--color-border);
      background-color: var(--color-surface);
      cursor: pointer;
      transition: color var(--transition-fast), background-color var(--transition-fast), border-color var(--transition-fast);
    }

    .btn-logout:hover {
      color: var(--color-danger);
      background-color: var(--color-danger-light);
      border-color: var(--color-danger);
    }

    @media (max-width: 640px) {
      .brand-title {
        display: none;
      }
      .user-info {
        display: none;
      }
      .logout-label {
        display: none;
      }
    }
  `]
})
export class NavbarComponent {
  public readonly authService = inject(AuthService);
  public readonly toggleSidebar = output<void>();

  public userInitial(): string {
    const roles = this.authService.session()?.roles;
    if (roles?.includes('Admin')) {
      return 'AD';
    }
    return 'US';
  }

  public userDisplayName(): string {
    const session = this.authService.session();
    if (!session) {
      return 'Invitado';
    }
    return `Usuario (${session.userId.slice(0, 8)})`;
  }

  public roleDisplayName(): string {
    const roles = this.authService.session()?.roles;
    if (roles?.includes('Admin')) {
      return 'Administrador';
    }
    if (roles && roles.length > 0) {
      return roles.join(', ');
    }
    return 'Usuario';
  }

  public onLogout(): void {
    this.authService.logout();
  }
}
