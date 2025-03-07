import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SeguridadService {

  constructor() { }

  estaLogueado(): boolean {
    return true;
  }

  obtenerRol(): string {
    return ''
  }
}
