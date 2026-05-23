using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq.Dynamic.Core; // Para poder usar el OrderBy dinámico en el método Filtrar

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController] //Permite realizar validaciones de forma automática
    [Route("api/v2/autores")]
    [Authorize(Policy = "EsAdmin")]
    [FiltroAgregarCabeceras("controlador", "Autores")]
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AutoresController> _logger;
        private readonly IMapper _mapper;
        private readonly IAlmacenadorArchivos _almacenadorArchivos;
        private const string contenedor = "autores";
        private readonly IOutputCacheStore _outputCacheStore;
        private const string cache = "autores-obtener";

        public AutoresController(
            ApplicationDbContext context, 
            ILogger<AutoresController> logger, 
            IMapper mapper, 
            IAlmacenadorArchivos almacenadorArchivos,
            IOutputCacheStore outputCacheStore)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _almacenadorArchivos = almacenadorArchivos;
            _outputCacheStore = outputCacheStore;
        }


        [HttpGet("/v2/lista-autores")] // En este ejemplo tenemos una acción que puede ser accedida con 2 endpoints distintos
        [HttpGet]
        [AllowAnonymous]
        //[OutputCache(Tags = [cache])]
        [ServiceFilter<MiFiltroDeAccion>()] //Como usa inyección de dependencias, hay que poner ServiceFilter
        [FiltroAgregarCabeceras("accion", "ObtenerAutores")]
        public async Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            _logger.LogTrace("Obteniendo la lista de autores");
            _logger.LogDebug("Obteniendo la lista de autores");
            _logger.LogInformation("Obteniendo la lista de autores");
            _logger.LogWarning("Obteniendo la lista de autores");
            _logger.LogError("Obteniendo la lista de autores");
            _logger.LogCritical("Obteniendo la lista de autores");

            var queryable = _context.Autores.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);

            var autores = await queryable
                            .OrderBy(x => x.Nombre)
                            .Paginar(paginacionDTO)
                            .ToListAsync();

            var autoresDTO = _mapper.Map<IEnumerable<AutorDTO>>(autores);

            return autoresDTO;
        }


        [HttpGet("/v2/listado-de-autores")] // Al ponerle "/" automáticamente se salta el enrutado del controlador y es esa url: https://localhost:7298/listado-de-autores
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<IEnumerable<Autor>> GetListado()
        {
            return await _context.Autores.ToListAsync();
        }


        [HttpGet("v2/primero")]
        [AllowAnonymous]
        
        public async Task<Autor?> GetPrimerAutor()
        {
            return await _context.Autores.FirstOrDefaultAsync();
        }


        [HttpGet("v2/filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult> Filtrar([FromQuery] AutorFiltroDTO autorFiltroDTO)
        {
            var queryable = _context.Autores.AsQueryable();

            if (!string.IsNullOrEmpty(autorFiltroDTO.Nombres))
            {
                queryable = queryable.Where(x => x.Nombre.Contains(autorFiltroDTO.Nombres));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.Apellidos))
            {
                queryable = queryable.Where(x => x.Apellidos.Contains(autorFiltroDTO.Apellidos));
            }

            if (autorFiltroDTO.IncluirLibros)
            {
                queryable = queryable
                                .Include(x => x.Libros!)
                                .ThenInclude(x => x.Libro);
            }

            if (autorFiltroDTO.TieneFoto.HasValue)
            {
                queryable = autorFiltroDTO.TieneFoto.Value
                                ? queryable.Where(x => x.Foto != null)
                                : queryable.Where(x => x.Foto == null);
            }

            if (autorFiltroDTO.TieneLibros.HasValue)
            {
                queryable = autorFiltroDTO.TieneLibros.Value
                                ? queryable.Where(x => x.Libros!.Any())
                                : queryable.Where(x => !x.Libros!.Any());
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.TituloLibro))
            {
                queryable = queryable.Where(x => x.Libros!.Any(y => y.Libro!.Titulo.Contains(autorFiltroDTO.TituloLibro)));  
            }


            if (!string.IsNullOrEmpty(autorFiltroDTO.CampoOrdenar))
            {
                var tipoOrden = autorFiltroDTO.OrdenAscendente ? "ascending" : "descending";
                try
                {
                    queryable = queryable.OrderBy($"{autorFiltroDTO.CampoOrdenar} {tipoOrden}");
                }
                catch(Exception ex)
                {
                    queryable = queryable.OrderBy(x => x.Nombre);
                    _logger.LogError(ex.Message, ex);
                }
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Nombre);
            }


            var autores = await queryable
                            .Paginar(autorFiltroDTO.PaginacionDto)
                            .ToListAsync();

            if (autorFiltroDTO.IncluirLibros)
            {
                var autoresDto = _mapper.Map<IEnumerable<AutorConLibrosDTO>>(autores);
                return Ok(autoresDto);
            }
            else
            {
                var autoresDto = _mapper.Map<IEnumerable<AutorDTO>>(autores);
                return Ok(autoresDto);
            }
        }


        [HttpGet("{id:int}", Name = "ObtenerAutorV2")] //api/autores/id
        [AllowAnonymous]
        [EndpointSummary("Obtiene un autor por su id")]
        [EndpointDescription("Obtiene un autor por su Id incluyendo sus libros. Si el autor no existe, se retorna un 404")]
        [ProducesResponseType<AutorConLibrosDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AutorConLibrosDTO>> Get([Description("El id del autor")]int id, bool incluirLibros = false)
        {
            var queryable = _context.Autores.AsQueryable();

            if (incluirLibros)
            {
                queryable = queryable
                            .Include(x => x.Libros!)
                            .ThenInclude(x => x.Libro);
            }

            var autor = await queryable.FirstOrDefaultAsync(x => x.Id == id);

            if (autor is null)
            {
                return NotFound();
            }

            var autorDto = _mapper.Map<AutorConLibrosDTO>(autor);
            return autorDto;
        }


        //[HttpGet("{nombre:alpha}")] //api/autores/nombre -> Alpha es para podere recibir un parámetro de tipo string, no se puede poner string
        //[AllowAnonymous]
        //public async Task<IEnumerable<Autor>> Get(string nombre)
        //{
        //    return await _context.Autores.Where(x => x.Nombre.Contains(nombre)).ToListAsync();
        //}


        //[HttpGet("{parametro1}/{parametro2?}")] //api/autores/Hola/Cucho // Parámetro opcional
        //[AllowAnonymous]
        //public ActionResult Get(string parametro1, string parametro2 = "Valor por defecto")
        //{
        //    return Ok(new { parametro1, parametro2 });
        //}


        [HttpPost]
        public async Task<ActionResult> Post(AutorCreacionDTO autorCreacionDTO)
        {
            var autor = _mapper.Map<Autor>(autorCreacionDTO);

            // Marca el objeto como agregado en el contexto de EF Core para que se inserte en la base de datos al guardar los cambios.
            _context.Add(autor);
            //_context.Autores.Add(autor); // Es lo mismo que _context.Add(autor);
            await _context.SaveChangesAsync();

            var autorDto = _mapper.Map<AutorDTO>(autor);

            //En la respuesta, dentro de "Headers" -> Location, se genera una url como esta: https://localhost:7298/api/autores/4 
            return CreatedAtRoute("ObtenerAutorV2", new { id = autor.Id }, autorDto);
        }


        [HttpPost("con-foto")]
        public async Task<ActionResult> PostConFoto([FromForm]AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var autor = _mapper.Map<Autor>(autorCreacionDTO);

            if (autorCreacionDTO.Foto is not null)
            {
                var url = await _almacenadorArchivos.Almacenar(contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            _context.Add(autor);
            await _context.SaveChangesAsync();

            //Limpiar cache
            await _outputCacheStore.EvictByTagAsync(cache, default);

            var autorDto = _mapper.Map<AutorDTO>(autor);

            return CreatedAtRoute("ObtenerAutorV2", new { id = autor.Id }, autorDto);
        }


        [HttpPut("{id:int}")] //api/autores/id
        public async Task<ActionResult> Put(int id, [FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var existeAutor = await _context.Autores.AnyAsync(x => x.Id == id);
            if (!existeAutor)
            {
                return NotFound();
            }

            var autor = _mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;

            if (autorCreacionDTO.Foto is not null)
            {
                var fotoActual = await _context.Autores
                                            .Where(x => x.Id == id)
                                            .Select(x => x.Foto).FirstAsync();

                var url = await _almacenadorArchivos.Editar(fotoActual, contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            _context.Update(autor);
            await _context.SaveChangesAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }


        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AutorPatchDTO> pathDoc)
        {
            if (pathDoc is null)
            {
                return BadRequest();
            }

            var autorDB = await _context.Autores.FirstOrDefaultAsync(x => x.Id == id);
            if (autorDB is null)
            {
                return NotFound();
            }

            var autorPatchDTO = _mapper.Map<AutorPatchDTO>(autorDB);

            pathDoc.ApplyTo(autorPatchDTO, ModelState); //Para aplicar los cambios que vienen del cliente
            var esValido = TryValidateModel(autorPatchDTO); //Para validar el modelo después de aplicar los cambios del patch

            if (!esValido)
            {
                return ValidationProblem();
            }

            _mapper.Map(autorPatchDTO, autorDB); //Para actualizar el autorDB con los cambios del autorPatchDTO
            await _context.SaveChangesAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }


        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var autor = await _context.Autores.FirstOrDefaultAsync(x => x.Id == id);
            if (autor is null)
            {
                return NotFound();
            }

            _context.Autores.Remove(autor);
            await _context.SaveChangesAsync();

            await _almacenadorArchivos.Borrar(autor.Foto, contenedor);

            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent(); // NoContent es el código de estado HTTP 204, que indica que la solicitud se ha procesado correctamente pero no hay contenido para devolver.
        }
    }
}
