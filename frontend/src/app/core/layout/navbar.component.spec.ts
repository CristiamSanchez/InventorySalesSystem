import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { NavbarComponent } from './navbar.component';
import { AuthService } from '../auth/auth.service';

describe('NavbarComponent', () => {
  let mockAuthService: {
    session: ReturnType<typeof signal>;
    isAdmin: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockAuthService = {
      session: signal({
        userId: '12345678-aaaa-bbbb-cccc-dddddddddddd',
        roles: ['Admin'],
        accessToken: 'mock-token',
        expiresAtUtc: '2026-10-22T20:00:00Z'
      }),
      isAdmin: vi.fn().mockReturnValue(true),
      logout: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [NavbarComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create navbar component', () => {
    const fixture = TestBed.createComponent(NavbarComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should display admin user information and role badge', () => {
    const fixture = TestBed.createComponent(NavbarComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.user-avatar')?.textContent?.trim()).toBe('AD');
    expect(compiled.querySelector('.user-name')?.textContent?.trim()).toBe('Usuario (12345678)');
    expect(compiled.querySelector('.user-role')?.textContent?.trim()).toBe('Administrador');
  });

  it('should emit toggleSidebar when menu button is clicked', () => {
    const fixture = TestBed.createComponent(NavbarComponent);
    const component = fixture.componentInstance;
    let emitted = false;
    component.toggleSidebar.subscribe(() => {
      emitted = true;
    });

    const button = fixture.nativeElement.querySelector('.menu-toggle-btn') as HTMLButtonElement;
    expect(button).toBeTruthy();
    button.click();

    expect(emitted).toBe(true);
  });

  it('should call authService.logout when logout button is clicked', () => {
    const fixture = TestBed.createComponent(NavbarComponent);
    fixture.detectChanges();

    const logoutBtn = fixture.nativeElement.querySelector('.btn-logout') as HTMLButtonElement;
    expect(logoutBtn).toBeTruthy();
    logoutBtn.click();

    expect(mockAuthService.logout).toHaveBeenCalled();
  });
});
