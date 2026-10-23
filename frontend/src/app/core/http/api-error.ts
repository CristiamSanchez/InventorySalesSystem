import { HttpErrorResponse } from '@angular/common/http';

/**
 * Standard RFC 7807 Problem Details representation.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

/**
 * Safely parses backend errors and ProblemDetails into a user-friendly message.
 * Excludes sensitive technical details, connection strings, and stack traces.
 */
export function parseApiError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'No se pudo conectar con el servidor. Verifique su conexión de red.';
    }

    const payload = error.error as ProblemDetails | string | null;

    if (payload && typeof payload === 'object') {
      // 1. Validation problem details with field errors
      if (payload.errors && typeof payload.errors === 'object') {
        const errorList = Object.values(payload.errors).flat();
        if (errorList.length > 0) {
          return errorList[0];
        }
      }

      // 2. Standard ProblemDetails detail
      if (typeof payload.detail === 'string' && payload.detail.trim().length > 0) {
        return payload.detail.trim();
      }

      // 3. ProblemDetails title
      if (typeof payload.title === 'string' && payload.title.trim().length > 0) {
        return payload.title.trim();
      }
    }

    if (typeof payload === 'string' && payload.trim().length > 0 && !payload.startsWith('<')) {
      return payload.trim();
    }

    switch (error.status) {
      case 400:
        return 'La solicitud contiene datos inválidos o incompletos.';
      case 401:
        return 'Credenciales inválidas o sesión no autorizada.';
      case 403:
        return 'No tiene permisos suficientes para realizar esta acción.';
      case 404:
        return 'El recurso solicitado no fue encontrado.';
      case 409:
        return 'Conflicto en la operación solicitada.';
      case 500:
      case 502:
      case 503:
        return 'Ocurrió un error en el servidor. Intente nuevamente más tarde.';
      default:
        return `Error inesperado (${error.status}).`;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Ocurrió un error inesperado. Intente nuevamente.';
}
