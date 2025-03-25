import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { SeguridadService } from './seguridad.service';

/**
 * Interceptor que maneja errores 401, intenta refrescar el token y reintenta la petición original.
 */
export const tokenRefreshInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn) => {
  const seguridadService = inject(SeguridadService);

  console.log('[Interceptor] Enviando petición:', req.url);

  return next(req).pipe(
    catchError((err) => {
      console.error('[Interceptor] Error detectado en petición:', req.url, err);

      debugger;
      // Si la respuesta es un error 401 (No autorizado), intentamos refrescar el token.
      if (err instanceof HttpErrorResponse && err.status === 401) {
        console.warn('[Interceptor] Token expirado. Intentando refrescar...');

        return seguridadService.refreshToken().pipe(
          switchMap((res) => {
            console.log('[Interceptor] Nuevo token obtenido:', res.accessToken);

            // Guardamos el nuevo token recibido.
            seguridadService.guardarToken(res);
  
            // Clonamos la petición original y le agregamos el nuevo token.
            const newReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${res.accessToken}`
              }
            });

            console.log('[Interceptor] Reintentando petición con nuevo token:', newReq.url);
  
            // Reintentamos la petición con el nuevo token.
            return next(newReq);
          }),
          catchError((refreshErr) => {
            console.error('[Interceptor] Error al refrescar token. Forzando logout.', refreshErr);

            // Si el refreshToken también falla, limpiamos el almacenamiento y forzamos logout.
              
            seguridadService.limpiarStorage();
  
            return throwError(() => new Error(refreshErr));
          })
        )
      }

      // Si el error no es 401, lo propagamos sin modificarlo.
      console.log('[Interceptor] Error no manejado por refreshToken:', err.status);
      return throwError(() => err);
    })
  );
};