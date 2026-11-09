import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const isBrowser = typeof window !== 'undefined' && !!window.localStorage;
  const token = isBrowser ? localStorage.getItem('marketing_api_token') : null;
  const router = inject(Router);

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  return next(req).pipe(
    catchError((error: any) => {
      if (error.status === 401 && !req.url.includes('/login')) {
        if (isBrowser) {
          localStorage.removeItem('marketing_api_token');
          localStorage.removeItem('marketing_api_user');
        }
        router.navigateByUrl('/login');
      }
      return throwError(() => error);
    })
  );
};
