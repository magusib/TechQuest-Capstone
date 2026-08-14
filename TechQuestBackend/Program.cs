using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechQuestBackend.Data;
using TechQuestBackend.Models;
using TechQuestBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<OTPService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddDbContext<TechQuestDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "TECHQUEST_SECRET_KEY_CHANGE_THIS_TO_A_LONG_RANDOM_VALUE";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TechQuestBackend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TechQuestGodot";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
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
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User
            {
                FirstName = "System",
                LastName = "Admin",
                Email = "admin@paterostechnologicalcollege.edu.ph",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "admin",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                FirstName = "System",
                LastName = "Professor",
                Email = "professor@paterostechnologicalcollege.edu.ph",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Professor123!"),
                Role = "professor",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowGodotLocalhost");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();