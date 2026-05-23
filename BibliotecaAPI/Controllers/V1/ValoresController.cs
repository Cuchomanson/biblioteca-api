using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ValoresController : ControllerBase
    {
        private readonly ServicioTransient transient1;
        private readonly ServicioTransient transient2;
        private readonly ServicioScoped scoped1;
        private readonly ServicioScoped scoped2;
        private readonly ServicioSingleton singleton;

        public ValoresController(ServicioTransient transient1,
            ServicioTransient transient2,
            ServicioScoped scoped1,
            ServicioScoped scoped2,
            ServicioSingleton singleton)
        {
            this.transient1 = transient1;
            this.transient2 = transient2;
            this.scoped1 = scoped1;
            this.scoped2 = scoped2;
            this.singleton = singleton;
        }

        [HttpGet("servicios-tiempos-de-vida")]
        public IActionResult GetServiciosTiemposDeVida()
        {
            return Ok(new
            {
                Transients = new
                {
                    Transient1 = transient1.Id,
                    Transient2 = transient2.Id
                },
                Scopeds = new
                {
                    Scoped1 = scoped1.Id,
                    Scoped2 = scoped2.Id
                },
                Singleton = singleton.Id
            });
        }
    }
}
