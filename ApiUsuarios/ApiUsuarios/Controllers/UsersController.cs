using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;          // tu AppDbContext
using Models;        // tu clase User
using Models.Dtos;   // DTOs
using System.Security.Claims;
using BCrypt.Net;    // Para BCrypt

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
        {
            // Devolver UserResponseDto para no exponer contraseñas hasheadas
            var users = await _context.Users
                                    .AsNoTracking()
                                    .Select(u => new UserResponseDto { Id = u.Id, Username = u.Username, Role = u.Role })
                                    .ToListAsync();
            return Ok(users);
        }

        // ─────────────────────────────────────────────────────────────
        // POST api/users  — crear (solo Admin)
        // ─────────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Log de claims para depuración (opcional, puedes quitarlo si ya no es necesario)
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var claims = identity?.Claims.Select(c => $"{c.Type}: {c.Value}");
            Console.WriteLine("Claims recibidos en POST /api/users: " + string.Join(", ", claims ?? Array.Empty<string>()));

            // Verificar si el nombre de usuario ya existe
            if (await _context.Users.AnyAsync(u => u.Username == createUserDto.Username))
            {
                return Conflict(new { message = $"El nombre de usuario '{createUserDto.Username}' ya está en uso." });
            }

            var user = new User
            {
                Username = createUserDto.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                // Role se tomará del valor por defecto en el modelo User ("Agricultor")
                // Si quisieras permitir que el DTO especifique el rol, descomenta la propiedad Role en CreateUserDto
                // y asígnala aquí: Role = createUserDto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userResponseDto = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            };

            return CreatedAtAction(nameof(GetUserById), new { id = userResponseDto.Id }, userResponseDto);
        }

        // GET api/users/{id} - Para que CreatedAtAction funcione correctamente y para obtener un usuario
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Solo el propio usuario o un Admin pueden ver los detalles
            var requestingUser = HttpContext.User;
            var isAdmin = requestingUser.IsInRole("Admin");
            var isCurrentUser = requestingUser.Identity?.Name == user.Username;

            if (!isAdmin && !isCurrentUser)
            {
                // Podrías devolver NotFound también para no revelar la existencia del usuario
                // return NotFound();
                return Forbid();
            }

            var userResponseDto = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            };

            return Ok(userResponseDto);
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
