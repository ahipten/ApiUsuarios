using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const int LIMITE_ALERTAS = 20;

        public AlertasController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene las alertas recientes basadas en condiciones críticas del clima y suelo.
        /// </summary>
        /// <returns>Lista de alertas con tipo, nivel de riesgo, cultivo y fecha.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAlertas()
        {
            var lecturas = await _context.Lecturas
                .Include(l => l.Cultivo)
                .OrderByDescending(l => l.Fecha)
                .Take(LIMITE_ALERTAS)
                .ToListAsync();

            var alertas = new List<object>();

            foreach (var lectura in lecturas)
            {
                var cultivo = lectura.Cultivo?.Nombre ?? "Desconocido";

                // 🔹 Humedad baja
                if (lectura.HumedadSuelo < 20)
                {
                    alertas.Add(CrearAlerta(
                        tipo: "Humedad crítica",
                        icono: "🚨",
                        cultivo: cultivo,
                        fecha: lectura.Fecha,
                        detalle: $"El suelo tiene solo {lectura.HumedadSuelo}% de humedad. Se requiere riego urgente.",
                        nivel: "Alto"
                    ));
                }
                else if (lectura.HumedadSuelo < 30)
                {
                    alertas.Add(CrearAlerta(
                        tipo: "Humedad baja",
                        icono: "⚠️",
                        cultivo: cultivo,
                        fecha: lectura.Fecha,
                        detalle: $"La humedad del suelo es {lectura.HumedadSuelo}%. Se recomienda revisar el riego.",
                        nivel: "Moderado"
                    ));
                }

                // 🔹 Viento fuerte
                if (lectura.Viento > 40)
                {
                    alertas.Add(CrearAlerta(
                        tipo: "Viento extremo",
                        icono: "🌪️",
                        cultivo: cultivo,
                        fecha: lectura.Fecha,
                        detalle: $"Velocidad del viento: {lectura.Viento} km/h. Posible daño a cultivos.",
                        nivel: "Alto"
                    ));
                }
                else if (lectura.Viento > 20)
                {
                    alertas.Add(CrearAlerta(
                        tipo: "Viento fuerte",
                        icono: "🌬️",
                        cultivo: cultivo,
                        fecha: lectura.Fecha,
                        detalle: $"Velocidad del viento: {lectura.Viento} km/h. Evita el riego por aspersión.",
                        nivel: "Moderado"
                    ));
                }

                // 🔹 Lluvia intensa
                if (lectura.Precipitacion > 80)
                {
                    alertas.Add(CrearAlerta(
                        tipo: "Lluvia extrema",
                        icono: "⛈️",
                        cultivo: cultivo,
                        fecha: lectura.Fecha,
                        detalle: $"Precipitación registrada: {lectura.Precipitacion} mm. Riesgo de anegamiento.",
                        nivel: "Alto"
                    ));
                }
                else if (lectura.Precipitacion > 50)
                {
                    alertas.Add(CrearAlerta(
                        tipo: "Lluvia intensa",
                        icono: "🌧️",
                        cultivo: cultivo,
                        fecha: lectura.Fecha,
                        detalle: $"Precipitación registrada: {lectura.Precipitacion} mm. Suspende riegos programados.",
                        nivel: "Moderado"
                    ));
                }
            }

            if (!alertas.Any())
            {
                return Ok(new { Mensaje = "✅ No hay alertas activas. Las condiciones son estables." });
            }

            return Ok(alertas);
        }

        /// <summary>
        /// Crea un objeto de alerta estandarizado con nivel de riesgo.
        /// </summary>
        private static object CrearAlerta(string tipo, string icono, string cultivo, DateTime fecha, string detalle, string nivel)
        {
            return new
            {
                Tipo = tipo,
                Icono = icono,
                Cultivo = cultivo,
                Fecha = fecha,
                Nivel = nivel, // "Bajo", "Moderado", "Alto"
                Descripcion = detalle
            };
        }
    }
}
