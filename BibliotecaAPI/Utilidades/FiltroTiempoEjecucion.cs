using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroTiempoEjecucion : IAsyncActionFilter
    {
        private readonly ILogger<FiltroTiempoEjecucion> _logger;

        public FiltroTiempoEjecucion(ILogger<FiltroTiempoEjecucion> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Antes de la ejecución de la acción
            var stopWath = Stopwatch.StartNew();
            _logger.LogInformation($"INICIO Accion: {context.ActionDescriptor.DisplayName}");

            // Inicio de ejecución del siguien filtro o acción
            await next();

            //Después de la ejecución de la acción
            stopWath.Stop();
            _logger.LogInformation($"FIN Accion: {context.ActionDescriptor.DisplayName} - Tiempo: {stopWath.ElapsedMilliseconds} ms");
        }
    }
}
