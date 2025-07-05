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
            // Buscar usuario por nombre
            var user = await _context.Users
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(u => u.Username == login.Username);

            if (user == null)
                return Unauthorized("Usuario o contraseña inválidos");

            // Verificar contraseña con BCrypt
            //if (!BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
            //   return Unauthorized("Usuario o contraseña inválidos");

            // Generar JWT
            var token = JwtHelper.GenerateToken(user, _config);

            //Imprimir en consola
            Console.WriteLine($"🔐 TOKEN: {token}");
            Console.WriteLine($"🔐 ROLE: {user.Role}");
            // Opcional: devolver también info del usuario
            return Ok(new
            {
                token,
                username = user.Username,
                role = user.Role
            });
            
        }
    }
}
