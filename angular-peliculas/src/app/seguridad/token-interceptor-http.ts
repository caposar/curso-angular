import { HttpHandlerFn, HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { SeguridadService } from "./seguridad.service";

/**
 * Interceptor que agrega el access_token en el encabezado de autorización de cada petición HTTP.
 */
export const authInterceptor: HttpInterceptorFn = (
    req: HttpRequest<any>,
    next: HttpHandlerFn
) => {
    const seguridadService = inject(SeguridadService);
    const token = seguridadService.obtenerToken();

    // Si existe un token, clonamos la solicitud y agregamos el encabezado de autorización.
    if (token){
        const authReq = req.clone({
            setHeaders: {
                'Authorization': `Bearer ${token}`
            }
        });

        return next(authReq);
    }

    // Si no hay token, enviamos la solicitud sin modificaciones.
    return next(req);
}