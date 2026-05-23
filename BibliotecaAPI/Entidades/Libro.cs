using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Entidades
{
    public class Libro
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(250, ErrorMessage = "El campo {0} no puede tener más de {1} caracteres")]
        public required string Titulo { get; set; }

        public List<AutorLibro> Autores { get; set; } = [];
        public List<Comentario> Comentarios { get; set; } = [];
    }
}
