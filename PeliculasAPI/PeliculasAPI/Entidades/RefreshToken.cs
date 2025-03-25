using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PeliculasAPI.Entidades
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; } // Clave primaria

        [Required]
        public string Token { get; set; } = null!; // Refresh Token seguro

        [Required]
        public string UserId { get; set; } = null!; // Usuario al que pertenece

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } = null!; // Referencia al usuario

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Fecha de creación
        public DateTime Expiration { get; set; } // Fecha de expiración del refresh token
        public bool Revoked { get; set; } = false; // Si el token fue revocado
        public DateTime? RevokedAt { get; set; } // Fecha de revocación (cuando se marcó como revocado)
    }
}
