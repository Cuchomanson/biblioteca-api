using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IServicioUsuarios _serviciosUsuarios;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UsuariosController(
            UserManager<Usuario> userManager, 
            IConfiguration configuration, 
            SignInManager<Usuario> signInManager,
            IServicioUsuarios serviciosUsuarios,
            ApplicationDbContext applicationDbContext,
            IMapper mapper)
        {
            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
            _serviciosUsuarios = serviciosUsuarios;
            _context = applicationDbContext;
            _mapper = mapper;
        }

        #region Registro de usuarios

        [HttpPost("registro")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credenciales)
        {
            var usuario = new Usuario
            {
                UserName = credenciales.Email,
                Email = credenciales.Email
            };

            var res = await _userManager.CreateAsync(usuario, credenciales.Password!);
            if (res.Succeeded)
            {
                var resAutenticacion = await ConstruirToken(credenciales);
                return resAutenticacion;
            }
            else
            {
                foreach (var error in res.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return ValidationProblem();
            }
        }

        #endregion

        #region Login de usuarios

        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credenciales)
        {
            var usuario = await _userManager.FindByEmailAsync(credenciales.Email);
            if (usuario is null)
            {
                return RetornarLoginIncorrecto();
            }

            var res = await _signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password!, false);
            if (res.Succeeded)
            {
                return await ConstruirToken(credenciales);
            }

            return RetornarLoginIncorrecto();
        }

        private ActionResult RetornarLoginIncorrecto()
        {
            ModelState.AddModelError(string.Empty, "Login incorrecto");
            return ValidationProblem();
        }

        #endregion

        #region Renovacion de token

        [HttpGet("renovar-token")]
        [Authorize]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> RenovarToken()
        {
            var usuario = await _serviciosUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return NotFound();
            }

            var credencialesUsuarios = new CredencialesUsuarioDTO() { Email = usuario.Email! };
            var respuestaAuth = await ConstruirToken(credencialesUsuarios);

            return respuestaAuth;
        }

        #endregion

        #region Políticas

        [HttpPost("hacer-admin")]
        [Authorize(Policy = "EsAdmin")]
        public async Task<ActionResult> HacerAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await _userManager.FindByEmailAsync(editarClaimDTO.Email);
            if (usuario is null)
            {
                return NotFound();
            }
            await _userManager.AddClaimAsync(usuario, new Claim("EsAdmin", "true"));
            return NoContent();
        }

        [HttpPost("remover-admin")]
        [Authorize(Policy = "EsAdmin")]
        public async Task<ActionResult> RemoverAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await _userManager.FindByEmailAsync(editarClaimDTO.Email);
            if (usuario is null)
            {
                return NotFound();
            }
            await _userManager.RemoveClaimAsync(usuario, new Claim("EsAdmin", "true"));
            return NoContent();
        }

        #endregion

        #region Obteniendo usuarios sin UserManager

        [HttpGet]
        [Authorize("EsAdmin")]
        public async Task<IEnumerable<UsuarioDTO>> Get()
        {
            var usuarios = await _context.Users.ToListAsync();
            var usuariosDTO = _mapper.Map<List<UsuarioDTO>>(usuarios);

            return usuariosDTO;
        }

        #endregion


        [HttpPut]
        [Authorize]
        public async Task<ActionResult> Put(ActualizarUsuarioDTO actualizarUsuarioDTO)
        {
            var usuario = await _serviciosUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return NotFound();
            }
            usuario.FechaNacimiento = actualizarUsuarioDTO.FechaNacimiento;

            var resultado = await _userManager.UpdateAsync(usuario);
            if (resultado.Succeeded)
            {
                return NoContent();
            }
            else
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
                return ValidationProblem();
            }
        }

        private async Task<RespuestaAutenticacionDTO> ConstruirToken(CredencialesUsuarioDTO credencialesUsuario)
        {
            //Generación de claims
            var claims = new List<Claim>
            {
                new Claim("email", credencialesUsuario.Email),
                new Claim("lo que yo quiera", "cualquier otro valor")
            };

            var usuario = await _userManager.FindByEmailAsync(credencialesUsuario.Email);
            var claimsDb = await _userManager.GetClaimsAsync(usuario!);

            claims.AddRange(claimsDb);


            //Llave secreta
            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["llavejwt"]!));
            var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credenciales
            );

            var tokenJwt = new JwtSecurityTokenHandler().WriteToken(token);

            return new RespuestaAutenticacionDTO() { Token = tokenJwt, Expiracion = token.ValidTo };
        }

    }
}
