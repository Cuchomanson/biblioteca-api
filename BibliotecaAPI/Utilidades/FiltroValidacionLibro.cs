using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroValidacionLibro : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _dbContext;

        public FiltroValidacionLibro(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Si no llega la variable o no es del tipo LibroCreacionDTO
            if (!context.ActionArguments.TryGetValue("libroCreacionDto", out var value) || value is not LibroCreacionDTO libroCreacionDto)
            {
                context.ModelState.AddModelError(string.Empty, "El modelo enviado no es válido o no se ha proporcionado");
                context.Result = context.ModelState.ContruirProblemDetails();
                return;
            }

            if (libroCreacionDto.AutoresIds == null || libroCreacionDto.AutoresIds.Count == 0)
            {
                context.ModelState.AddModelError(nameof(libroCreacionDto.AutoresIds), "Debe especificar al menos un autor");
                context.Result = context.ModelState.ContruirProblemDetails();
                return;
            }

            var autoresIdsExisten = await _dbContext.Autores
                .Where(x => libroCreacionDto.AutoresIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (autoresIdsExisten.Count != libroCreacionDto.AutoresIds.Count)
            {
                var autoresNoExisten = libroCreacionDto.AutoresIds.Except(autoresIdsExisten);
                var autoresNoExistenString = string.Join(", ", autoresNoExisten);

                context.ModelState.AddModelError(nameof(libroCreacionDto.AutoresIds), $"Los siguientes autores no existen: {autoresNoExistenString}");
                context.Result = context.ModelState.ContruirProblemDetails();
                return;
            }

            await next();
        }
    }
}
