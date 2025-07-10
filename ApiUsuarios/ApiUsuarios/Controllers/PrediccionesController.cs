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
        // GET: api/predicciones/regar-avanzado/{id}
        // Usa una lectura guardada en BD
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
                    HumedadSuelo    = lectura.HumedadSuelo ?? 0f,
                    Temperatura     = lectura.Temperatura ?? 0f,
                    Precipitacion   = lectura.Precipitacion ?? 0f,
                    Viento          = lectura.Viento ?? 0f,
                    RadiacionSolar  = lectura.RadiacionSolar ?? 0f,
                    EtapaCultivo    = lectura.EtapaCultivo,
                    Cultivo         = await ObtenerNombreCultivo(lectura.CultivoId)
                };

                return Ok(ConstruirRespuesta(input));
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

                return Ok(ConstruirRespuesta(input));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en predicción directa: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
        // En PrediccionesController.cs

        [HttpGet("regar-todos")]
        public async Task<IActionResult> PredecirTodos()
        {
            try
            {
                var lecturas = await _context.Lecturas
                    .Include(l => l.Cultivo)
                    .OrderByDescending(l => l.Fecha)
                    .Take(20) // Puedes ajustar este número o quitarlo si deseas todas
                    .ToListAsync();

                var resultados = new List<object>();

                foreach (var lectura in lecturas)
                {
                    var input = new LecturaInput
                    {
                        HumedadSuelo = lectura.HumedadSuelo.HasValue ? Convert.ToSingle(lectura.HumedadSuelo.Value) : 0f,
                        Temperatura    = lectura.Temperatura.HasValue ? Convert.ToSingle(lectura.Temperatura.Value) : 0f,
                        Precipitacion  = lectura.Precipitacion.HasValue ? Convert.ToSingle(lectura.Precipitacion.Value) : 0f,
                        Viento         = lectura.Viento.HasValue ? Convert.ToSingle(lectura.Viento.Value) : 0f,
                        RadiacionSolar = lectura.RadiacionSolar.HasValue ? Convert.ToSingle(lectura.RadiacionSolar.Value) : 0f,
                        EtapaCultivo   = lectura.EtapaCultivo,
                        Cultivo        = await ObtenerNombreCultivo(lectura.CultivoId)
                    };

                    var prediccion = _predEnginePool.Predict(input);
                    double consumoPromedio = (CONSUMO_MIN_M3 + CONSUMO_MAX_M3) / 2.0;
                    double costoEstimado = prediccion.NecesitaRiego ? consumoPromedio * COSTO_POR_M3 : 0;
                    string temporada = DeterminarTemporada(input.Cultivo, lectura.Fecha);

                    resultados.Add(new
                    {
                        necesitaRiego = prediccion.NecesitaRiego,
                        probabilidad = Math.Round(prediccion.Score, 2),
                        costo_estimado = Math.Round(costoEstimado, 2),
                        temporada,
                        cultivo = input.Cultivo,
                        etapa = input.EtapaCultivo,
                        fecha = lectura.Fecha.ToString("yyyy-MM-dd")
                    });
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
        // Método para usar el modelo ML.NET
        // ─────────────────────────────
        private object ConstruirRespuesta(LecturaInput input)
        {
            var prediccion = _predEnginePool.Predict(input);

            double consumoPromedio = (CONSUMO_MIN_M3 + CONSUMO_MAX_M3) / 2.0;
            double costoEstimado = prediccion.NecesitaRiego ? consumoPromedio * COSTO_POR_M3 : 0;
            string temporada = DeterminarTemporada(input.Cultivo ?? "Desconocido", DateTime.Now);
            double probabilidad = 1.0 / (1.0 + Math.Exp(-prediccion.Score));
            return new
            {
                necesitaRiego = prediccion.NecesitaRiego,
                probabilidad = Math.Round(probabilidad, 2),  // Ya es un float directo
                costo_estimado = Math.Round(costoEstimado, 2),
                temporada,
                cultivo = input.Cultivo,
                etapa = input.EtapaCultivo,
                fecha = DateTime.Now.ToString("yyyy-MM-dd")
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
