using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Backend.Features.Auth;
using Backend.Features.Quizes;
using Backend.Features.GameRooms;
using Backend.Features.GameSessions;
using Backend.Data;
using Backend.Hubs;
using Backend.Shared;
using Backend.Data.Seeds;


var builder = WebApplication.CreateBuilder(args);
var oidcOptions = builder.Configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>()
    ?? new OidcOptions();
var oidcCredentials = OidcCredentials.Create(oidcOptions, builder.Environment.IsProduction());

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
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services
    .AddDefaultIdentity<QuizUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Stores.MaxLengthForKeys = 0;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = oidcOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = "quizit-api",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new X509SecurityKey(oidcCredentials.SigningCertificate),
            NameClaimType = OpenIddictConstants.Claims.Name,
            RoleClaimType = OpenIddictConstants.Claims.Role,
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Api", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
    options.AddPolicy("Authoring", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireRole(Roles.Admin, Roles.QuizAuthor);
    });
});
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.SectionName));
builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AppDbContext>())
    .AddServer(options =>
    {
        options.SetIssuer(new Uri(oidcOptions.Issuer));
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetEndSessionEndpointUris("/connect/logout")
            .AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .RequireProofKeyForCodeExchange()
            .RegisterScopes(
                OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Scopes.Roles,
                "quizit_api")
            .AddSigningCertificate(oidcCredentials.SigningCertificate)
            .AddEncryptionKey(oidcCredentials.EncryptionKey)
            .DisableAccessTokenEncryption();

        var aspNetCore = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            aspNetCore.DisableTransportSecurityRequirement();
        }
    });
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
builder.Services.AddScoped<UserManagementService>();
builder.Services.Configure<InitialAdminOptions>(
    builder.Configuration.GetSection(InitialAdminOptions.SectionName));
builder.Services.AddScoped<InitialAdminProvisioner>();
builder.Services.AddScoped<OidcClientProvisioner>();
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

    // Add roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(Roles.Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
    }
    if (!await roleManager.RoleExistsAsync(Roles.QuizAuthor))
    {
        await roleManager.CreateAsync(new IdentityRole(Roles.QuizAuthor));
    }

    var initialAdminProvisioner = scope.ServiceProvider
        .GetRequiredService<InitialAdminProvisioner>();
    await initialAdminProvisioner.ProvisionAsync();

    var oidcClientProvisioner = scope.ServiceProvider.GetRequiredService<OidcClientProvisioner>();
    await oidcClientProvisioner.ProvisionAsync();

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

// The default Identity UI contains a registration page, but accounts are admin-provisioned.
app.Use(async (context, next) =>
{
    if (string.Equals(
            context.Request.Path.Value,
            "/Identity/Account/Register",
            StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<GameHub>("/api/v1/hubs/game");
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();

app.Run();
