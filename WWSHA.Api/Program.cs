using WWSHA.Api.Endpoints;
using WWSHA.Api.Errors;
using WWSHA.Api.Services;
using System.Text.Json.Serialization;
using WWSHA.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Enums travel as names ("InProgress", "DrunkenBarFight") rather than numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Single in-memory store for the life of the process; DomainService serializes access to it.
builder.Services.AddSingleton<InMemoryDomainRepository>();
builder.Services.AddSingleton<DomainService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapHeroEndpoints();
app.MapIncidentEndpoints();
app.MapAssignmentEndpoints();
app.MapReportAndAssessmentEndpoints();

app.Run();

// Lets integration tests reference the entry point via WebApplicationFactory.
public partial class Program;
