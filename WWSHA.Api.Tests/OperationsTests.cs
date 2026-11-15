using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WWSHA.Api.Contracts;
using WWSHA.Domain;
using Xunit;

namespace WWSHA.Api.Tests;

/// <summary>A clock the test can break on demand, to exercise the unexpected-error path.</summary>
internal sealed class SwitchableClock : TimeProvider
{
    public bool Fail { get; set; }

    public override DateTimeOffset GetUtcNow() =>
        Fail
            ? throw new InvalidOperationException("clock-secret: internal detail that must not leak")
            : DateTimeOffset.UtcNow;
}

/// <summary>Health, documentation, validation, and failure-path behavior of the running API.</summary>
public class OperationsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static WebApplicationFactory<Program> NewHost(TimeProvider? clock = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            if (clock is not null)
                builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(clock));
        });

    private static async Task<string?> ProblemCode(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [Fact]
    public async Task Health_rejects_unsupported_methods_with_405()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsync("/api/health", null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task The_OpenAPI_document_lists_the_api_routes_in_development()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"/api/heroes\"", document);
        Assert.Contains("\"/api/incidents/{id}/close\"", document);
    }

    [Fact]
    public async Task Overlong_names_are_rejected_with_400_and_a_code()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/heroes",
            new RegisterHeroRequest(new string('x', DomainLimits.NameMaxLength + 1), "x"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", await ProblemCode(response));
    }

    [Fact]
    public async Task Out_of_range_starting_reputation_is_rejected_with_400()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/heroes",
            new RegisterHeroRequest("Captain", "x", DomainLimits.StartingReputationMax + 1), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Incidents_in_the_future_are_rejected_with_400_and_the_incident_invalid_code()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/incidents",
            new FileIncidentRequest("Redmond", DateTime.Now.AddDays(5), "Future", IncidentCategory.PettyCrime), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("incident.invalid", await ProblemCode(response));
    }

    [Fact]
    public async Task Unexpected_failures_return_a_generic_500_that_does_not_leak_details()
    {
        var clock = new SwitchableClock();
        using var host = NewHost(clock);
        var client = host.CreateClient();
        await client.PostAsJsonAsync("/api/heroes", new RegisterHeroRequest("Captain", "x"), Json);

        clock.Fail = true;
        var response = await client.PostAsJsonAsync("/api/incidents",
            new FileIncidentRequest("Redmond", DateTime.Now, "Brawl", IncidentCategory.DrunkenBarFight), Json);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("clock-secret", body);
        Assert.Contains("An unexpected error occurred.", body);
        Assert.Equal("server.error", await ProblemCode(response));
    }

    [Fact]
    public async Task A_failed_incident_filing_leaves_no_incident_behind()
    {
        var clock = new SwitchableClock();
        using var host = NewHost(clock);
        var client = host.CreateClient();
        clock.Fail = true;

        await client.PostAsJsonAsync("/api/incidents",
            new FileIncidentRequest("Redmond", DateTime.Now, "Brawl", IncidentCategory.DrunkenBarFight), Json);

        clock.Fail = false;
        var incidents = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents", Json);
        Assert.Empty(incidents!);
    }
}
