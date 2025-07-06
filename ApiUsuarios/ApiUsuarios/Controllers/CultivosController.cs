using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models;
using Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CultivosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CultivosController(AppDbContext context) => _context = context;

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cultivo>>> Get()
            => await _context.Cultivos.ToListAsync();

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Cultivo>> GetById(int id)
        {
            var cultivo = await _context.Cultivos.FindAsync(id);
            return cultivo is null ? NotFound() : cultivo;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Cultivo cultivo)
        {
            try
            {
                Console.WriteLine($"🧾 Intentando crear cultivo: {cultivo.Nombre}");

                // Verifica si ya existe un cultivo con el mismo nombre (ignorando mayúsculas/minúsculas)
                var exists = await _context.Cultivos
                    .AnyAsync(c => c.Nombre.ToLower() == cultivo.Nombre.ToLower());

                if (exists)
                    return Conflict($"El cultivo '{cultivo.Nombre}' ya existe.");

                _context.Cultivos.Add(cultivo);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = cultivo.Id }, cultivo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en Create Cultivo: {ex.Message}");
                return StatusCode(500, "Error interno al registrar cultivo.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cultivo = await _context.Cultivos.FindAsync(id);
            if (cultivo is null) return NotFound();

            _context.Cultivos.Remove(cultivo);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Cultivo cultivo)
        {
            if (id != cultivo.Id) return BadRequest("El ID no coincide.");

            var cultivoExistente = await _context.Cultivos.FindAsync(id);
            if (cultivoExistente == null) return NotFound();

            cultivoExistente.Nombre = cultivo.Nombre;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
