using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1")]
    public class RouteController : ControllerBase
    {
        [HttpGet("ObtenerRootV1")]
        public IEnumerable<DatosHATEOASDTO> Get()   
        {
            var enlaces = new List<DatosHATEOASDTO>
            {
                new(Url.Link("ObtenerRootV1", new { })!, "Self", "GET"),
                new(Url.Link("obtenerAutores", new { })!, "Obtener autores", "GET")
            };
            return enlaces;
        }
    }
}
