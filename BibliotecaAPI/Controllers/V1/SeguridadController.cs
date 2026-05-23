using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controllers.V1
{
    [Route("api/v1/seguridad")]
    [ApiController]
    public class SeguridadController : ControllerBase
    {
        private readonly IDataProtector protector;
        private readonly ITimeLimitedDataProtector protectorLimitadoPorTiempo;
        private readonly IServicioHash _servicioHash;

        public SeguridadController(IDataProtectionProvider protectionProvider, IServicioHash servicioHash)
        {
            // El string de proposito no es la key, es parte de ella
            protector = protectionProvider.CreateProtector("BibliotecaAPI.SeguridadController");
            protectorLimitadoPorTiempo = protector.ToTimeLimitedDataProtector();
            _servicioHash = servicioHash;
        }

        [HttpGet("encriptar")]
        public ActionResult Encriptar(string texto)
        {
            var textoEncriptado = protector.Protect(texto);
            return Ok(new { textoEncriptado });
        }

        [HttpGet("desencriptar")]
        public ActionResult Desencriptar(string textoEncriptado)
        {
            var textoDesencriptado = protector.Unprotect(textoEncriptado);
            return Ok(new { textoDesencriptado });
        }

        #region Limitado por tiempo

        [HttpGet("encriptar-limitado-por-tiempo")]
        public ActionResult EncriptarLimitadoPorTiempo(string texto)
        {
            var textoEncriptado = protectorLimitadoPorTiempo.Protect(texto, TimeSpan.FromSeconds(30));
            return Ok(new { textoEncriptado });
        }

        [HttpGet("desencriptar-limitado-por-tiempo")]
        public ActionResult DesencriptarLimitadoPorTiempo(string textoEncriptado)
        {
            var textoDesencriptado = protectorLimitadoPorTiempo.Unprotect(textoEncriptado);
            return Ok(new { textoDesencriptado });
        }

        #endregion


        [HttpGet("hash")]
        public ActionResult Hash(string textoPlano)
        {
            var hash1 = _servicioHash.Hash(textoPlano);
            var hash2 = _servicioHash.Hash(textoPlano);
            var hash3 = _servicioHash.Hash(textoPlano, hash2.Sal);

            var resultado = new
            {
                textoPlano,
                hash1,
                hash2,
                hash3,
            };

            return Ok(resultado);
        }
    }
}
