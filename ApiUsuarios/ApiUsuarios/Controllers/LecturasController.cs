using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper.TypeConversion;
using System.Text;


namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LecturasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Random _rand = new();

        public LecturasController(AppDbContext context) => _context = context;

        // GET: api/lecturas
        // Devuelve todas las lecturas con sus sensores y cultivos
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lectura>>> Get()
        {
            return await _context.Lecturas
                .Include(l => l.Sensor)
                .Include(l => l.Cultivo)
                .ToListAsync();
        }
        // GET: api/lecturas/geo-lecturas
        // Devuelve lecturas con geolocalización y una intensidad basada en la humedad del
        
       [HttpGet("geo-lecturas")]
        public async Task<IActionResult> GetLecturasGeolocalizadas([FromQuery] int? anio, [FromQuery] int? mes, [FromQuery] string? cultivo)
        {
            var query = _context.Lecturas
                .Include(l => l.Cultivo)
                .AsNoTracking()
                .Where(l => l.Lat != 0 && l.Lng != 0);

            if (anio.HasValue)
                query = query.Where(l => l.Fecha.Year == anio.Value);

            if (mes.HasValue)
                query = query.Where(l => l.Fecha.Month == mes.Value);

            if (!string.IsNullOrEmpty(cultivo))
                query = query.Where(l => l.Cultivo != null && l.Cultivo.Nombre == cultivo);

            var lecturas = await query
                .Select(l => new {
                    lat = l.Lat,
                    lng = l.Lng,
                    intensidad = l.HumedadSuelo < 30 ? 1 :
                                l.Viento > 20 ? 0.8 :
                                l.Precipitacion > 50 ? 0.6 :
                                0.3
                })
                .ToListAsync();

            return Ok(lecturas);
        }

        // GET: api/lecturas/{id}
        // Devuelve una lectura por ID con sus detalles
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Lectura>> GetById(int id)
        {
            var lectura = await _context.Lecturas
                .Include(l => l.Sensor)
                .Include(l => l.Cultivo)
                .FirstOrDefaultAsync(l => l.Id == id);

            return lectura == null ? NotFound() : lectura;
        }

        // POST: api/lecturas
        // Crea una nueva lectura
        [Authorize(Roles = "Agricultor,Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(LecturaDto dto)
        {
            // SensorId automático si no viene
            if (dto.SensorId == 0)
            {
                var ids = await _context.Sensores.Select(s => s.Id).ToListAsync();
                if (ids.Count == 0) return BadRequest("No hay sensores registrados.");
                dto.SensorId = ids[_rand.Next(ids.Count)];
            }

            // Equivalencia Cultivo → Id
            var map = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Maíz",1 }, { "Maiz",1 },
                { "Palta",2 },
                { "Espárrago",3 }, { "Esparrago",3 },
                { "Mango",4 }
            };

            if (dto.CultivoId == 0 && !string.IsNullOrWhiteSpace(dto.Cultivo))
            {
                if (!map.TryGetValue(dto.Cultivo, out int id))
                    return BadRequest($"Cultivo '{dto.Cultivo}' no reconocido.");
                dto.CultivoId = id;
            }

            var lectura = new Lectura
            {
                SensorId = dto.SensorId,
                CultivoId = dto.CultivoId,
                Fecha = dto.Fecha,
                HumedadSuelo = dto.HumedadSuelo,
                Temperatura = dto.Temperatura,
                Precipitacion = dto.Precipitacion,
                Viento = dto.Viento,
                RadiacionSolar = dto.RadiacionSolar,
                EtapaCultivo = dto.EtapaCultivo ?? "",
                NecesitaRiego = dto.NecesitaRiego ?? false,
                Lat = dto.Lat,
                Lng = dto.Lng,
                IndiceSequia = dto.IndiceSequia,
                MateriaOrganica = dto.MateriaOrganica,
                MetodoRiego = dto.MetodoRiego ?? "",
                pH_Suelo = dto.pH_Suelo
            };

            _context.Lecturas.Add(lectura);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = lectura.Id }, lectura);
        }

        // DELETE: api/lecturas/{id}
        // Elimina una lectura por ID    
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var lectura = await _context.Lecturas.FindAsync(id);
            if (lectura == null) return NotFound();

            _context.Lecturas.Remove(lectura);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ────────────────────────────
        // POST: api/lecturas/upload-csv
        // Carga masiva desde CSV
        // ────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost("upload-csv")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCsv([FromForm] FileUploadDto file)
        {
            if (file == null || file.File.Length == 0)
                return BadRequest("Archivo CSV no válido.");

            // 🔧 Diccionario de cultivos sin tildes
            var cultivoMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "maiz", 1 },
                { "palta", 2 },
                { "esparrago", 3 },
                { "mango", 4 }
            };

            // 🔧 Función para normalizar texto (quita tildes y pasa a minúsculas)
            string NormalizeText(string input)
            {
                return new string(
                    (input ?? "").Normalize(NormalizationForm.FormD)
                                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                                .ToArray()
                ).ToLowerInvariant();
            }

            var nuevas = new List<Lectura>();

            try
            {
                // 🔧 Intentamos primero con UTF-8, si falla usamos Latin-1
                StreamReader reader;
                try
                {
                    reader = new StreamReader(file.File.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    // Probar lectura para confirmar UTF-8 válido
                    reader.ReadLine();
                    reader.BaseStream.Position = 0; // reset
                    reader.DiscardBufferedData();
                }
                catch
                {
                    // Fallback a ISO-8859-1
                    reader = new StreamReader(file.File.OpenReadStream(), Encoding.GetEncoding("ISO-8859-1"));
                }

                var config = new CsvConfiguration(new CultureInfo("es-PE"))
                {
                    HasHeaderRecord = true,
                    Delimiter = ";",
                    BadDataFound = null,
                    HeaderValidated = null,
                    TrimOptions = TrimOptions.Trim,
                    MissingFieldFound = null
                };

                using var csv = new CsvReader(reader, config);
                csv.Context.RegisterClassMap<LecturaCsvMap>();
                var records = csv.GetRecords<LecturaCsvRow>().ToList();
                var idsSensores = await _context.Sensores.Select(s => s.Id).ToListAsync();
                if (idsSensores.Count == 0) return BadRequest("No hay sensores registrados.");

                int fila = 1;
                foreach (var r in records)
                {
                    try
                    {
                        int sensorId = r.SensorId ?? idsSensores[_rand.Next(idsSensores.Count)];

                        string cultivoKey = NormalizeText(r.Cultivo ?? "");
                        if (!cultivoMap.TryGetValue(cultivoKey, out int cultivoId))
                        {
                            Console.WriteLine($"Fila {fila}: Cultivo '{r.Cultivo}' no reconocido. Se omitirá.");
                            fila++;
                            continue;
                        }

                        var lectura = new Lectura
                        {
                            SensorId = sensorId,
                            CultivoId = cultivoId,
                            Fecha = r.Fecha,
                            HumedadSuelo = r.HumedadSuelo,
                            Temperatura = r.Temperatura,
                            Precipitacion = r.Precipitacion,
                            Viento = r.Viento,
                            RadiacionSolar = r.RadiacionSolar,
                            EtapaCultivo = r.EtapaCultivo ?? "",
                            NecesitaRiego = r.NecesitaRiego,
                            Lat = r.Lat,
                            Lng = r.Lng,
                            IndiceSequia = r.IndiceSequia,
                            MateriaOrganica = r.MateriaOrganica,
                            MetodoRiego = r.MetodoRiego ?? "",
                            pH_Suelo = r.pH_Suelo
                        };

                        nuevas.Add(lectura);
                    }
                    catch (Exception filaEx)
                    {
                        Console.WriteLine($"Error en fila {fila}: {filaEx.Message}");
                    }
                    fila++;
                }

                _context.Lecturas.AddRange(nuevas);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = $"Se importaron {nuevas.Count} lecturas." });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"ERROR GENERAL: {inner}");
                return StatusCode(500, $"Error al procesar CSV: {inner}");
            }
        }

    }


}
