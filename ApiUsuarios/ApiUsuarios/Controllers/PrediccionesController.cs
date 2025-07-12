using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Models;
using Data;
using System.Text.Json;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrediccionesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PredictionEnginePool<LecturaInput, LecturaPrediction> _predEnginePool;

        private const double COSTO_POR_M3 = 1.24;
        private const int CONSUMO_MIN_M3 = 18000;
        private const int CONSUMO_MAX_M3 = 20000;

        public PrediccionesController(AppDbContext context, PredictionEnginePool<LecturaInput, LecturaPrediction> predEnginePool)
        {
            _context = context;
            _predEnginePool = predEnginePool;
        }

        [HttpGet("regar-avanzado/{id}")]
        public async Task<IActionResult> PredecirDesdeBD(int id)
        {
            var lectura = await _context.Lecturas.Include(l => l.Cultivo).FirstOrDefaultAsync(l => l.Id == id);
            if (lectura == null) return NotFound(new { mensaje = "Lectura no encontrada" });

            var input = new LecturaInput
            {
                HumedadSuelo = (float)lectura.HumedadSuelo,
                Temperatura = (float)lectura.Temperatura,
                Precipitacion = (float)lectura.Precipitacion,
                Viento = (float)lectura.Viento,
                RadiacionSolar = (float)lectura.RadiacionSolar,
                EtapaCultivo = lectura.EtapaCultivo,
                Cultivo = await ObtenerNombreCultivo(lectura.CultivoId),
                Mes = (float)lectura.Fecha.Month,
                DiaDelAnio = (float)lectura.Fecha.DayOfYear
            };

            return Ok(ConstruirRespuesta(input, lectura.Fecha));
        }

        [HttpPost("regar-avanzado")]
        public IActionResult PredecirDesdeInput([FromBody] LecturaInput input)
        {
            if (input == null) return BadRequest(new { mensaje = "Entrada inválida" });
            return Ok(ConstruirRespuesta(input, DateTime.Now));
        }

        [HttpGet("regar-todos")]
        public async Task<IActionResult> PredecirParaTodo2024()
        {
            var fechaInicio = new DateTime(2024, 1, 1);
            var fechaFin = new DateTime(2024, 12, 31);

            var lecturas = await _context.Lecturas.Include(l => l.Cultivo)
                .Where(l => l.Fecha >= fechaInicio && l.Fecha <= fechaFin)
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
                    Cultivo = await ObtenerNombreCultivo(lectura.CultivoId),
                    Mes = (float)lectura.Fecha.Month,
                    DiaDelAnio = (float)lectura.Fecha.DayOfYear
                };

                var prediction = _predEnginePool.Predict(input);
                var threshold = ObtenerUmbralDesdeJson();
                var necesitaRiego = prediction.Score >= threshold;

                resultados.Add(new
                {
                    necesitaRiego,
                    probabilidad = Math.Round(prediction.Score * 100, 2),
                    costo_estimado = necesitaRiego ? 98990 : 0,
                    temporada = DeterminarTemporada(await ObtenerNombreCultivo(lectura.CultivoId), lectura.Fecha),
                    cultivo = await ObtenerNombreCultivo(lectura.CultivoId),
                    etapa = lectura.EtapaCultivo,
                    fecha = lectura.Fecha.ToString("yyyy-MM-dd"),
                    explicacion = ObtenerExplicacionBasadaEn(input)
                });
            }

            return Ok(resultados);
        }

        // 🔧 Lógica de predicción con lectura de umbral desde JSON
        private object ConstruirRespuesta(LecturaInput input, DateTime fecha)
        {
            var prediccion = _predEnginePool.Predict(input);
            float threshold = ObtenerUmbralDesdeJson();
            bool necesitaRiegoConUmbral = prediccion.Score >= threshold;

            double consumoPromedio = (CONSUMO_MIN_M3 + CONSUMO_MAX_M3) / 2.0;
            double costoEstimado = necesitaRiegoConUmbral ? consumoPromedio * COSTO_POR_M3 : 0;
            string temporada = DeterminarTemporada(input.Cultivo ?? "Desconocido", fecha);

            return new
            {
                necesitaRiego = necesitaRiegoConUmbral,
                probabilidad = Math.Round(prediccion.Score * 100, 2),
                costo_estimado = Math.Round(costoEstimado, 2),
                temporada,
                cultivo = input.Cultivo,
                etapa = input.EtapaCultivo,
                fecha = fecha.ToString("yyyy-MM-dd"),
                explicacion = ObtenerExplicacionBasadaEn(input)
            };
        }

        // 🔍 Lector del umbral dinámico desde el archivo JSON
        private float ObtenerUmbralDesdeJson()
        {
            var ruta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "metricas_modelo.json");

            if (!System.IO.File.Exists(ruta))
                return 0.5f; // Umbral por defecto

            var json = System.IO.File.ReadAllText(ruta);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("threshold", out var thresholdProp) &&
                thresholdProp.TryGetSingle(out float threshold))
                return threshold;

            return 0.5f;
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
