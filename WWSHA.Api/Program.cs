using System.Text.Json.Serialization;
using WWSHA.Api.Contracts;
using WWSHA.Api.Endpoints;
using WWSHA.Api.Errors;
using WWSHA.Api.Services;
using WWSHA.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Enums travel as names ("InProgress", "DrunkenBarFight") rather than numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// One clock and one in-memory store for the life of the process.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<InMemoryDomainRepository>();
builder.Services.AddSingleton<DomainService>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/api/health", () => Results.Ok(new HealthResponse("ok")))
    .WithTags("Health");

app.MapHeroEndpoints();
app.MapIncidentEndpoints();
app.MapAssignmentEndpoints();
app.MapReportAndAssessmentEndpoints();

app.Run();

// Lets integration tests reference the entry point via WebApplicationFactory.
public partial class Program;
