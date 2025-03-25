export interface CredencialesUsuarioDTO {
    email: string;
    password: string;
}

export interface RespuestaAutenticacionDTO {
    accessToken: string;
    refreshToken: string;
}

export interface UsuarioDTO {
    email: string;
}
