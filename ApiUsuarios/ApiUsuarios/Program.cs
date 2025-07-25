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
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// 🔗 SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔮 ML.NET modelo predictivo
builder.Services.AddPredictionEnginePool<LecturaInput, LecturaPrediction>()
    .FromFile("MLModels/ModeloRiego.zip", watchForChanges: true);

// ✅ Controladores con IgnoreCycles (evita errores de referencias circulares al serializar JSON)
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 🌍 CORS para permitir acceso desde React u otro frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

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

// 🌐 Swagger con soporte para carga de archivos
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
  //  options.OperationFilter<FileUploadOperation>(); // ✅ Permite manejar IFormFile en Swagger

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Riego API",
        Version = "v1",
        Description = "API para gestión y predicción de riego agrícola."
    });
});

var app = builder.Build();

// Middleware de documentación
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Riego API V1");
});

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🔄 Migración automática de contraseñas (una sola vez al iniciar)
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
            Cultivo = "Maiz",
            //Fecha = DateTime.Today
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
