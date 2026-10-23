import { HttpErrorResponse } from '@angular/common/http';
import { parseApiError } from './api-error';

describe('parseApiError', () => {
  it('should parse validation errors from ValidationProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          Email: ['The Email field is required.']
        }
      }
    });

    expect(parseApiError(error)).toBe('The Email field is required.');
  });

  it('should parse detail string from ProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 401,
      error: {
        title: 'Authentication failed',
        detail: 'The email or password is invalid.',
        status: 401
      }
    });

    expect(parseApiError(error)).toBe('The email or password is invalid.');
  });

  it('should parse title string when detail is missing', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Customer conflict',
        status: 409
      }
    });

    expect(parseApiError(error)).toBe('Customer conflict');
  });

  it('should return network connection message when status is 0', () => {
    const error = new HttpErrorResponse({ status: 0 });
    expect(parseApiError(error)).toContain('No se pudo conectar con el servidor');
  });

  it('should return standard status message when error payload is empty', () => {
    const error403 = new HttpErrorResponse({ status: 403 });
    expect(parseApiError(error403)).toContain('No tiene permisos suficientes');

    const error404 = new HttpErrorResponse({ status: 404 });
    expect(parseApiError(error404)).toContain('no fue encontrado');

    const error500 = new HttpErrorResponse({ status: 500 });
    expect(parseApiError(error500)).toContain('error en el servidor');
  });

  it('should handle standard Error object', () => {
    const error = new Error('Custom client exception');
    expect(parseApiError(error)).toBe('Custom client exception');
  });

  it('should return fallback message for unknown error types', () => {
    expect(parseApiError(null)).toBe('Ocurrió un error inesperado. Intente nuevamente.');
  });
});
