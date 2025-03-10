import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SeguridadService } from './seguridad.service';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const seguridadService = inject(SeguridadService);
    const router = inject(Router);

    return next(req).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401) {
                console.error('Error 401 - No autorizado. Redirigiendo al login.');
                seguridadService.logout();
                router.navigate(['/login']);
            }
            return throwError(() => error);
        })
    );
};
