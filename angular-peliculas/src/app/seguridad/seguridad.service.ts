import { HttpClient, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { PaginacionDTO } from '../compartidos/modelos/PaginacionDTO';
import { Observable, tap } from 'rxjs';
import { CredencialesUsuarioDTO, RespuestaAutenticacionDTO, UsuarioDTO } from './seguridad';
import { construirQueryParams } from '../compartidos/funciones/construirQueryParams';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class SeguridadService {

  constructor() { }

  private http = inject(HttpClient);
  private router = inject(Router);

  private urlBase = environment.apiURL + '/usuarios';
  private readonly llaveAccessToken = 'access_token';
  private readonly llaveRefreshToken = 'refresh_token';

  obtenerUsuariosPaginado(paginacion: PaginacionDTO): Observable<HttpResponse<UsuarioDTO[]>> {
    let queryParams = construirQueryParams(paginacion);
    return this.http.get<UsuarioDTO[]>(`${this.urlBase}/ListadoUsuarios`, { params: queryParams, observe: 'response' });
  }

  hacerAdmin(email: string) {
    return this.http.post(`${this.urlBase}/haceradmin`, { email });
  }

  removerAdmin(email: string) {
    return this.http.post(`${this.urlBase}/removeradmin`, { email });
  }

  obtenerToken(): string | null {
    return localStorage.getItem(this.llaveAccessToken);
  }

  registrar(credenciales: CredencialesUsuarioDTO): Observable<RespuestaAutenticacionDTO> {
    return this.http.post<RespuestaAutenticacionDTO>(`${this.urlBase}/registrar`, credenciales)
      .pipe(
        tap(respuestaAutenticacion => this.guardarToken(respuestaAutenticacion))
      )
  }

  login(credenciales: CredencialesUsuarioDTO): Observable<RespuestaAutenticacionDTO> {
    return this.http.post<RespuestaAutenticacionDTO>(`${this.urlBase}/login`, credenciales)
      .pipe(
        tap(respuestaAutenticacion => this.guardarToken(respuestaAutenticacion))
      )
  }

  obtenerCampoJWT(campo: string): string {
    const token = this.obtenerToken();
    if (!token) { return '' }
    var dataToken = JSON.parse(atob(token.split('.')[1]))
    return dataToken[campo];
  }

  guardarToken(respuestaAutenticacion: RespuestaAutenticacionDTO) {
    localStorage.setItem(this.llaveAccessToken, respuestaAutenticacion.accessToken);
    localStorage.setItem(this.llaveRefreshToken, respuestaAutenticacion.refreshToken);
    
    const expiracion = this.obtenerExpiracionToken(respuestaAutenticacion.accessToken);
    console.log("%cExpiracion access_token: " + expiracion, "color: #003366; background: #cce5ff; font-weight: bold; padding: 4px; border-radius: 4px;");
  }

  estaLogueado(): boolean {
    const token = this.obtenerToken();
    if (!token) return false;

    // const expiracion = localStorage.getItem(this.llaveExpiracion)!;
    // const expiacionFecha = new Date(expiracion);

    // if (expiacionFecha <= new Date()) {
    //   this.logout();
    //   return false;
    // }


    // Obtiene la expiración desde el token mismo
    // const expiracion = this.obtenerExpiracionToken(token);
    
    // if (!expiracion || expiracion <= new Date()) {
    //   // this.logout();
    //   return false;
    // }

    return true;
  }

  obtenerExpiracionToken(token: string): Date | null {
    try {
        const payloadBase64 = token.split('.')[1]; 
        const payloadJson = atob(payloadBase64); 
        const payload = JSON.parse(payloadJson);

        if (!payload.exp) return null;

        return new Date(payload.exp * 1000); // `exp` viene en segundos
    } catch (error) {
        return null;
    }
  }

  logout() {
    const refreshToken = localStorage.getItem(this.llaveRefreshToken);
  
    if (refreshToken) {
      this.http.post(`${this.urlBase}/logout`, { refreshToken }).subscribe({
        next: () => {
          console.log('Logout exitoso en el backend.');
          this.limpiarStorage()
        },
        error: (err) => {
          console.warn('Error en logout, limpiando sesión local...', err);
          this.limpiarStorage() // En caso de error, igual se limpia el storage
        }
      });
    } else {
      console.log('No había refresh token, limpiando sesión local...');
      this.limpiarStorage();
    }
  }

  limpiarStorage() {
    localStorage.removeItem(this.llaveAccessToken);
    localStorage.removeItem(this.llaveRefreshToken);
    this.router.navigate(['/login']);
  }

  obtenerRol(): string {
    const esAdmin = this.obtenerCampoJWT('esadmin');
    return esAdmin ? 'admin' : '';
  }

  refreshToken() {
    const refreshToken = localStorage.getItem(this.llaveRefreshToken);
    return this.http.post<RespuestaAutenticacionDTO>(`${this.urlBase}/renovar-token`, { refreshToken });
  }
  
}
