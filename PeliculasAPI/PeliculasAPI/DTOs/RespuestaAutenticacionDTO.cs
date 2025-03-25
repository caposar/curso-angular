namespace PeliculasAPI.DTOs
{
    /// <summary>
    /// Contiene la información de autenticación
    /// </summary>
    public class RespuestaAutenticacionDTO
    {
        /// <summary>
        /// Token de acceso para autenticación
        /// </summary>
        public required string AccessToken { get; set; }

        /// <summary>
        /// Token de renovación para obtener un nuevo token de acceso
        /// </summary>
        public required string RefreshToken { get; set; }
    }
}
