import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { MainLayoutComponent } from './main-layout.component';
import { AuthService } from '../auth/auth.service';

describe('MainLayoutComponent', () => {
  let mockAuthService: {
    session: ReturnType<typeof signal>;
    isAdmin: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockAuthService = {
      session: signal({
        userId: '11111111-2222-3333-4444-555555555555',
        roles: ['User'],
        accessToken: 'token',
        expiresAtUtc: '2026-10-22T20:00:00Z'
      }),
      isAdmin: vi.fn().mockReturnValue(false),
      logout: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create main layout component', () => {
    const fixture = TestBed.createComponent(MainLayoutComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should toggle sidebar state', () => {
    const fixture = TestBed.createComponent(MainLayoutComponent);
    const component = fixture.componentInstance;
    expect(component.isSidebarOpen()).toBe(true);

    component.onToggleSidebar();
    expect(component.isSidebarOpen()).toBe(false);

    component.onToggleSidebar();
    expect(component.isSidebarOpen()).toBe(true);
  });
});
