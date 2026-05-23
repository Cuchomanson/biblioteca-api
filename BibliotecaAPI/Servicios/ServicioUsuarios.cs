using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Servicios
{
    public class ServicioUsuarios : IServicioUsuarios
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;

        public ServicioUsuarios(UserManager<Usuario> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _contextAccessor = httpContextAccessor;
        }


        public async Task<Usuario?> ObtenerUsuario()
        {
            var emailClaim = _contextAccessor.HttpContext!.User.Claims.FirstOrDefault(x => x.Type == "email");
            if (emailClaim is null)
            {
                return null;
            }

            var email = emailClaim.Value;
            return await _userManager.FindByEmailAsync(email);
        }

    }
}
