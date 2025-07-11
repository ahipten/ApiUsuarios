using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Linq;
using Models;

[ApiController]
[Route("api/[controller]")]
public class MetricasController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public MetricasController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public IActionResult ObtenerMetricas()
    {
        var mlContext = new MLContext();

        var modelPath = Path.Combine(_env.ContentRootPath, "MLModels", "ModeloRiego.zip");
        if (!System.IO.File.Exists(modelPath))
            return NotFound("Modelo no encontrado.");

        ITransformer modelo = mlContext.Model.Load(modelPath, out var modeloInputSchema);

        // Cargar datos de prueba para evaluar el modelo
        var dataPath = Path.Combine(_env.ContentRootPath, "MLModels", "datos_test.csv");
        if (!System.IO.File.Exists(dataPath))
            return NotFound("Archivo de prueba no encontrado.");

        var dataView = mlContext.Data.LoadFromTextFile<LecturaInput>(
            path: dataPath,
            hasHeader: true,
            separatorChar: ',');

        var predictions = modelo.Transform(dataView);

        var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        var resultado = new MetricasResultado
        {
            Accuracy = (float)metrics.Accuracy,
            Precision = (float)metrics.PositivePrecision,
            Recall = (float)metrics.PositiveRecall,
            Specificity = (float)metrics.NegativeRecall,
            F1 = (float)metrics.F1Score,
            Auc = (float)metrics.AreaUnderRocCurve
        };

        return Ok(resultado);
    }
}
