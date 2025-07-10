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

        // ─────────────────────────────
        // GET: api/predicciones/regar-avanzado/sensor/{sensorId}
        // Usa la última lectura registrada por sensor
        // ─────────────────────────────
        [HttpGet("regar-avanzado/sensor/{sensorId}")]
        public async Task<IActionResult> PredecirPorSensor(int sensorId)
        {
            try
            {
                var lectura = await _context.Lecturas
                    .Include(l => l.Cultivo)
                    .Where(l => l.SensorId == sensorId)
                    .OrderByDescending(l => l.Fecha)
                    .FirstOrDefaultAsync();

                if (lectura == null)
                {
                    return Ok(new
                    {
                        necesitaRiego = false,
                        probabilidad = 0.0,
                        costo_estimado = 0.0,
                        temporada = "N/A",
                        cultivo = "Desconocido",
                        etapa = "N/A",
                        fecha = DateTime.Now.ToString("yyyy-MM-dd")
                    });
                }

                var input = new LecturaInput
                {
                    HumedadSuelo    = (float)lectura.HumedadSuelo,
                    Temperatura     = (float)lectura.Temperatura,
                    Precipitacion   = (float)lectura.Precipitacion,
                    Viento          = (float)lectura.Viento,
                    RadiacionSolar  = (float)lectura.RadiacionSolar,
                    EtapaCultivo    = lectura.EtapaCultivo,
                    Cultivo         = await ObtenerNombreCultivo(lectura.CultivoId)
                };

                return Ok(ConstruirRespuesta(input, lectura.Fecha));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al predecir por SensorId: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // ─────────────────────────────
        // GET: api/predicciones/regar-avanzado/{id}
        // Usa una lectura específica por su Id (ya existente)
        // ─────────────────────────────
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
                    HumedadSuelo    = (float)lectura.HumedadSuelo,
                    Temperatura     = (float)lectura.Temperatura,
                    Precipitacion   = (float)lectura.Precipitacion,
                    Viento          = (float)lectura.Viento,
                    RadiacionSolar  = (float)lectura.RadiacionSolar,
                    EtapaCultivo    = lectura.EtapaCultivo,
                    Cultivo         = await ObtenerNombreCultivo(lectura.CultivoId)
                };

                return Ok(ConstruirRespuesta(input, lectura.Fecha));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al predecir por ID: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // ─────────────────────────────
        // POST: api/predicciones/regar-avanzado
        // Usa un JSON enviado directamente
        // ─────────────────────────────
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
        // ─────────────────────────────
        // GET: api/predicciones/regar-todos
        // Retorna una lista de predicciones recientes
        // ─────────────────────────────
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

                    resultados.Add(ConstruirRespuesta(input, lectura.Fecha));
                }

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al predecir todos: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
        // ─────────────────────────────
        // GET: api/predicciones/sensores-con-lecturas
        // Retorna todos los SensorId únicos que tienen lecturas
        // ─────────────────────────────
        [HttpGet("sensores-con-lecturas")]
        public async Task<IActionResult> ObtenerSensoresConLecturas()
        {
            try
            {
                var sensores = await _context.Lecturas
                    .Select(l => l.SensorId)
                    .Distinct()
                    .ToListAsync();

                return Ok(sensores);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener sensores con lecturas: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }


        // ─────────────────────────────
        // MÉTODOS AUXILIARES
        // ─────────────────────────────

        private object ConstruirRespuesta(LecturaInput input, DateTime fecha)
        {
            var prediccion = _predEnginePool.Predict(input);

            double consumoPromedio = (CONSUMO_MIN_M3 + CONSUMO_MAX_M3) / 2.0;
            double costoEstimado = consumoPromedio * COSTO_POR_M3; //: 0;//prediccion.NecesitaRiego ?
            double probabilidad = 1.0 / (1.0 + Math.Exp(-prediccion.Score));
            string temporada = DeterminarTemporada(input.Cultivo ?? "Desconocido", fecha);

            return new
            {
                necesitaRiego = prediccion.NecesitaRiego,
                probabilidad = Math.Round(probabilidad, 2),
                costo_estimado = Math.Round(costoEstimado, 2),
                temporada,
                cultivo = input.Cultivo,
                etapa = input.EtapaCultivo,
                fecha = fecha.ToString("yyyy-MM-dd")
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
    }
}
