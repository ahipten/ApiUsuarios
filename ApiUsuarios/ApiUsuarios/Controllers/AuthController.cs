using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Data;
using Helpers;
using BCrypt.Net;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config  = config;
        }

        public record LoginDto(string Username, string Password);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            // 1️⃣ Buscar usuario
            var user = await _context.Users
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(u => u.Username == login.Username);

            if (user is null)
                return Unauthorized("Usuario o contraseña inválidos");

            // 2️⃣ Verificar contraseña con BCrypt
            try
            {
                if (!BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
                    return Unauthorized("Usuario o contraseña inválidos");
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Si la contraseña en BD no es un hash válido, la tratamos como inválida
                return Unauthorized("Usuario o contraseña inválidos");
            }

            // 3️⃣ Generar JWT
            var token = JwtHelper.GenerateToken(user, _config)
                                 .Replace("\r", "").Replace("\n", ""); // limpiamos saltos de línea

            // 4️⃣ Log opcional
            Console.WriteLine($"🔐 TOKEN: {token}");
            Console.WriteLine($"🔐 USERNAME: {user.Username}");
            Console.WriteLine($"🔐 ROLE: {user.Role}");

            return Ok(new
            {
                token,
                username = user.Username,
                role     = user.Role
            });
        }
    }
}
