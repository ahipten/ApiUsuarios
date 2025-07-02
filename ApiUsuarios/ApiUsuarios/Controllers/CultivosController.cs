using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CultivosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CultivosController(AppDbContext context) => _context = context;

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cultivo>>> Get()
        {
            return await _context.Cultivos.ToListAsync();
        }

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
            _context.Cultivos.Add(cultivo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = cultivo.Id }, cultivo);
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
    }
}