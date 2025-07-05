using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 🔗 SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
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
                    // Extraer y limpiar token
                    var token = rawHeader.Substring("Bearer ".Length).Trim();
                    token = token.Trim('\'', '"');
                    context.Token = token;

                    Console.WriteLine($"[TOKEN LIMPIO]: '{token}' (len: {token.Length})");
                }

                if (string.IsNullOrEmpty(context.Token))
                {
                    Console.WriteLine("[❌ JWT] Token no recibido o inválido.");
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ JWT Authentication failed: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"❗ Inner Exception: {context.Exception.InnerException.Message}");
                }
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

app.UseAuthentication(); // 🔐 primero autenticación
app.UseAuthorization();

app.MapControllers();
app.Run();
