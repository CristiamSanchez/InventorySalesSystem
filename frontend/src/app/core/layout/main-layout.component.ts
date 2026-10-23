import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar.component';
import { SidebarComponent } from './sidebar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, SidebarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="app-layout">
      <app-navbar (toggleSidebar)="onToggleSidebar()" />

      <div class="app-body">
        <app-sidebar
          [isOpen]="isSidebarOpen()"
          [isMobileOpen]="isMobileSidebarOpen()"
          (closeMobile)="closeMobileSidebar()"
        />

        <main class="main-content" role="main">
          <div class="main-container">
            <router-outlet />
          </div>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .app-layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      background-color: var(--color-bg);
      color: var(--color-text);
    }

    .app-body {
      display: flex;
      flex: 1;
      min-height: calc(100vh - var(--navbar-height));
      position: relative;
    }

    .main-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow-x: hidden;
      padding: var(--space-6);
      background-color: var(--color-bg);
    }

    .main-container {
      width: 100%;
      max-width: 1400px;
      margin: 0 auto;
    }

    @media (max-width: 768px) {
      .main-content {
        padding: var(--space-4);
      }
    }
  `]
})
export class MainLayoutComponent {
  public readonly isSidebarOpen = signal<boolean>(true);
  public readonly isMobileSidebarOpen = signal<boolean>(false);

  public onToggleSidebar(): void {
    if (typeof window !== 'undefined' && window.innerWidth <= 768) {
      this.isMobileSidebarOpen.update((open) => !open);
    } else {
      this.isSidebarOpen.update((open) => !open);
    }
  }

  public closeMobileSidebar(): void {
    this.isMobileSidebarOpen.set(false);
  }
}
