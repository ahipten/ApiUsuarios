using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Models;
using Data;

namespace RiegoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly PredictionEnginePool<LecturaInput, LecturaPrediction> _predictionEngine;

        public MetricasController(
            AppDbContext context,
            IWebHostEnvironment env,
            PredictionEnginePool<LecturaInput, LecturaPrediction> predictionEngine)
        {
            _context = context;
            _env = env;
            _predictionEngine = predictionEngine;
        }

        [HttpGet("evaluar")]
        public async Task<IActionResult> EvaluarModelo()
        {
            var lecturas = await _context.Lecturas
                .Include(l => l.Cultivo)
                .ToListAsync();

            if (lecturas.Count == 0)
                return NotFound("No hay datos de prueba disponibles.");

            // 📊 Leer umbral desde metricas_modelo.json
            float threshold = 0.85f; // valor por defecto
            string path = Path.Combine(_env.ContentRootPath, "Data", "metricas_modelo.json");

            if (System.IO.File.Exists(path))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("threshold", out var prop))
                        threshold = prop.GetSingle();
                }
                catch
                {
                    Console.WriteLine("⚠️ Error al leer metricas_modelo.json. Usando threshold por defecto.");
                }
            }

            int tp = 0, tn = 0, fp = 0, fn = 0;

            foreach (var l in lecturas)
            {
                // 🧠 Construimos el input EXACTO que espera el modelo
                var input = new LecturaInput
                {
                    HumedadSuelo = (float)l.HumedadSuelo,
                    Temperatura = (float)l.Temperatura,
                    Precipitacion = (float)l.Precipitacion,
                    Viento = (float)l.Viento,
                    RadiacionSolar = (float)l.RadiacionSolar,

                    // ✅ Nuevas columnas esperadas por el modelo
                    Indice_Sequía = (float)l.IndiceSequia ,
                    pH_Suelo = (float)l.pH_Suelo ,
                    Materia_Organica = (float)l.MateriaOrganica,
                    Metodo_Riego = l.MetodoRiego ?? "Desconocido",

                    EtapaCultivo = l.EtapaCultivo ?? "Desconocido",
                    Cultivo = l.Cultivo?.Nombre ?? "Desconocido",
                    Mes = l.Fecha.Month,
                    DiaDelAnio = l.Fecha.DayOfYear
                };

                // 📈 Predicción
                var prediction = _predictionEngine.Predict(input);
                bool predicho = prediction.Score >= threshold;
                bool real = l.NecesitaRiego ?? false;

                if (predicho && real) tp++;
                else if (predicho && !real) fp++;
                else if (!predicho && real) fn++;
                else tn++;
            }

            // 📊 Cálculo de métricas
            int total = tp + tn + fp + fn;
            float accuracy = total > 0 ? (float)(tp + tn) / total : 0;
            float precision = (tp + fp) > 0 ? (float)tp / (tp + fp) : 0;
            float recall = (tp + fn) > 0 ? (float)tp / (tp + fn) : 0;
            float specificity = (tn + fp) > 0 ? (float)tn / (tn + fp) : 0;
            float f1 = (precision + recall) > 0 ? 2 * (precision * recall) / (precision + recall) : 0;
            float auc = (recall + specificity) / 2;

            var resultado = new MetricasResultado
            {
                Accuracy = accuracy,
                Precision = precision,
                Recall = recall,
                Specificity = specificity,
                F1 = f1,
                Auc = auc
            };

            return Ok(resultado);
        }
    }

    // 📊 Configuración del threshold desde JSON
    public class ThresholdConfig
    {
        public float Valor { get; set; }
    }
}
