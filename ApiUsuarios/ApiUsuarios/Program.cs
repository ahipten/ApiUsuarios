using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;   // ← Necesario
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// 🔗 SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Controladores con IgnoreCycles
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Evita excepciones por ciclos de navegación (Sensor ↔ Usuario, etc.)
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔐 JWT Config
var jwt = builder.Configuration.GetSection("Jwt");
Console.WriteLine($"👉 JWT SETTINGS | Issuer: {jwt["Issuer"]} | Audience: {jwt["Audience"]} | Key (len): {jwt["Key"]?.Length}");

var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt["Issuer"],
            ValidAudience            = jwt["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(key),
            RoleClaimType            = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var rawHeader = context.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"[Authorization RAW]: {rawHeader}");

                if (!string.IsNullOrEmpty(rawHeader) && rawHeader.StartsWith("Bearer "))
                {
                    var token = rawHeader.Substring("Bearer ".Length).Trim(' ', '\'', '"');
                    context.Token = token;
                    Console.WriteLine($"[TOKEN LIMPIO]: '{token}' (len: {token.Length})");
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ JWT Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ JWT Token validado correctamente.");
                return Task.CompletedTask;
            }
        };
    });

// 🌍 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.UseAuthentication();   // primero autenticación
app.UseAuthorization();

app.MapControllers();

// 🔄 Migración automática de contraseñas a BCrypt
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = db.Users.Where(u => !u.Password.StartsWith("$2")).ToList();

    foreach (var user in users)
    {
        Console.WriteLine($"🔐 Migrando contraseña para: {user.Username}");
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
    }

    if (users.Any())
    {
        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Migradas {users.Count} contraseñas.");
    }
    else
    {
        Console.WriteLine("🔍 No hay contraseñas sin migrar.");
    }
}

app.Run();
