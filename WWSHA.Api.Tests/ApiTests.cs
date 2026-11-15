using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using WWSHA.Api.Contracts;
using WWSHA.Domain;
using Xunit;

namespace WWSHA.Api.Tests;

public class ApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateTime Noon = new(2026, 11, 15, 12, 0, 0);

    // Each test gets its own host, so each starts with an empty store.
    private static WebApplicationFactory<Program> NewHost() => new();

    private static async Task<HeroDto> RegisterHero(HttpClient client, string name, int reputation = 0)
    {
        var response = await client.PostAsJsonAsync("/api/heroes",
            new RegisterHeroRequest(name, "Superpower", reputation), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HeroDto>(Json))!;
    }

    private static async Task<IncidentDto> FileIncident(HttpClient client, string city = "Redmond",
        IncidentCategory category = IncidentCategory.DrunkenBarFight)
    {
        var response = await client.PostAsJsonAsync("/api/incidents",
            new FileIncidentRequest(city, Noon, "Brawl", category), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IncidentDto>(Json))!;
    }

    private static async Task<IncidentDetailDto> GetIncident(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/incidents/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IncidentDetailDto>(Json))!;
    }

    // ---- Heroes ------------------------------------------------------------

    [Fact]
    public async Task Register_hero_returns_201_with_location()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/heroes",
            new RegisterHeroRequest("Captain Inconvenient", "Teleporting", 1), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var hero = await response.Content.ReadFromJsonAsync<HeroDto>(Json);
        Assert.Equal($"/api/heroes/{hero!.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(Availability.Available, hero.Availability);
        Assert.Equal(1, hero.Reputation);
    }

    [Fact]
    public async Task Register_hero_with_blank_name_returns_400()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/heroes", new RegisterHeroRequest(" ", "x"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_hero_returns_404()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/heroes/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/heroes/{Guid.NewGuid()}/assessments")).StatusCode);
    }

    [Fact]
    public async Task List_heroes_filters_by_availability()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var busy = await RegisterHero(client, "Busy");
        await RegisterHero(client, "Free");
        await FileIncident(client); // auto-assigns the first hero, "Busy"

        var available = await client.GetFromJsonAsync<HeroDto[]>("/api/heroes?available=true", Json);
        var unavailable = await client.GetFromJsonAsync<HeroDto[]>("/api/heroes?available=false", Json);

        Assert.Equal("Free", Assert.Single(available!).Name);
        Assert.Equal(busy.Id, Assert.Single(unavailable!).Id);
    }

    // ---- Incidents ---------------------------------------------------------

    [Fact]
    public async Task File_incident_auto_assigns_an_available_hero_and_returns_201()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");

        var response = await client.PostAsJsonAsync("/api/incidents",
            new FileIncidentRequest("Redmond", Noon, "Brawl", IncidentCategory.DrunkenBarFight), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var incident = await response.Content.ReadFromJsonAsync<IncidentDto>(Json);
        Assert.Equal(IncidentStatus.InProgress, incident!.Status);
        Assert.NotNull(incident.AssignmentId);
    }

    [Fact]
    public async Task File_incident_with_unknown_category_returns_400()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsync("/api/incidents",
            JsonContent("{\"city\":\"Redmond\",\"occurredAt\":\"2026-11-15T12:00:00\",\"description\":\"x\",\"category\":\"Nonsense\"}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_incidents_filters_and_rejects_invalid_enum_values()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await FileIncident(client, "Redmond", IncidentCategory.DrunkenBarFight);
        await FileIncident(client, "Seattle", IncidentCategory.WanderingKaiju);

        var byCity = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents?city=seattle", Json);
        var byCategory = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents?category=WanderingKaiju", Json);
        var bad = await client.GetAsync("/api/incidents?status=bogus");

        Assert.Equal("Seattle", Assert.Single(byCity!).City);
        Assert.Equal(IncidentCategory.WanderingKaiju, Assert.Single(byCategory!).Category);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Incident_detail_returns_404_when_missing()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/incidents/{Guid.NewGuid()}")).StatusCode);
    }

    // ---- Assignments -------------------------------------------------------

    [Fact]
    public async Task Assign_hero_to_opened_incident_then_busy_hero_gets_409()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Late");
        var incident = await FileIncident(client);      // hero is auto-assigned here
        var second = await FileIncident(client);        // no hero left: stays Opened

        var busy = await client.PostAsJsonAsync($"/api/incidents/{second.Id}/heroes",
            new AssignHeroRequest(hero.Id), Json);
        Assert.Equal(HttpStatusCode.Conflict, busy.StatusCode);

        // Free the hero by closing the first incident, then assign.
        var close = await client.PostAsync($"/api/incidents/{incident.Id}/close", null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);

        var assigned = await client.PostAsJsonAsync($"/api/incidents/{second.Id}/heroes",
            new AssignHeroRequest(hero.Id), Json);
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var assignment = await assigned.Content.ReadFromJsonAsync<AssignmentDto>(Json);
        Assert.Equal(hero.Id, Assert.Single(assignment!.Heroes).Id);
    }

    [Fact]
    public async Task Assign_unknown_hero_returns_404()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var incident = await FileIncident(client);

        var response = await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/heroes",
            new AssignHeroRequest(Guid.NewGuid()), Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_endpoints_return_heroes_and_reports()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);
        await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/reports",
            new FileReportRequest("Redmond", "Broken table", Noon), Json);

        var forIncident = await client.GetAsync($"/api/incidents/{incident.Id}/assignment");
        var assignment = await forIncident.Content.ReadFromJsonAsync<AssignmentDto>(Json);
        var byId = await client.GetFromJsonAsync<AssignmentDto>($"/api/assignments/{assignment!.Id}", Json);
        var byHero = await client.GetFromJsonAsync<AssignmentDto[]>($"/api/assignments?heroId={hero.Id}", Json);
        var open = await client.GetFromJsonAsync<AssignmentDto[]>("/api/assignments?open=true", Json);

        Assert.Equal(hero.Id, Assert.Single(byId!.Heroes).Id);
        Assert.Single(byId.Reports);
        Assert.Single(byHero!);
        Assert.Single(open!);
    }

    // ---- Reports -----------------------------------------------------------

    [Fact]
    public async Task Report_rules_are_enforced()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var opened = await FileIncident(client);   // no hero registered: stays Opened
        await RegisterHero(client, "Late");

        var missing = await client.PostAsJsonAsync($"/api/incidents/{Guid.NewGuid()}/reports",
            new FileReportRequest("Redmond", "x", Noon), Json);
        var notInProgress = await client.PostAsJsonAsync($"/api/incidents/{opened.Id}/reports",
            new FileReportRequest("Redmond", "x", Noon), Json);

        var inProgress = await FileIncident(client, "Redmond");
        var wrongCity = await client.PostAsJsonAsync($"/api/incidents/{inProgress.Id}/reports",
            new FileReportRequest("Seattle", "x", Noon), Json);

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, notInProgress.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongCity.StatusCode);
    }

    [Fact]
    public async Task Filed_report_can_be_fetched_by_id_and_listed_for_its_incident()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);

        var created = await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/reports",
            new FileReportRequest("Redmond", "Broken table", Noon), Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var report = await created.Content.ReadFromJsonAsync<ReportDto>(Json);

        var fetched = await client.GetFromJsonAsync<ReportDto>($"/api/reports/{report!.Id}", Json);
        var listed = await client.GetFromJsonAsync<ReportDto[]>($"/api/incidents/{incident.Id}/reports", Json);

        Assert.Equal(report.Id, fetched!.Id);
        Assert.Equal(report.Id, Assert.Single(listed!).Id);
    }

    // ---- Closing and assessments -------------------------------------------

    [Fact]
    public async Task Close_incident_assesses_hero_and_frees_them()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain Inconvenient", reputation: 1);
        var incident = await FileIncident(client);
        await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/reports",
            new FileReportRequest("Redmond", "Broken table", Noon), Json);

        var response = await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var closed = await response.Content.ReadFromJsonAsync<CloseIncidentResponse>(Json);
        Assert.Equal(IncidentStatus.Closed, closed!.Incident.Status);
        Assert.Equal(4, Assert.Single(closed.Assessments).ReputationChange);

        var heroNow = await client.GetFromJsonAsync<HeroDetailDto>($"/api/heroes/{hero.Id}", Json);
        Assert.Equal(Availability.Available, heroNow!.Availability);
        Assert.Equal(5, heroNow.Reputation);
        Assert.Single(heroNow.Assessments);

        var assessments = await client.GetFromJsonAsync<AssessmentDto[]>(
            $"/api/assessments?heroId={hero.Id}&incidentId={incident.Id}", Json);
        Assert.Single(assessments!);
    }

    [Fact]
    public async Task Close_rejects_opened_and_already_closed_incidents_with_409()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var opened = await FileIncident(client);      // no heroes yet: Opened
        await RegisterHero(client, "Late");

        var openedClose = await client.PostAsync($"/api/incidents/{opened.Id}/close", null);
        Assert.Equal(HttpStatusCode.Conflict, openedClose.StatusCode);

        var toClose = await FileIncident(client);     // gets the hero: InProgress
        await client.PostAsync($"/api/incidents/{toClose.Id}/close", null);
        var again = await client.PostAsync($"/api/incidents/{toClose.Id}/close", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Closed_incident_rejects_new_reports_with_409()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);
        await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        var response = await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/reports",
            new FileReportRequest("Redmond", "too late", Noon), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---- Helpers -----------------------------------------------------------

    private static StringContent JsonContent(string body) =>
        new(body, System.Text.Encoding.UTF8, "application/json");
}
