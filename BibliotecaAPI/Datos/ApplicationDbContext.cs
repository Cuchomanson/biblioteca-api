using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Datos
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }


        protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Nunca se debe borrar
            base.OnModelCreating(modelBuilder);

            //Configuración por API Fluent en lugar de DataAnnotations

            modelBuilder.Entity<Autor>()
                .Property(x => x.Nombre).HasMaxLength(150);

            //Filtro global para no obtener los comentarios borrados (lógico)
            modelBuilder.Entity<Comentario>()
                .HasQueryFilter(x => !x.EstaBorrado);
        }


        public DbSet<Autor> Autores { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<AutorLibro> AutoresLibros { get; set; }
        public DbSet<Error> Errores { get; set; }
    }
}
