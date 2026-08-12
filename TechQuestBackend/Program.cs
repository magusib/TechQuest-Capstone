using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Data;
using TechQuestBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<OTPService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddDbContext<TechQuestDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowGodotLocalhost");

app.UseAuthorization();

app.MapControllers();

app.Run();