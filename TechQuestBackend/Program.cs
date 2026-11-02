using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using TechQuestBackend.Data;
using TechQuestBackend.Models;
using TechQuestBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddScoped<OTPService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IQuestProgressService, QuestProgressService>();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<TechQuestDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key must be configured with a secure secret.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TechQuestBackend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TechQuestGodot";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RoleClaimType = "role",
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGodotLocalhost", policy =>
    {
        policy
            .WithOrigins("http://localhost", "https://localhost")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TechQuestDbContext>();
    string[][] miniGames =
    {
        new[] { "Match the Pairs", "MatchThePairs" },
        new[] { "Falling Duck", "FallingDuck" },
        new[] { "Quick Tap Challenge", "QuickTapChallenge" }
    };
    foreach (string[] miniGame in miniGames)
    {
        if (!db.MiniGames.Any(item => item.GameType == miniGame[1]))
        {
            db.MiniGames.Add(new MiniGame
            {
                GameName = miniGame[0],
                GameType = miniGame[1],
                CreatedAt = DateTime.UtcNow
            });
        }
    }
    db.SaveChanges();

    if (app.Environment.IsDevelopment())
    {
        // Keep development credentials outside the source code.
        var adminPassword = builder.Configuration["SeedUsers:AdminPassword"];
        var professorPassword = builder.Configuration["SeedUsers:ProfessorPassword"];

        if (!string.IsNullOrWhiteSpace(adminPassword) || !string.IsNullOrWhiteSpace(professorPassword))
        {
            if (string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(professorPassword))
                throw new InvalidOperationException(
                    "Both SeedUsers:AdminPassword and SeedUsers:ProfessorPassword must be configured.");

            const string adminEmail = "admin@paterostechnologicalcollege.edu.ph";
            const string professorEmail = "professor@paterostechnologicalcollege.edu.ph";

            if (!db.Users.Any(user => user.Email == adminEmail))
            {
                db.Users.Add(new User
                {
                    FirstName = "System",
                    LastName = "Admin",
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = "admin",
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!db.Users.Any(user => user.Email == professorEmail))
            {
                db.Users.Add(new User
                {
                    FirstName = "System",
                    LastName = "Professor",
                    Email = professorEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(professorPassword),
                    Role = "professor",
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.SaveChanges();
        }
    }

    if (!db.Users.Any() && !app.Environment.IsDevelopment())
    {
        var adminEmail = builder.Configuration["BootstrapAdmin:Email"];
        var adminPassword = builder.Configuration["BootstrapAdmin:Password"];
        if (!string.IsNullOrWhiteSpace(adminEmail) || !string.IsNullOrWhiteSpace(adminPassword))
        {
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                throw new InvalidOperationException("Both BootstrapAdmin:Email and BootstrapAdmin:Password must be configured.");

            db.Users.Add(new User
            {
                FirstName = "System",
                LastName = "Admin",
                Email = adminEmail.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = "admin",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(NpcImageStorage.GetFolder(app.Environment)),
    RequestPath = "/uploads/npcs"
});

app.UseCors("AllowGodotLocalhost");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
