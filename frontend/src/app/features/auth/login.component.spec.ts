import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { LoginComponent } from './login.component';
import { AuthService } from '@core';

describe('LoginComponent', () => {
  let routerSpy: { navigateByUrl: ReturnType<typeof vi.fn> };
  let authServiceSpy: { login: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    routerSpy = { navigateByUrl: vi.fn() };
    authServiceSpy = { login: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authServiceSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: (key: string) => (key === 'returnUrl' ? '/inventory' : null)
              }
            }
          }
        }
      ]
    }).compileComponents();
  });

  it('should create login component with empty form and idle state', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(component.form.valid).toBe(false);
    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBeNull();
  });

  it('should validate email and password as required fields', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    const emailControl = component.form.controls.email;
    const passwordControl = component.form.controls.password;

    expect(emailControl.valid).toBe(false);
    expect(emailControl.hasError('required')).toBe(true);

    emailControl.setValue('not-an-email');
    expect(emailControl.hasError('email')).toBe(true);

    emailControl.setValue('valid@sistema.com');
    expect(emailControl.valid).toBe(true);

    expect(passwordControl.hasError('required')).toBe(true);
    passwordControl.setValue('SecurePass123!');
    expect(passwordControl.valid).toBe(true);

    expect(component.form.valid).toBe(true);
  });

  it('should not submit when form is invalid', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.onSubmit();

    expect(authServiceSpy.login).not.toHaveBeenCalled();
    expect(component.isLoading()).toBe(false);
  });

  it('should call authService.login and navigate to returnUrl on successful submission', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.setValue({
      email: 'admin@sistema.com',
      password: 'Password123!'
    });

    authServiceSpy.login.mockReturnValue(
      of({
        userId: '11111111-1111-1111-1111-111111111111',
        roles: ['Admin'],
        accessToken: 'token',
        expiresAtUtc: '2026-10-22T21:00:00Z'
      })
    );

    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({
      email: 'admin@sistema.com',
      password: 'Password123!'
    });
    expect(component.isLoading()).toBe(false);
    expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/inventory');
  });

  it('should display error alert when authentication fails', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.setValue({
      email: 'admin@sistema.com',
      password: 'WrongPassword'
    });

    const error = new HttpErrorResponse({
      status: 401,
      error: {
        detail: 'The email or password is invalid.'
      }
    });

    authServiceSpy.login.mockReturnValue(throwError(() => error));

    component.onSubmit();
    fixture.detectChanges();

    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBe('The email or password is invalid.');
    expect(routerSpy.navigateByUrl).not.toHaveBeenCalled();

    const alert = fixture.nativeElement.querySelector('.alert-danger');
    expect(alert).toBeTruthy();
    expect(alert.textContent).toContain('The email or password is invalid.');
  });
});
