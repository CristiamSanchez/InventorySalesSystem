import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { catchError, firstValueFrom, of } from 'rxjs';
import { authInterceptor, AuthService, errorInterceptor } from '@core';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, errorInterceptor])
    ),
    provideRouter(routes, withComponentInputBinding()),
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      return firstValueFrom(
        authService.initializeSession().pipe(catchError(() => of(false)))
      );
    })
  ]
};
