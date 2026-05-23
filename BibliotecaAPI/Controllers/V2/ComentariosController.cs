using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController]
    [Route("api/v2/libros/{libroId:int}/comentarios")] // Esto fuerza a que para cualquier acceso a un comentario -> Hay que pasar la info del libro
    [Authorize]
    public class ComentariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IOutputCacheStore _outputCacheStore;
        private const string cache = "comentarios";

        public ComentariosController(
            ApplicationDbContext dbContext, 
            IMapper mapper, 
            IServicioUsuarios servicioUsuarios,
            IOutputCacheStore outputCacheStore)
        {
            _context = dbContext;
            _mapper = mapper;
            _servicioUsuarios = servicioUsuarios;
            _outputCacheStore = outputCacheStore;
        }


        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<List<ComentarioDTO>>> Get(int libroId)
        {
            var existeLibro = await _context.Libros.AnyAsync(x => x.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }

            var comentarios = await _context.Comentarios
                                        .Include(x => x.Usuario)
                                        .Where(x => x.LibroId == libroId)
                                        .OrderByDescending(x => x.FechaPublicacion)
                                        .ToListAsync();

            return _mapper.Map<List<ComentarioDTO>>(comentarios);
        }


        [HttpGet("{id}", Name = "ObtenerComentarioV2")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<ComentarioDTO>> Get(Guid id)
        {
            var comentario = await _context.Comentarios
                                    .Include(x => x.Usuario)
                                    .FirstOrDefaultAsync(x => x.Id == id);

            return comentario is null
                ? NotFound()
                : _mapper.Map<ComentarioDTO>(comentario);
        }


        [HttpPost]
        public async Task<ActionResult> Post(int libroId, ComentarioCreacionDTO comentarioCreacionDTO)
        {
            var existeLibro = await _context.Libros.AnyAsync(x => x.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await _servicioUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return Unauthorized();
            }

            var comentario = _mapper.Map<Comentario>(comentarioCreacionDTO);
            comentario.LibroId = libroId;
            comentario.FechaPublicacion = DateTime.UtcNow;
            comentario.UsuarioId = usuario.Id;

            _context.Add(comentario);
            await _context.SaveChangesAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            var comentarioDto = _mapper.Map<ComentarioDTO>(comentario);
            return CreatedAtRoute("ObtenerComentarioV2", new { id = comentario.Id, libroId }, comentarioDto);
        }


        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(Guid id, int libroId, JsonPatchDocument<ComentarioPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var existeLibro = await _context.Libros.AnyAsync(x => x.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await _servicioUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return Unauthorized();
            }

            var comentarioDb = await _context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);
            if (comentarioDb is null)
            {
                return NotFound();
            }

            // Solo el autor del comentario puede editarlo
            if (comentarioDb.UsuarioId != usuario.Id)
            {
                return Forbid();
            }

            var comentarioPatchDTO = _mapper.Map<ComentarioPatchDTO>(comentarioDb);

            patchDoc.ApplyTo(comentarioPatchDTO, ModelState); //Para aplicar los cambios que vienen del cliente
            var esValido = TryValidateModel(comentarioPatchDTO); //Para validar el modelo después de aplicar los cambios del patch

            if (!esValido)
            {
                return ValidationProblem();
            }

            _mapper.Map(comentarioPatchDTO, comentarioDb); //Para actualizar el autorDB con los cambios del autorPatchDTO
            await _context.SaveChangesAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id, int libroId)
        {
            var existeLibro = await _context.Libros.AnyAsync(x => x.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await _servicioUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return Unauthorized();
            }

            var comentario = await _context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);
            if (comentario is null)
            {
                return NotFound();
            }

            if (comentario.UsuarioId != usuario.Id)
            {
                return Forbid();
            }

            comentario.EstaBorrado = true;
            await _context.SaveChangesAsync();

            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }
    }
}