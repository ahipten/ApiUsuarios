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
        [HttpGet("evaluar")]
        public IActionResult GetMetricasEntrenamiento()
        {
            // Ruta al archivo JSON
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "metricas_modelo.json");

            if (!System.IO.File.Exists(path))
                return NotFound("No se encontró el archivo de métricas del modelo.");

            try
            {
                var json = System.IO.File.ReadAllText(path);

                // 👇 Se agrega case-insensitive para que haga match aunque el JSON esté en minúsculas
                var metricas = JsonSerializer.Deserialize<MetricasResultado>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(metricas);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al leer metricas_modelo.json: {ex.Message}");
            }
        }
    }
}

