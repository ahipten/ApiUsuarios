using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;
using BCrypt.Net;
using Microsoft.Extensions.ML;
using Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 🔗 SQL Server
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// 🔮 ML.NET Modelo predictivo
// ============================================================
builder.Services.AddPredictionEnginePool<LecturaInput, LecturaPrediction>()
    .FromFile("MLModels/ModeloRiego.zip", watchForChanges: true);

// ============================================================
// ✅ Controladores con IgnoreCycles (evita referencias circulares)
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// ============================================================
// 🌍 CORS — Permitir acceso desde React (localhost y producción)
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",   // Vite o CoreUI React
                "http://localhost:3000",   // CRA u otros
                "https://casmainteligente.pe" // Producción (ajústalo a tu dominio)
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// ============================================================
// 🔐 JWT Configuración
// ============================================================
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
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            RoleClaimType = ClaimTypes.Role
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

// ============================================================
// 🌐 Swagger con soporte para carga de archivos
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Riego API",
        Version = "v1",
        Description = "API para gestión y predicción de riego agrícola."
    });

    // 🔒 Permitir autenticación con JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT con el prefijo 'Bearer '",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
// ============================================================
// ⚙️ Aumentar límite de carga de archivos grandes (hasta 500 MB)
// ============================================================
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000; // 500 MB
});
var app = builder.Build();

// ============================================================
// 📘 Swagger Middleware
// ============================================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Riego API V1");
});

// ============================================================
// 🛰 Middleware CORS + Preflight
// ============================================================
app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        var origin = context.Request.Headers["Origin"];
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        }

        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
        context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");

        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }

    await next();
});


// ============================================================
// 🔑 Autenticación y autorización
// ============================================================
app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// 🚀 Controladores
// ============================================================
app.MapControllers();

// ============================================================
// 🧩 Migración automática de contraseñas y validación de modelo ML
// ============================================================
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

    // ✅ Verificación del modelo ML.NET
    var predPool = scope.ServiceProvider.GetRequiredService<PredictionEnginePool<LecturaInput, LecturaPrediction>>();
    try
    {
        var resultado = predPool.Predict(new LecturaInput
        {
            HumedadSuelo = 20,
            Temperatura = 25,
            Precipitacion = 5,
            Viento = 2,
            RadiacionSolar = 100,
            EtapaCultivo = "Crecimiento",
            Cultivo = "Maiz"
        });

        Console.WriteLine($"✅ Modelo ML.NET cargado correctamente. ¿Necesita riego?: {resultado.NecesitaRiego}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al cargar o usar el modelo ML.NET: {ex.Message}");
    }
}

app.UseStaticFiles();
app.Run();
