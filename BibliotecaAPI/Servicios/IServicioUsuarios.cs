using BibliotecaAPI.Entidades;

namespace BibliotecaAPI.Servicios
{
    public interface IServicioUsuarios
    {
        Task<Usuario?> ObtenerUsuario();
    }
}