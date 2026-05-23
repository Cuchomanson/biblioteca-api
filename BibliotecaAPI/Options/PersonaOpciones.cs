using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Options
{
    public class PersonaOpciones
    {
        public const string Seccion = "seccion_1";

        [Required]
        public required string Nombre { get; set; }
        public int Edad { get; set; }
    }
}