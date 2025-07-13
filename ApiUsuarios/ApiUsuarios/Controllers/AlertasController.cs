using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AlertasController : ControllerBase
{
    private readonly AppDbContext _context;

    public AlertasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/alertas
    [HttpGet]
    public async Task<IActionResult> GetAlertas()
    {
        var alertas = await _context.Lecturas
            .Include(l => l.Cultivo) // Trae el nombre del cultivo directamente
            .OrderByDescending(l => l.Fecha)
            .Take(20)
            .Where(l =>
                l.HumedadSuelo < 30 ||
                l.Viento > 20 ||
                l.Precipitacion > 50)
            .Select(l => new
            {
                Cultivo = l.Cultivo != null ? l.Cultivo.Nombre : "Desconocido",
                Fecha = l.Fecha,
                Mensaje = l.HumedadSuelo < 30 ? "⚠️ Humedad baja" :
                          l.Viento > 20 ? "🌬️ Viento fuerte" :
                          l.Precipitacion > 50 ? "🌧️ Lluvia intensa" :
                          null
            })
            .Where(a => a.Mensaje != null)
            .ToListAsync();

        return Ok(alertas);
    }
}
