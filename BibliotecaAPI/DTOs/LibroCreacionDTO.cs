using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class LibroCreacionDTO
    {
        [Required]
        [StringLength(250, ErrorMessage = "El campo {0} no puede tener más de {1} caracteres")]
        public required string Titulo { get; set; }

        public List<int> AutoresIds { get; set; } = [];
    }
}
