using BibliotecaAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1/configuraciones")]
    public class ConfiguracionesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection seccion_1;
        private readonly IConfigurationSection seccion_2;
        private readonly PersonaOpciones _opcionesPersona;
        private readonly PagosProcesamiento _pagosService;

        public ConfiguracionesController(IConfiguration configuration, IOptions<PersonaOpciones> opcionesPeronsa, PagosProcesamiento pagosService)
        {
            _configuration = configuration;
            seccion_1 = configuration.GetSection("Seccion_1");
            seccion_2 = configuration.GetSection("Seccion_2");
            _opcionesPersona = opcionesPeronsa.Value;

            _pagosService = pagosService;
        }


        [HttpGet]
        public ActionResult<string> Get()
        {
            var opcion1 = _configuration["apellido"];
            var opcion2 = _configuration.GetValue<string>("apellido")!;

            return opcion2;
        }


        [HttpGet("secciones")]
        public ActionResult GetSeccion()
        {
            var opcion1 = _configuration["ConnectionStrings:DefaultConnection"]; //Navegación por secciones
            var opcion2 = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection")!;

            var seccion = _configuration.GetSection("ConnectionStrings");
            var opcion3 = seccion["DefaultConnection"];

            return Ok(opcion1);
        }

        [HttpGet("seccion-01")]
        public ActionResult GetSeccion01()
        {
            var nombre = seccion_1.GetValue<string>("nombre");
            var edad = seccion_1.GetValue<int>("edad")!;
            return Ok(new { nombre, edad });
        }

        [HttpGet("seccion-02")]
        public ActionResult GetSeccion02()
        {
            var nombre = seccion_2.GetValue<string>("nombre");
            var edad = seccion_2.GetValue<int>("edad")!;
            return Ok(new { nombre, edad });
        }


        [HttpGet("obtenerTodos")]
        public ActionResult GetObtenerTodos()
        {
            var hijos = _configuration.GetChildren().Select(x => $"{x.Key}: {x.Value}");
            return Ok(hijos);
        }


        [HttpGet("proveedores")]
        public ActionResult GetProveedor()
        {
            var valor = _configuration.GetValue<string>("quien_soy");
            return Ok(valor);
        }

        [HttpGet("seccion-1-opciones")]
        public ActionResult GetSeccion1Opciones()
        {
            return Ok(_opcionesPersona);
        }


        [HttpGet("options-monitor")]
        public ActionResult GetTarifas()
        {
            return Ok(_pagosService.ObtenerTarifas());
        }
    }
}
