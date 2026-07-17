using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Backend.Features.Quizes;
using Backend.Features.GameRooms;
using Backend.Features.GameSessions;
using Backend.Data;
using Backend.Hubs;
using Backend.Shared;
using Backend.Data.Seeds;


var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
var postgresConnectionString = PostgresConnectionString.Normalize(
    new string?[]
    {
        builder.Configuration.GetConnectionString("Postgres"),
        builder.Configuration["DATABASE_PRIVATE_URL"],
        builder.Configuration["DATABASE_URL"],
    }.FirstOrDefault(connectionString => !string.IsNullOrWhiteSpace(connectionString))
    ?? throw new InvalidOperationException("PostgreSQL connection string is not configured."));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));
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

var isRailway = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT"));
if (isRailway)
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };

    // Railway terminates TLS before forwarding requests to the container.
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);
}

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

if (!app.Environment.IsProduction() || isRailway)
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/api/v1/hubs/game");
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();

app.Run();
