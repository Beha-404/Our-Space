import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../shared/toast/toast.service';

const AUTH_ENDPOINTS = [
  '/auth/login',
  '/auth/register',
  '/auth/refresh',
  '/auth/logout',
  '/auth/forgot-password',
  '/auth/reset-password',
];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  if (AUTH_ENDPOINTS.some(path => req.url.includes(path))) {
    return next(req);
  }

  const withToken = (token: string | null) =>
    token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(withToken(authService.getToken())).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      return authService.refreshSession().pipe(
        switchMap(success => {
          if (success) {
            return next(withToken(authService.getToken()));
          }

          if (authService.clearSession()) {
            toast.error('toast.sessionExpired');
          }

          router.navigate(['/login']);

          return throwError(() => error);
        })
      );
    })
  );
};
