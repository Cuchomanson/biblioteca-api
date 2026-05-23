using Microsoft.AspNetCore.Mvc.Filters;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroAgregarCabecerasAttribute : ActionFilterAttribute
    {
        private readonly string _nombre;
        private readonly string _valor;

        public FiltroAgregarCabecerasAttribute(string nombre, string valor)
        {
            _nombre = nombre;
            _valor = valor;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            // Antes de la ejecución de la acción
            context.HttpContext.Response.Headers.Append(_nombre, _valor);

            // Ejecución de la acción
            base.OnResultExecuting(context);

            // Después de la ejecución de la acción
        }
    }
}
