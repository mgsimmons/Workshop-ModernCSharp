using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WWSHA.Api.Contracts;
using WWSHA.Domain;
using Xunit;

namespace WWSHA.Api.Tests;

/// <summary>Filter combinations, detail lookups, and not-found paths for the read endpoints.</summary>
public class QueryAndNotFoundTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateTime Noon = new(2026, 11, 15, 12, 0, 0);

    private static WebApplicationFactory<Program> NewHost(string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment(environment));

    private static async Task<HeroDto> RegisterHero(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/heroes", new RegisterHeroRequest(name, "x"), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HeroDto>(Json))!;
    }

    private static async Task<IncidentDto> FileIncident(HttpClient client, string city,
        IncidentCategory category, DateTime? occurredAt = null)
    {
        var response = await client.PostAsJsonAsync("/api/incidents",
            new FileIncidentRequest(city, occurredAt ?? Noon, "x", category), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IncidentDto>(Json))!;
    }

    private static async Task<ReportDto> FileReport(HttpClient client, Guid incidentId, string city = "Redmond")
    {
        var response = await client.PostAsJsonAsync($"/api/incidents/{incidentId}/reports",
            new FileReportRequest(city, "x", ReportSeverity.Minor, 10, 0, Noon), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReportDto>(Json))!;
    }

    // ---- Incident filters and detail -------------------------------------------

    [Fact]
    public async Task Incident_filters_combine_status_city_and_category()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain");
        await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);   // InProgress
        await FileIncident(client, "Redmond", IncidentCategory.WanderingKaiju);    // Opened
        await FileIncident(client, "Seattle", IncidentCategory.DrunkenBarFight);   // Opened

        var match = await client.GetFromJsonAsync<IncidentDto[]>(
            "/api/incidents?status=InProgress&city=redmond&category=DrunkenBarFight", Json);
        var noMatch = await client.GetFromJsonAsync<IncidentDto[]>(
            "/api/incidents?status=Opened&category=DrunkenBarFight&city=Redmond", Json);
        var all = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents", Json);

        Assert.Single(match!);
        Assert.Empty(noMatch!);
        Assert.Equal(3, all!.Length);
    }

    [Fact]
    public async Task Incident_status_filter_matches_only_that_status()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await FileIncident(client, "Redmond", IncidentCategory.PettyCrime);   // no hero: Opened

        var opened = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents?status=Opened", Json);
        var closed = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents?status=Closed", Json);

        Assert.Single(opened!);
        Assert.Empty(closed!);
    }

    [Fact]
    public async Task Invalid_category_filter_returns_400_with_a_message()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.GetAsync("/api/incidents?category=Nonsense");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not a valid incident category", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Incident_detail_includes_its_assignment_and_reports()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain");
        var incident = await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);
        var report = await FileReport(client, incident.Id);

        var detail = await client.GetFromJsonAsync<IncidentDetailDto>($"/api/incidents/{incident.Id}", Json);

        Assert.Equal(incident.Id, detail!.Incident.Id);
        Assert.Equal(hero.Id, Assert.Single(detail.Assignment!.Heroes).Id);
        Assert.Equal(report.Id, Assert.Single(detail.Reports).Id);
    }

    [Fact]
    public async Task Incident_assignment_lookup_returns_the_assignment_for_a_worked_incident()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain");
        var incident = await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);

        var response = await client.GetAsync($"/api/incidents/{incident.Id}/assignment");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var assignment = await response.Content.ReadFromJsonAsync<AssignmentDto>(Json);
        Assert.Equal(hero.Id, Assert.Single(assignment!.Heroes).Id);
    }

    [Fact]
    public async Task Incident_reports_list_returns_404_for_an_unknown_incident()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/incidents/{Guid.NewGuid()}/reports")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/incidents/{Guid.NewGuid()}/assignment")).StatusCode);
    }

    [Fact]
    public async Task Incident_reports_list_is_empty_for_an_incident_with_no_reports()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var incident = await FileIncident(client, "Redmond", IncidentCategory.PettyCrime);

        var reports = await client.GetFromJsonAsync<ReportDto[]>($"/api/incidents/{incident.Id}/reports", Json);

        Assert.Empty(reports!);
    }

    // ---- Heroes -----------------------------------------------------------

    [Fact]
    public async Task Hero_list_without_a_filter_returns_everyone()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "A");
        await RegisterHero(client, "B");

        var heroes = await client.GetFromJsonAsync<HeroDto[]>("/api/heroes", Json);

        Assert.Equal(2, heroes!.Length);
    }

    [Fact]
    public async Task Hero_detail_returns_the_hero_and_404_for_unknown_ids()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain");

        var detail = await client.GetFromJsonAsync<HeroDetailDto>($"/api/heroes/{hero.Id}", Json);

        Assert.Equal("Captain", detail!.Name);
        Assert.Empty(detail.Assessments);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/heroes/{Guid.NewGuid()}")).StatusCode);
    }

    // ---- Assignments -----------------------------------------------------

    [Fact]
    public async Task Assignment_filters_by_incident_hero_and_open_state()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain");
        var first = await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);
        var second = await FileIncident(client, "Seattle", IncidentCategory.WanderingKaiju);
        await client.PostAsync($"/api/incidents/{first.Id}/close", null);

        var byIncident = await client.GetFromJsonAsync<AssignmentDto[]>(
            $"/api/assignments?incidentId={first.Id}", Json);
        var byHero = await client.GetFromJsonAsync<AssignmentDto[]>(
            $"/api/assignments?heroId={hero.Id}", Json);
        var closedOnly = await client.GetFromJsonAsync<AssignmentDto[]>("/api/assignments?open=false", Json);
        var all = await client.GetFromJsonAsync<AssignmentDto[]>("/api/assignments", Json);

        Assert.Equal(first.Id, Assert.Single(byIncident!).IncidentId);
        Assert.Equal(2, byHero!.Length);   // the first (closed) and the second (dispatched after the close)
        Assert.Single(closedOnly!);
        Assert.Equal(2, all!.Length);
    }

    [Fact]
    public async Task Assignment_detail_returns_404_for_an_unknown_id()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/assignments/{Guid.NewGuid()}")).StatusCode);
    }

    // ---- Reports and assessments ------------------------------------------

    [Fact]
    public async Task Assessment_filters_by_hero_incident_or_both()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain");
        var incident = await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);
        await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        var byHero = await client.GetFromJsonAsync<AssessmentDto[]>($"/api/assessments?heroId={hero.Id}", Json);
        var byIncident = await client.GetFromJsonAsync<AssessmentDto[]>(
            $"/api/assessments?incidentId={incident.Id}", Json);
        var both = await client.GetFromJsonAsync<AssessmentDto[]>(
            $"/api/assessments?heroId={hero.Id}&incidentId={incident.Id}", Json);
        var other = await client.GetFromJsonAsync<AssessmentDto[]>(
            $"/api/assessments?heroId={Guid.NewGuid()}", Json);
        var all = await client.GetFromJsonAsync<AssessmentDto[]>("/api/assessments", Json);

        Assert.Single(byHero!);
        Assert.Single(byIncident!);
        Assert.Single(both!);
        Assert.Empty(other!);
        Assert.Single(all!);
    }

    [Fact]
    public async Task Report_detail_returns_the_report()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var incident = await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);
        var report = await FileReport(client, incident.Id);

        var fetched = await client.GetFromJsonAsync<ReportDto>($"/api/reports/{report.Id}", Json);

        Assert.Equal(report.Id, fetched!.Id);
        Assert.Equal(10, fetched.Cost);
    }

    [Fact]
    public async Task Filters_that_match_nothing_return_empty_lists_not_errors()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain");
        await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);
        var missing = Guid.NewGuid();

        var incidents = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents?city=Atlantis", Json);
        var byIncident = await client.GetFromJsonAsync<AssignmentDto[]>($"/api/assignments?incidentId={missing}", Json);
        var byHero = await client.GetFromJsonAsync<AssignmentDto[]>($"/api/assignments?heroId={missing}", Json);
        var assessments = await client.GetFromJsonAsync<AssessmentDto[]>($"/api/assessments?incidentId={missing}", Json);

        Assert.Empty(incidents!);
        Assert.Empty(byIncident!);
        Assert.Empty(byHero!);
        Assert.Empty(assessments!);
    }

    // ---- Environment-specific behavior ------------------------------------

    [Fact]
    public async Task OpenAPI_is_not_served_outside_development()
    {
        using var host = NewHost("Production");
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }

    [Fact]
    public async Task Health_still_works_outside_development()
    {
        using var host = NewHost("Production");
        var client = host.CreateClient();

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health", Json);

        Assert.Equal("ok", health!.Status);
    }
}
