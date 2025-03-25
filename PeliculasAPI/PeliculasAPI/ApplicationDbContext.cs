using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.Entidades;

namespace PeliculasAPI
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PeliculaGenero>().HasKey(e => new { e.GeneroId, e.PeliculaId });
            modelBuilder.Entity<PeliculaCine>().HasKey(e => new { e.CineId, e.PeliculaId });
            modelBuilder.Entity<PeliculaActor>().HasKey(e => new { e.ActorId, e.PeliculaId });

            modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique(); // Índice único para mejorar búsqueda y evitar duplicados

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina un usuario, sus tokens también se eliminan
        }

        public DbSet<Genero> Generos { get; set; }
        public DbSet<Actor> Actores { get; set; }
        public DbSet<Cine> Cines { get; set; }
        public DbSet<Pelicula> Peliculas { get; set; }
        public DbSet<PeliculaGenero> PeliculasGeneros { get; set; }
        public DbSet<PeliculaCine> PeliculasCines { get; set; }
        public DbSet<PeliculaActor> PeliculasActores { get; set; }
        public DbSet<Rating> RatingsPeliculas { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
