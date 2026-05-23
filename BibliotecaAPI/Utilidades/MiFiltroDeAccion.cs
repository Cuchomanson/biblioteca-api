using Microsoft.AspNetCore.Mvc.Filters;

namespace BibliotecaAPI.Utilidades
{
    public class MiFiltroDeAccion : IActionFilter
    {
        private readonly ILogger<MiFiltroDeAccion> _logger;

        public MiFiltroDeAccion(ILogger<MiFiltroDeAccion> logger)
        {
            _logger = logger;
        }


        // Este método se ejecutará antes de que se ejecute la acción
        public void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.LogInformation("Ejecutando la acción");
        }


        // Este método se ejecutará después de que se ejecute la acción
        public void OnActionExecuted(ActionExecutedContext context)
        {
            _logger.LogInformation("Acción ejecutada");
        }
    }
}
