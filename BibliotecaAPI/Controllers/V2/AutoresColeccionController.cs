using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController]
    [Route("api/v2/autores-coleccion")]
    [Authorize(Policy = "EsAdmin")]
    public class AutoresColeccionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AutoresColeccionController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("{ids}", Name = "ObtenerAutoresPorIdV2")] // api/autores-coleccion/1,2,3,4
        public async Task<ActionResult<List<AutorConLibrosDTO>>> Get(string ids)
        {
            var idsColeccion = new List<int>();
            foreach (var id in ids.Split(','))
            {
                if (int.TryParse(id, out int idEntero))
                {
                    idsColeccion.Add(idEntero);
                }
                else
                {
                    return BadRequest($"El valor '{id}' no es un número entero válido.");
                }
            }

            if (idsColeccion.Count == 0)
            {
                ModelState.AddModelError(nameof(ids), "Debe proporcionar al menos un ID válido.");
                return ValidationProblem(ModelState);
            }

            var autores = await _context.Autores
                                 .Include(x => x.Libros!)
                                 .ThenInclude(x => x.Libro)
                                 .Where(x => idsColeccion.Contains(x.Id))
                                 .ToListAsync();

            if (autores.Count != idsColeccion.Count)
            {
                return NotFound("No se encontraron todos los autores solicitados.");
            }

            var autoresDTO = _mapper.Map<List<AutorConLibrosDTO>>(autores);
            return autoresDTO;
        }



        [HttpPost]
        public async Task<ActionResult> Post(IEnumerable<AutorCreacionDTO> autoresCreacionDTO)
        {
            var autores = _mapper.Map<IEnumerable<Autor>>(autoresCreacionDTO);
            _context.Autores.AddRange(autores);

            await _context.SaveChangesAsync();

            var autoresDTO = _mapper.Map<IEnumerable<AutorDTO>>(autores);
            var ids = autores.Select(x => x.Id);
            var idsString = string.Join(",", ids);

            return CreatedAtRoute("ObtenerAutoresPorIdV2", new { ids = idsString }, autoresDTO);
        }
    }
}
