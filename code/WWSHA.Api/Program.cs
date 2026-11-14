using System.Text.Json.Serialization;
using WWSHA.Api.Contracts;
using WWSHA.Api.Endpoints;
using WWSHA.Api.Infrastructure;
using WWSHA.Persistence;
using WWSHA.Persistence.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryDomainRepository>();
builder.Services.AddSingleton<IDomainRepository>(
    services => services.GetRequiredService<InMemoryDomainRepository>());
builder.Services.AddSingleton<IDomainContext>(
    services => services.GetRequiredService<InMemoryDomainRepository>());

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();

app.MapGet("/api/health", () => TypedResults.Ok(new HealthResponse("ok")))
    .WithName("GetHealth")
    .WithSummary("Returns the API health status.")
    .Produces<HealthResponse>(StatusCodes.Status200OK);

var api = app.MapGroup("/api");
api.MapHeroEndpoints();
api.MapIncidentEndpoints();
api.MapAssignmentEndpoints();
api.MapReportAndAssessmentEndpoints();

app.Run();
