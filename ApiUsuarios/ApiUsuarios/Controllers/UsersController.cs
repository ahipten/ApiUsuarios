using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;          // tu AppDbContext
using Models;        // tu clase User
using System.Security.Claims; // <-- Agrega esto

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context) => _context = context;

        // ─────────────────────────────────────────────────────────────
        // GET api/users   — listado (solo autenticados)
        // ─────────────────────────────────────────────────────────────
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _context.Users.AsNoTracking().ToListAsync());

        // ─────────────────────────────────────────────────────────────
        // POST api/users  — crear (solo Admin)
        // ─────────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
                Console.WriteLine($"🧾 Usuario autenticado: {User.Identity?.Name}");
                Console.WriteLine($"🧾 Rol: {User.FindFirst(ClaimTypes.Role)?.Value}");
            // Log de claims para depuración
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var claims = identity?.Claims.Select(c => $"{c.Type}: {c.Value}");
            Console.WriteLine("Claims recibidos en POST /api/users: " + string.Join(", ", claims ?? new string[0]));

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Devuelve 201 con ubicación
            return CreatedAtAction(nameof(GetAll), new { id = user.Id }, user);
        }

        // ─────────────────────────────────────────────────────────────
        // PUT api/users/{id}  — actualizar (propio o Admin)
        // ─────────────────────────────────────────────────────────────
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, User user)
        {
            if (id != user.Id) return BadRequest();

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser is null) return NotFound();

            // Permite que cada usuario cambie solo su cuenta o Admin
            var requestingUser = HttpContext.User;
            var isAdmin = requestingUser.IsInRole("Admin");
            if (!isAdmin && requestingUser.Identity?.Name != existingUser.Username)
                return Forbid();

            existingUser.Username = user.Username;
            existingUser.Role     = user.Role;

            if (!string.IsNullOrWhiteSpace(user.Password))
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE api/users/{id}  — eliminar (solo Admin)
        // ─────────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
