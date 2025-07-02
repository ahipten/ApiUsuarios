using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LecturasController : ControllerBase
    {
        private readonly AppDbContext _context;
        public LecturasController(AppDbContext context) => _context = context;

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lectura>>> Get()
        {
            return await _context.Lecturas
                .Include(l => l.Cultivo)
                .Include(l => l.Sensor)
                .ToListAsync();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Lectura>> GetById(int id)
        {
            var lectura = await _context.Lecturas
                .Include(l => l.Cultivo)
                .Include(l => l.Sensor)
                .FirstOrDefaultAsync(l => l.Id == id);

            return lectura is null ? NotFound() : lectura;
        }

        [Authorize(Roles = "Agricultor,Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Lectura lectura)
        {
            _context.Lecturas.Add(lectura);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = lectura.Id }, lectura);
        }

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
    }
}