import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@core';
import { parseApiError } from '@core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="auth-page">
      <div class="auth-card card">
        <div class="auth-header">
          <div class="brand-badge">SIV</div>
          <h1 class="auth-title">Iniciar Sesión</h1>
          <p class="auth-subtitle">Sistema de Inventario y Ventas</p>
        </div>

        @if (errorMessage()) {
          <div class="alert alert-danger" role="alert" aria-live="assertive">
            <svg viewBox="0 0 24 24" width="18" height="18" stroke="currentColor" stroke-width="2" fill="none" aria-hidden="true">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{{ errorMessage() }}</span>
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-form" novalidate>
          <div class="form-group">
            <label for="email" class="form-label">Correo Electrónico</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('email')"
              autocomplete="username"
              placeholder="admin@sistema.com"
              aria-required="true"
              [attr.aria-invalid]="isFieldInvalid('email')"
              aria-describedby="email-error"
            />
            @if (isFieldInvalid('email')) {
              <div id="email-error" class="field-error" role="alert">
                @if (form.get('email')?.hasError('required')) {
                  El correo electrónico es requerido.
                } @else if (form.get('email')?.hasError('email')) {
                  Ingrese un correo electrónico válido.
                }
              </div>
            }
          </div>

          <div class="form-group">
            <label for="password" class="form-label">Contraseña</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('password')"
              autocomplete="current-password"
              placeholder="••••••••"
              aria-required="true"
              [attr.aria-invalid]="isFieldInvalid('password')"
              aria-describedby="password-error"
            />
            @if (isFieldInvalid('password')) {
              <div id="password-error" class="field-error" role="alert">
                La contraseña es requerida.
              </div>
            }
          </div>

          <button
            type="submit"
            class="btn btn-primary submit-btn"
            [disabled]="isLoading() || form.invalid"
            [attr.aria-busy]="isLoading()"
          >
            @if (isLoading()) {
              <span class="spinner" aria-hidden="true"></span>
              <span>Iniciando sesión...</span>
            } @else {
              <span>Acceder al Sistema</span>
            }
          </button>
        </form>

        <div class="auth-footer">
          <span class="auth-note">Acceso restringido a usuarios autorizados</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: var(--space-4);
      background-color: var(--color-bg);
    }

    .auth-card {
      width: 100%;
      max-width: 420px;
      padding: var(--space-8);
      background-color: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-md);
    }

    .auth-header {
      text-align: center;
      margin-bottom: var(--space-6);
    }

    .brand-badge {
      display: inline-block;
      background-color: var(--color-primary);
      color: var(--color-primary-contrast);
      font-size: var(--font-size-sm);
      font-weight: 800;
      padding: 0.35rem 0.75rem;
      border-radius: var(--radius-md);
      margin-bottom: var(--space-3);
      letter-spacing: 0.05em;
    }

    .auth-title {
      font-size: var(--font-size-xl);
      font-weight: 700;
      color: var(--color-text);
      margin-bottom: var(--space-1);
    }

    .auth-subtitle {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
    }

    .alert {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      margin-bottom: var(--space-4);
      line-height: 1.4;
    }

    .alert-danger {
      background-color: var(--color-danger-light);
      color: var(--color-danger-dark);
      border: 1px solid var(--color-danger);
    }

    .auth-form {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }

    .form-label {
      font-size: var(--font-size-xs);
      font-weight: 600;
      color: var(--color-text);
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .form-control {
      width: 100%;
      padding: var(--space-2) var(--space-3);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-md);
      background-color: var(--color-surface);
      color: var(--color-text);
      font-size: var(--font-size-sm);
      transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
      outline: none;
    }

    .form-control:focus {
      border-color: var(--color-primary);
      box-shadow: 0 0 0 3px var(--color-primary-light);
    }

    .form-control.is-invalid {
      border-color: var(--color-danger);
    }

    .form-control.is-invalid:focus {
      box-shadow: 0 0 0 3px var(--color-danger-light);
    }

    .field-error {
      font-size: var(--font-size-xs);
      color: var(--color-danger);
      margin-top: 2px;
    }

    .submit-btn {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
      padding: var(--space-3);
      font-size: var(--font-size-sm);
      font-weight: 600;
      margin-top: var(--space-2);
    }

    .submit-btn:disabled {
      opacity: 0.65;
      cursor: not-allowed;
    }

    .spinner {
      width: 16px;
      height: 16px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-radius: 50%;
      border-top-color: #fff;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }

    .auth-footer {
      text-align: center;
      margin-top: var(--space-6);
      padding-top: var(--space-4);
      border-top: 1px solid var(--color-border);
    }

    .auth-note {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
    }
  `]
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  public readonly isLoading = signal<boolean>(false);
  public readonly errorMessage = signal<string | null>(null);

  public readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  public isFieldInvalid(fieldName: 'email' | 'password'): boolean {
    const field = this.form.get(fieldName);
    return !!field && field.invalid && (field.dirty || field.touched);
  }

  public onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.form.getRawValue();

    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.errorMessage.set(parseApiError(error));
      }
    });
  }
}
