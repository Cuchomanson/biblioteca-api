using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController]
    [Route("api/v2/libros")]
    [Authorize(Policy = "EsAdmin")]
    public class LibrosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITimeLimitedDataProtector protectorLimitadoPorTiempo;
        private readonly IOutputCacheStore _outputCacheStore;
        private const string cache = "libros-obtener";

        public LibrosController(
            ApplicationDbContext context, 
            IMapper mapper, 
            IDataProtectionProvider protectionProvider,
            IOutputCacheStore outputCacheStore)
        {
            _context = context;
            _mapper = mapper;
            _outputCacheStore = outputCacheStore;
            protectorLimitadoPorTiempo = protectionProvider.CreateProtector("BibliotecaAPI.LibrosController").ToTimeLimitedDataProtector();
        }

        #region Link con token limitado por tiempo

        [HttpGet("v2/listado/obtener-token")]
        [AllowAnonymous]
        public ActionResult ObtenerTokenListado()
        {
            var token = protectorLimitadoPorTiempo.Protect(Guid.NewGuid().ToString(), TimeSpan.FromSeconds(30));
            var url = Url.RouteUrl("ObtenerListadoLibrosUsandoTokenV2", new { token }, Request.Scheme);

            return Ok(new { url });
        }


        [HttpGet("v2/listado/{token}", Name = "ObtenerListadoLibrosUsandoTokenV2")]
        [AllowAnonymous]
        public async Task<ActionResult> ObtenerListadoLibrosUsandoToken(string token)
        {
            try
            {
                protectorLimitadoPorTiempo.Unprotect(token);

            }
            catch
            {
                ModelState.AddModelError(nameof(token), "El token no es válido o ha expirado");
                return ValidationProblem();
            }

            var libros = await _context.Libros.ToListAsync();
            var librosDto = _mapper.Map<IEnumerable<LibroDTO>>(libros);

            return Ok(librosDto);
        }

        #endregion


        [HttpGet(Name = "ObtenerLibroV2")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<IEnumerable<LibroDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            var queryable = _context.Libros.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);

            var libros = await queryable
                            .OrderBy(x => x.Titulo)
                            .Paginar(paginacionDTO)
                            .ToListAsync();

            var librosDto = _mapper.Map<IEnumerable<LibroDTO>>(libros);

            return librosDto;
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<LibroConAutoresDTO>> Get(int id)
        {
            var libro = await _context.Libros
                .Include(x => x.Autores!)
                .ThenInclude(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);

            return libro != null
                ? Ok(_mapper.Map<LibroConAutoresDTO>(libro))
                : NotFound();
        }

        [HttpPost]
        [ServiceFilter<FiltroValidacionLibro>()]
        public async Task<ActionResult> Post(LibroCreacionDTO libroCreacionDto)
        {
            var libro = _mapper.Map<Libro>(libroCreacionDto);
            AsignarOrdenAutores(libro);

            _context.Add(libro);
            await _context.SaveChangesAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            var libroDTO = _mapper.Map<LibroDTO>(libro);

            return CreatedAtRoute("ObtenerLibroV2", new { id = libro.Id }, libroDTO);
        }

        private void AsignarOrdenAutores(Libro libro)
        {
            if (libro.Autores is not null)
            {
                for (int i = 0; i < libro.Autores.Count; i++)
                {
                    libro.Autores[i].Orden = i + 1;
                }
            }
        }

        [HttpPut("{id:int}")]
        [ServiceFilter<FiltroValidacionLibro>()]
        public async Task<ActionResult> Put(int id, LibroCreacionDTO libroCreacionDto)
        {
            var libroDb = await _context.Libros
                                    .Include(x => x.Autores)
                                    .FirstOrDefaultAsync(x => x.Id == id);

            if (libroDb is null)
            {
                return NotFound();
            }

            // ** No es necesario eliminar los registros que no venga informados como yo hacía antes. Al dejar esta lista, se asignan y eliminan solos
            libroDb = _mapper.Map(libroCreacionDto, libroDb);
            AsignarOrdenAutores(libroDb);

            await _context.SaveChangesAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var registrosBorrados = await _context.Libros.Where(x => x.Id == id).ExecuteDeleteAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            return registrosBorrados > 0
                ? NoContent()
                : NotFound();
        }
    }
}
