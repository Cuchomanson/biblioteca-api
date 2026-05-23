using BibliotecaAPI.Validaciones;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Entidades
{
    //Las validaciones por modelo, solo se ejecutan después de las de atributo y solo si son exitosas- 
    public class Autor : IValidatableObject
    {
        public int Id { get; set; }

        //Concepto placeholder, el {0} se reemplaza por el nombre de la propiedad. Si no se proporciona un valor para la propiedad Nombre
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(150, ErrorMessage = "El campo {0} no puede tener más de {1} caracteres")]
        [PrimeraLetraMayuscula]
        // required: Indica que para instancia la clase Autor, es necesario proporcionar un valor para la propiedad Nombre.
        public required string Nombre { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(150, ErrorMessage = "El campo {0} no puede tener más de {1} caracteres")]
        [PrimeraLetraMayuscula]
        public required string Apellidos { get; set; }

        [StringLength(20, ErrorMessage = "El campo {0} no puede tener más de {1} caracteres")]
        public string? Identificacion { get; set; }

        [Unicode(false)] //Indica que la propiedad Foto no debe ser almacenada como texto Unicode en la base de datos, lo que puede ser útil para optimizar el almacenamiento si se espera que los datos sean principalmente ASCII.
        public string? Foto { get; set; }

        public List<AutorLibro>? Libros { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Nombre))
            {
                var primeraLetra = Nombre[0].ToString();
                if (primeraLetra != primeraLetra.ToUpper())
                {
                    //Yield permite ir generando el listado de errores de validación a medida que se van encontrando, en lugar de tener que esperar a que se complete toda la validación para devolver el resultado.
                    yield return new ValidationResult("La primera letra debe ser mayúscula", new[] { nameof(Nombre) });
                }
            }
        }
    }
}
