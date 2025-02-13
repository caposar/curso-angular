export interface ActorDTO {
    id: number;
    nombre: string;
    fechaNacimiento: Date;
    foto?: string;
}

export interface ActorCreacionDTO {
    nombre: string;
    fechaNacimiento: Date;
    foto?: File;
}