using FtelMarket.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

const string FrontCorsPolicy = "Front";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Erreurs non gérées et codes d'erreur sans corps => réponses ProblemDetails (RFC 9457).
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Horloge injectable : les dates de création sont testables et toujours en UTC.
builder.Services.AddSingleton(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("FtelMarket")
    ?? throw new InvalidOperationException(
        "La chaîne de connexion 'FtelMarket' est absente (voir appsettings.Development.json).");

builder.Services.AddDbContext<FtelMarketDbContext>(options => options
    .UseNpgsql(connectionString)
    // Tables et colonnes en snake_case, la convention PostgreSQL (pas de guillemets en SQL).
    .UseSnakeCaseNamingConvention()
    // Données initiales, appliquées par `dotnet ef database update`.
    .UseSeeding((context, _) => DataSeeder.Seed(context))
    .UseAsyncSeeding((context, _, cancellationToken) => DataSeeder.SeedAsync(context, cancellationToken)));

// Le front (http://localhost:3000) n'a pas la même origine que l'API :
// le navigateur bloque ses appels tant que l'API ne l'autorise pas explicitement.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(FrontCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Interface de test de l'API : http://localhost:5082/scalar
    app.MapScalarApiReference();
    // La racine n'est pas une route de l'API : on y affiche la documentation plutôt qu'un 404.
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}
else
{
    // En développement, le front appelle l'API en HTTP (port 5082) : une
    // redirection vers HTTPS casserait les requêtes CORS de pré-vérification.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(FrontCorsPolicy);

app.MapControllers();

app.Run();

// Rend la classe Program accessible au projet de tests (WebApplicationFactory<Program>).
public partial class Program;
