using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SensoresController(AppDbContext context) => _context = context;

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sensor>>> Get()
        {
            return await _context.Sensores.Include(s => s.Usuario).ToListAsync();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Sensor>> GetById(int id)
        {
            var sensor = await _context.Sensores.Include(s => s.Usuario).FirstOrDefaultAsync(s => s.Id == id);
            return sensor is null ? NotFound() : sensor;
        }

        [Authorize(Roles = "Agricultor,Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Sensor sensor)
        {
            _context.Sensores.Add(sensor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = sensor.Id }, sensor);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sensor = await _context.Sensores.FindAsync(id);
            if (sensor is null) return NotFound();

            _context.Sensores.Remove(sensor);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}