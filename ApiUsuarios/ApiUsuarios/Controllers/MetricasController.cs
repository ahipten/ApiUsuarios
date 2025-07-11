using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using Microsoft.EntityFrameworkCore;
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
            // 🔍 Obtener las lecturas reales desde la base de datos
            var lecturas = await _context.Lecturas
                .Include(l => l.Cultivo)
                .ToListAsync();

            if (lecturas.Count == 0)
                return NotFound("No hay datos de prueba disponibles.");

            int tp = 0, tn = 0, fp = 0, fn = 0;

            foreach (var l in lecturas)
            {
                var input = new LecturaInput
                {
                    HumedadSuelo = (float)l.HumedadSuelo,
                    Temperatura = (float)l.Temperatura,
                    Precipitacion = (float)l.Precipitacion,
                    Viento = (float)l.Viento,
                    RadiacionSolar = (float)l.RadiacionSolar,
                    EtapaCultivo = l.EtapaCultivo,
                    Cultivo = l.Cultivo?.Nombre ?? ""
                };

                var prediction = _predictionEngine.Predict(input);
                bool predicho = prediction.NecesitaRiego;
                bool real = l.NecesitaRiego ?? false; // evitar error de nullable

                if (predicho && real) tp++;
                else if (predicho && !real) fp++;
                else if (!predicho && real) fn++;
                else tn++;
            }

            int total = tp + tn + fp + fn;
            float accuracy = total > 0 ? (float)(tp + tn) / total : 0;
            float precision = (tp + fp) > 0 ? (float)tp / (tp + fp) : 0;
            float recall = (tp + fn) > 0 ? (float)tp / (tp + fn) : 0;
            float specificity = (tn + fp) > 0 ? (float)tn / (tn + fp) : 0;
            float f1 = (precision + recall) > 0 ? 2 * (precision * recall) / (precision + recall) : 0;

            var resultado = new MetricasResultado
            {
                Accuracy = accuracy,
                Precision = precision,
                Recall = recall,
                Specificity = specificity,
                F1 = f1,
                Auc = (recall + specificity) / 2
            };

            return Ok(resultado);
        }
    }
}
