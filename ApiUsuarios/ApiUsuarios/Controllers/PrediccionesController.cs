using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Models;
using Data;
using System.Text.Json;
using System.Globalization;
using System.Text;

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

        // =========================================================
        // 🔹 REGAR AVANZADO (POR ID)
        // =========================================================
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

        // =========================================================
        // 🔹 REGAR AVANZADO (POST)
        // =========================================================
        [HttpPost("regar-avanzado")]
        public IActionResult PredecirDesdeInput([FromBody] LecturaInput input)
        {
            if (input == null) return BadRequest(new { mensaje = "Entrada inválida" });
            return Ok(ConstruirRespuesta(input, DateTime.Now));
        }

        // =========================================================
        // 🔹 REGAR TODOS
        // =========================================================
        [HttpGet("regar-todos")]
        public async Task<IActionResult> PredecirParaTodo2024()
        {
            var fechaInicio = new DateTime(2025, 1, 1);
            var fechaFin = new DateTime(2025, 12, 31);

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
                    DiaDelAnio = (float)lectura.Fecha.DayOfYear,
                    Indice_Sequía = (float)lectura.IndiceSequia,
                    Materia_Organica = (float)lectura.MateriaOrganica,
                    Metodo_Riego = lectura.MetodoRiego,
                    pH_Suelo = (float)lectura.pH_Suelo,
                    Interaccion_HT = (float)(lectura.HumedadSuelo * lectura.Temperatura),
                    Balance_Agua = (float)(lectura.Precipitacion - (0.0023 * (lectura.Temperatura + 17.8) * Math.Sqrt(lectura.RadiacionSolar))),
                    Sequía_MO = (float)(lectura.IndiceSequia * lectura.MateriaOrganica),
                    Latitud = (float)lectura.Lat,
                    Longitud = (float)lectura.Lng
                };

                var prediction = _predEnginePool.Predict(input);
                var threshold = ObtenerUmbralDesdeJson();
                var necesitaRiego = prediction.Score >= threshold;

                double consumoPromedio = (CONSUMO_MIN_M3 + CONSUMO_MAX_M3) / 2.0;
                double litrosEstimados = necesitaRiego ? consumoPromedio : 0;
                double costoEstimado = litrosEstimados * COSTO_POR_M3;

                resultados.Add(new
                {
                    necesitaRiego,
                    probabilidad = Math.Round(prediction.Score * 100, 2),
                    litros_estimados = Math.Round(litrosEstimados, 2),
                    costo_estimado = Math.Round(costoEstimado, 2),
                    temporada = DeterminarTemporada(await ObtenerNombreCultivo(lectura.CultivoId), lectura.Fecha),
                    cultivo = await ObtenerNombreCultivo(lectura.CultivoId),
                    etapa = lectura.EtapaCultivo,
                    fecha = lectura.Fecha.ToString("yyyy-MM-dd"),
                    explicacion = ObtenerExplicacionBasadaEn(input)
                });
            }

            return Ok(resultados);
        }

        // =========================================================
        // 🔹 IMPORTANCIA DE CARACTERÍSTICAS
        // =========================================================
        [HttpGet("importancia-caracteristicas")]
        public IActionResult GetImportanciaCaracteristicas()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "importancia_caracteristicas.json");

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "Archivo de importancia de características no encontrado." });

                var json = System.IO.File.ReadAllText(filePath);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al leer el archivo de importancia de características.", error = ex.Message });
            }
        }

        // =========================================================
        // 🔹 RECOMENDACIÓN POR CULTIVO
        // =========================================================
        [HttpGet("recomendacion")]
        public async Task<IActionResult> ObtenerRecomendacionPorCultivo([FromQuery] string cultivo)
        {
            if (string.IsNullOrWhiteSpace(cultivo))
                return BadRequest(new { mensaje = "Debe especificar un cultivo." });

            // ✅ Normalizar texto: quitar tildes y pasar a minúsculas
            string cultivoNormalizado = new string(cultivo
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray())
                .ToLower();

            // 📘 Buscar cultivo en BD (ignorando tildes y mayúsculas)
            var cultivos = await _context.Cultivos.ToListAsync();

            var cultivoBD = cultivos.FirstOrDefault(c =>
            {
                string nombreNormalizado = new string(c.Nombre
                    .Normalize(NormalizationForm.FormD)
                    .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    .ToArray())
                    .ToLower();

                return nombreNormalizado == cultivoNormalizado;
            });

            if (cultivoBD == null)
                return NotFound(new { mensaje = $"No se encontró el cultivo '{cultivo}'." });

            // 🔹 Parámetros base de riego (todas las claves en minúsculas y sin tildes)
            var parametros = new Dictionary<string, (double LaminaNeta, double Eficiencia, string Metodo)>
            {
                ["maiz"] = (45.0, 0.90, "Goteo"),
                ["palta"] = (35.0, 0.85, "Aspersión"),
                ["platano"] = (70.0, 0.75, "Inundación"),
                ["esparrago"] = (40.0, 0.88, "Goteo"),
                ["mango"] = (50.0, 0.87, "Goteo")
            };

            if (!parametros.ContainsKey(cultivoNormalizado))
                return NotFound(new { mensaje = $"No hay parámetros de riego configurados para el cultivo '{cultivo}'." });

            var data = parametros[cultivoNormalizado];


            // 📊 Calcular volumen recomendado (m³ por hectárea)
            double areaHa = 1.0;
            double volumen = data.LaminaNeta * areaHa * 10 * data.Eficiencia;
            double ahorro = 0;

            // 💧 Buscar la lectura más reciente
            var ultimaLectura = await _context.Lecturas
                .Where(l => l.CultivoId == cultivoBD.Id)
                .OrderByDescending(l => l.Fecha)
                .FirstOrDefaultAsync();

            string mensaje;
            if (ultimaLectura != null)
            {
                var input = new LecturaInput
                {
                    HumedadSuelo = (float)ultimaLectura.HumedadSuelo,
                    Temperatura = (float)ultimaLectura.Temperatura,
                    Precipitacion = (float)ultimaLectura.Precipitacion,
                    Viento = (float)ultimaLectura.Viento,
                    RadiacionSolar = (float)ultimaLectura.RadiacionSolar,
                    EtapaCultivo = ultimaLectura.EtapaCultivo,
                    Cultivo = cultivoBD.Nombre,
                    Mes = (float)ultimaLectura.Fecha.Month,
                    DiaDelAnio = (float)ultimaLectura.Fecha.DayOfYear
                };

                var prediccion = _predEnginePool.Predict(input);
                float threshold = ObtenerUmbralDesdeJson();
                bool necesitaRiego = prediccion.Score >= threshold;

                mensaje = necesitaRiego
                    ? $"El cultivo de {cultivoBD.Nombre} requiere riego hoy. Se recomienda aplicar {Math.Round(volumen, 0)} m³/ha mediante riego por {data.Metodo.ToLower()}."
                    : $"El cultivo de {cultivoBD.Nombre} no requiere riego en este momento. La humedad del suelo es adecuada.";

                if (!necesitaRiego)
                    ahorro = volumen;
            }
            else
            {
                mensaje = $"No hay lecturas recientes para el cultivo de {cultivoBD.Nombre}. Se sugiere una lámina promedio de {data.LaminaNeta} mm.";
            }

            var respuesta = new
            {
                cultivo = cultivoBD.Nombre,
                metodo_riego = data.Metodo,
                lamina_neta_mm = data.LaminaNeta,
                eficiencia = data.Eficiencia,
                volumen_recomendado_m3 = Math.Round(volumen, 2),
                ahorro_estimado_m3 = Math.Round(ahorro, 2),
                mensaje
            };

            return Ok(respuesta);
        }

        // =========================================================
        // 🔹 MÉTODOS PRIVADOS
        // =========================================================
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

        private float ObtenerUmbralDesdeJson()
        {
            var ruta = Path.Combine(Directory.GetCurrentDirectory(), "Data", "metricas_modelo.json");

            if (!System.IO.File.Exists(ruta))
                return 0.5f;

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
