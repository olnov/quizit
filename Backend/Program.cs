using Microsoft.EntityFrameworkCore;
using Backend.Features.Quizes;
using Backend.Features.GameRooms;
using Backend.Features.GameSessions;
using Backend.Data;
using Backend.Hubs;
using Backend.Shared;
using Backend.Data.Seeds;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"))
);
builder.Services.AddControllers();
builder.Services.AddSignalR();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddScoped<QuizCatalog>();
builder.Services.AddSingleton<GameRoomService>();
builder.Services.AddHostedService<GameRoomCleanupService>();
builder.Services.AddHostedService<GameQuestionTimeoutService>();
builder.Services.AddScoped<GameSessionService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Database:SeedDemoData"))
    {
        await DevelopmentDataSeeder.SeedAsync(dbContext, CancellationToken.None);
    }
}

app.UseMiddleware<ApiExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Quizz API");
    });
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/api/v1/hubs/game");

app.Run();
