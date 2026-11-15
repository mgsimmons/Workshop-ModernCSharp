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

    private static Task<HttpResponseMessage> FileReport(HttpClient client, Guid incidentId,
        string city = "Redmond", int cost = 0) =>
        client.PostAsJsonAsync($"/api/incidents/{incidentId}/reports",
            new FileReportRequest(city, "Broken table", ReportSeverity.Minor, cost, 0, Noon), Json);

    private static async Task<IncidentDetailDto> GetIncident(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/incidents/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IncidentDetailDto>(Json))!;
    }

    private static async Task<string?> ProblemCode(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    // ---- Health and documentation -----------------------------------------

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health", Json);

        Assert.Equal("ok", health!.Status);
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
    public async Task Register_hero_with_blank_name_returns_400_with_a_code()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/heroes", new RegisterHeroRequest(" ", "x"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", await ProblemCode(response));
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
    public async Task Hero_assessments_return_200_for_a_known_hero()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);
        await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        var response = await client.GetAsync($"/api/heroes/{hero.Id}/assessments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single((await response.Content.ReadFromJsonAsync<AssessmentDto[]>(Json))!);
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
    public async Task Closing_an_incident_dispatches_the_freed_hero_to_the_waiting_one()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Late");
        var first = await FileIncident(client);      // hero is auto-assigned here
        var waiting = await FileIncident(client);    // no hero left: stays Opened

        Assert.Equal(IncidentStatus.Opened, (await GetIncident(client, waiting.Id)).Incident.Status);

        var close = await client.PostAsync($"/api/incidents/{first.Id}/close", null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);

        var detail = await GetIncident(client, waiting.Id);
        Assert.Equal(IncidentStatus.InProgress, detail.Incident.Status);
        Assert.Equal(hero.Id, Assert.Single(detail.Assignment!.Heroes).Id);
    }

    [Fact]
    public async Task Assign_busy_hero_returns_409_with_hero_unavailable_code()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Late");
        await FileIncident(client);                  // hero is busy on this one
        var second = await FileIncident(client);     // waiting

        var response = await client.PostAsJsonAsync($"/api/incidents/{second.Id}/heroes",
            new AssignHeroRequest(hero.Id), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("hero.unavailable", await ProblemCode(response));
    }

    [Fact]
    public async Task Assign_the_same_hero_twice_to_one_incident_returns_409()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);   // hero already assigned here

        var response = await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/heroes",
            new AssignHeroRequest(hero.Id), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("assignment.duplicate", await ProblemCode(response));
    }

    [Fact]
    public async Task Assign_fourth_hero_returns_409_with_assignment_full_code()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var heroes = new List<HeroDto>();
        for (int i = 0; i < 4; i++)
            heroes.Add(await RegisterHero(client, $"Hero {i}"));
        var incident = await FileIncident(client);   // hero 0 auto-assigned

        await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/heroes", new AssignHeroRequest(heroes[1].Id), Json);
        await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/heroes", new AssignHeroRequest(heroes[2].Id), Json);
        var full = await client.PostAsJsonAsync($"/api/incidents/{incident.Id}/heroes",
            new AssignHeroRequest(heroes[3].Id), Json);

        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Equal("assignment.full", await ProblemCode(full));
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
    public async Task Assignment_for_an_incident_without_heroes_returns_404()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var incident = await FileIncident(client);   // no heroes: Opened, no assignment

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/incidents/{incident.Id}/assignment")).StatusCode);
    }

    [Fact]
    public async Task Assignment_endpoints_return_heroes_reports_and_timestamps()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);
        await FileReport(client, incident.Id);

        var forIncident = await client.GetAsync($"/api/incidents/{incident.Id}/assignment");
        var assignment = await forIncident.Content.ReadFromJsonAsync<AssignmentDto>(Json);
        var byId = await client.GetFromJsonAsync<AssignmentDto>($"/api/assignments/{assignment!.Id}", Json);
        var byHero = await client.GetFromJsonAsync<AssignmentDto[]>($"/api/assignments?heroId={hero.Id}", Json);
        var open = await client.GetFromJsonAsync<AssignmentDto[]>("/api/assignments?open=true", Json);

        Assert.Equal(hero.Id, Assert.Single(byId!.Heroes).Id);
        Assert.Single(byId.Reports);
        Assert.Single(byHero!);
        Assert.Single(open!);
        Assert.Null(byId.ClosedAt);
    }

    [Fact]
    public async Task Assignments_filter_open_false_returns_closed_work()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);
        await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        var closed = await client.GetFromJsonAsync<AssignmentDto[]>("/api/assignments?open=false", Json);
        var open = await client.GetFromJsonAsync<AssignmentDto[]>("/api/assignments?open=true", Json);

        var assignment = Assert.Single(closed!);
        Assert.NotNull(assignment.ClosedAt);
        Assert.Empty(open!);
    }

    // ---- Reports -----------------------------------------------------------

    [Fact]
    public async Task Report_on_an_unknown_incident_returns_404()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await FileReport(client, Guid.NewGuid())).StatusCode);
    }

    [Fact]
    public async Task Report_from_the_wrong_city_returns_400()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client, "Redmond");

        Assert.Equal(HttpStatusCode.BadRequest, (await FileReport(client, incident.Id, city: "Seattle")).StatusCode);
    }

    [Fact]
    public async Task Report_with_negative_cost_returns_400()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var incident = await FileIncident(client);

        Assert.Equal(HttpStatusCode.BadRequest, (await FileReport(client, incident.Id, cost: -5)).StatusCode);
    }

    [Fact]
    public async Task Reports_are_accepted_while_opened_and_listed_for_the_incident()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var incident = await FileIncident(client);   // no hero: stays Opened

        var created = await FileReport(client, incident.Id);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var listed = await client.GetFromJsonAsync<ReportDto[]>($"/api/incidents/{incident.Id}/reports", Json);
        Assert.Single(listed!);
    }

    [Fact]
    public async Task Report_filed_while_opened_is_attached_when_a_hero_arrives()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var incident = await FileIncident(client);
        var created = await (await FileReport(client, incident.Id)).Content.ReadFromJsonAsync<ReportDto>(Json);

        var hero = await RegisterHero(client, "Late Arrival");

        var detail = await GetIncident(client, incident.Id);
        Assert.Equal(IncidentStatus.InProgress, detail.Incident.Status);
        Assert.Equal(created!.Id, Assert.Single(detail.Assignment!.Reports).Id);
        Assert.Equal(hero.Id, Assert.Single(detail.Assignment.Heroes).Id);
    }

    [Fact]
    public async Task Filed_report_can_be_fetched_by_id_and_listed_for_its_incident()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);

        var created = await FileReport(client, incident.Id);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var report = await created.Content.ReadFromJsonAsync<ReportDto>(Json);

        var fetched = await client.GetFromJsonAsync<ReportDto>($"/api/reports/{report!.Id}", Json);
        var listed = await client.GetFromJsonAsync<ReportDto[]>($"/api/incidents/{incident.Id}/reports", Json);

        Assert.Equal(report.Id, fetched!.Id);
        Assert.Equal(report.Id, Assert.Single(listed!).Id);
        Assert.Equal(ReportSeverity.Minor, fetched.Severity);
    }

    [Fact]
    public async Task Unknown_report_returns_404()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/reports/{Guid.NewGuid()}")).StatusCode);
    }

    // ---- Closing and assessments -------------------------------------------

    [Fact]
    public async Task Close_incident_assesses_hero_and_frees_them()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var hero = await RegisterHero(client, "Captain Inconvenient", reputation: 1);
        var incident = await FileIncident(client);
        await FileReport(client, incident.Id);

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
    public async Task Close_rejects_an_opened_incident_with_409_and_a_code()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        var opened = await FileIncident(client);      // no heroes: Opened

        var response = await client.PostAsync($"/api/incidents/{opened.Id}/close", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("incident.state", await ProblemCode(response));
    }

    [Fact]
    public async Task Closing_twice_returns_409_with_incident_closed_code()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);
        await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        var again = await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("incident.closed", await ProblemCode(again));
    }

    [Fact]
    public async Task Close_unknown_incident_returns_404()
    {
        using var host = NewHost();
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/incidents/{Guid.NewGuid()}/close", null)).StatusCode);
    }

    [Fact]
    public async Task Closed_incident_rejects_new_reports_with_409()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        await RegisterHero(client, "Captain Inconvenient");
        var incident = await FileIncident(client);
        await client.PostAsync($"/api/incidents/{incident.Id}/close", null);

        var response = await FileReport(client, incident.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("incident.closed", await ProblemCode(response));
    }

    // ---- Concurrency ------------------------------------------------------

    [Fact]
    public async Task Parallel_requests_never_double_book_a_hero()
    {
        using var host = NewHost();
        var client = host.CreateClient();
        const int heroCount = 10;
        const int incidentCount = 15;

        var work = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < heroCount; i++)
            work.Add(client.PostAsJsonAsync("/api/heroes", new RegisterHeroRequest($"Hero {i}", "x"), Json));
        for (int i = 0; i < incidentCount; i++)
            work.Add(client.PostAsJsonAsync("/api/incidents",
                new FileIncidentRequest("Redmond", Noon.AddMinutes(i), "Brawl", IncidentCategory.PettyCrime), Json));
        var responses = await Task.WhenAll(work);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        var heroes = await client.GetFromJsonAsync<HeroDto[]>("/api/heroes?available=false", Json);
        var incidents = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents?status=InProgress", Json);
        var waiting = await client.GetFromJsonAsync<IncidentDto[]>("/api/incidents?status=Opened", Json);

        Assert.Equal(heroCount, heroes!.Length);                       // every hero busy
        Assert.Equal(heroCount, incidents!.Length);                    // one hero per in-progress incident
        Assert.Equal(incidentCount - heroCount, waiting!.Length);      // the rest wait
    }

    // ---- Helpers -----------------------------------------------------------

    private static StringContent JsonContent(string body) =>
        new(body, System.Text.Encoding.UTF8, "application/json");
}
