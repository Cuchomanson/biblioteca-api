using BibliotecaAPI.Options;
using Microsoft.Extensions.Options;

namespace BibliotecaAPI
{
    public class PagosProcesamiento
    {
        private TarifaOpciones _tarifasOpciones; //Como se quiere actualizar no puede ser readonly

        public PagosProcesamiento(IOptionsMonitor<TarifaOpciones> options)
        {
            _tarifasOpciones = options.CurrentValue;

            options.OnChange((nuevaTarifa) =>
            {
                _tarifasOpciones = nuevaTarifa;
                Console.WriteLine("Tarifa actualizada: " + nuevaTarifa.Dia + " - " + nuevaTarifa.Noche);
            });
        }

        public void ProcesarPago()
        {
            Console.WriteLine("Procesando pago con tarifa de día: " + _tarifasOpciones.Dia);
            Console.WriteLine("Procesando pago con tarifa de noche: " + _tarifasOpciones.Noche);
        }

        public TarifaOpciones ObtenerTarifas()
        {
            return _tarifasOpciones;
        }
    }
}
