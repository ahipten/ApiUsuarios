using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Models;
using Data;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrediccionesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PredictionEnginePool<LecturaInput, LecturaPrediction> _predEnginePool;

        private const double COSTO_POR_M3 = 5.21;
        private const int CONSUMO_MIN_M3 = 18000;
        private const int CONSUMO_MAX_M3 = 20000;

        public PrediccionesController(AppDbContext context, PredictionEnginePool<LecturaInput, LecturaPrediction> predEnginePool)
        {
            _context = context;
            _predEnginePool = predEnginePool;
        }

        // GET: api/predicciones/regar-avanzado/{id}
        [HttpGet("regar-avanzado/{id}")]
        public async Task<IActionResult> PredecirDesdeBD(int id)
        {
            try
            {
                var lectura = await _context.Lecturas
                    .Include(l => l.Cultivo)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (lectura == null)
                    return NotFound(new { mensaje = "Lectura no encontrada" });

                var input = new LecturaInput
                {
                    HumedadSuelo = (float)lectura.HumedadSuelo,
                    Temperatura = (float)lectura.Temperatura,
                    Precipitacion = (float)lectura.Precipitacion,
                    Viento = (float)lectura.Viento,
                    RadiacionSolar = (float)lectura.RadiacionSolar,
                    EtapaCultivo = lectura.EtapaCultivo,
                    Cultivo = await ObtenerNombreCultivo(lectura.CultivoId)
                };

                return Ok(ConstruirRespuesta(input, lectura.Fecha));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al predecir por ID: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/predicciones/regar-avanzado
        [HttpPost("regar-avanzado")]
        public IActionResult PredecirDesdeInput([FromBody] LecturaInput input)
        {
            try
            {
                if (input == null)
                    return BadRequest(new { mensaje = "Entrada inválida" });

                return Ok(ConstruirRespuesta(input, DateTime.Now));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en predicción directa: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // GET: api/predicciones/regar-todos
        [HttpGet("regar-todos")]
        public async Task<IActionResult> PredecirTodos()
        {
            try
            {
                var lecturas = await _context.Lecturas
                    .Include(l => l.Cultivo)
                    .GroupBy(l => l.CultivoId)
                    .Select(g => g.OrderByDescending(l => l.Fecha).First())
                    .ToListAsync();

                var resultados = new List<object>();

                foreach (var lectura in lecturas)
                {
                    var input = new LecturaInput
                    {
                        HumedadSuelo = (float)lectura.HumedadSuelo,
                        Temperatura = (float)lectura.Temperatura,
                        Precipitacion = (float)lectura.Precipitacion,
                        Viento = (float)lectura.Viento,
                        RadiacionSolar = (float)lectura.RadiacionSolar,
                        EtapaCultivo = lectura.EtapaCultivo,
                        Cultivo = await ObtenerNombreCultivo(lectura.CultivoId)
                    };

                    var respuesta = ConstruirRespuesta(input, lectura.Fecha);
                    resultados.Add(respuesta);
                }

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al predecir todos: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // Método para usar el modelo ML.NET y construir la respuesta enriquecida
        private object ConstruirRespuesta(LecturaInput input, DateTime fecha)
        {
            var prediccion = _predEnginePool.Predict(input);

            double consumoPromedio = (CONSUMO_MIN_M3 + CONSUMO_MAX_M3) / 2.0;
            double costoEstimado = prediccion.NecesitaRiego ? consumoPromedio * COSTO_POR_M3 : 0;
            string temporada = DeterminarTemporada(input.Cultivo ?? "Desconocido", fecha);
            double probabilidad = 1.0 / (1.0 + Math.Exp(-prediccion.Score));

            string explicacion = ObtenerExplicacionBasadaEn(input);

            return new
            {
                necesitaRiego = prediccion.NecesitaRiego,
                probabilidad = Math.Round(probabilidad, 2),
                costo_estimado = Math.Round(costoEstimado, 2),
                temporada,
                cultivo = input.Cultivo,
                etapa = input.EtapaCultivo,
                fecha = fecha.ToString("yyyy-MM-dd"),
                explicacion
            };
        }

        private async Task<string> ObtenerNombreCultivo(int cultivoId)
        {
            var cultivo = await _context.Cultivos.FindAsync(cultivoId);
            return cultivo?.Nombre ?? "Desconocido";
        }

        private string DeterminarTemporada(string cultivo, DateTime fecha)
        {
            int mes = fecha.Month;
            return cultivo switch
            {
                "Maiz" => (mes >= 11 || mes <= 2) ? "Alta" : "Baja",
                "Palta" => (mes >= 4 && mes <= 8) ? "Alta" : "Baja",
                "Espárrago" => (mes >= 8 && mes <= 12) ? "Alta" : "Baja",
                "Mango" => (mes >= 10 || mes <= 1) ? "Alta" : "Baja",
                _ => "Baja"
            };
        }

        private string ObtenerExplicacionBasadaEn(LecturaInput input)
        {
            if (input.HumedadSuelo < 20)
                return "Humedad baja";
            if (input.Temperatura > 30)
                return "Alta temperatura";
            if (input.EtapaCultivo?.ToLower() == "floración")
                return "Etapa crítica del cultivo";
            return "Condiciones normales";
        }
    }
}
