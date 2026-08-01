using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExpenseTracker.Api.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ExpenseTracker.Tests.Integration;

/// <summary>
/// Integration tests using WebApplicationFactory which spins up the full ASP.NET Core
/// pipeline in-process. Each test class gets an isolated application instance so that
/// the in-memory store starts empty (no shared state across test classes).
/// </summary>
public class ExpensesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ExpensesIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Set ASPNETCORE_ENVIRONMENT = "Development" so Swagger middleware registers
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        }).CreateClient();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateExpenseRequest ValidRequest(
        string title = "Lunch",
        decimal amount = 12.50m,
        string category = "Food",
        DateTime? date = null) =>
        new()
        {
            Title = title,
            Amount = amount,
            Category = category,
            Date = date ?? DateTime.UtcNow
        };

    private async Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/api/expenses", request ?? ValidRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        return body!;
    }

    // ─── POST /api/expenses ───────────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidExpense_Returns201WithLocationHeaderAndBody()
    {
        var request = ValidRequest("Dinner", 25m, "Food");

        var response = await _client.PostAsJsonAsync("/api/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().NotBe(Guid.Empty);
        body.Title.Should().Be("Dinner");
        body.Amount.Should().Be(25m);
        body.Category.Should().Be("Food");
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400WithValidationError()
    {
        var request = ValidRequest(title: "");

        var response = await _client.PostAsJsonAsync("/api/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Title");
    }

    [Fact]
    public async Task Post_NegativeAmount_Returns400WithValidationError()
    {
        var request = ValidRequest(amount: -5m);

        var response = await _client.PostAsJsonAsync("/api/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Amount");
    }

    [Fact]
    public async Task Post_ZeroAmount_Returns400WithValidationError()
    {
        var request = ValidRequest(amount: 0m);

        var response = await _client.PostAsJsonAsync("/api/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyCategory_Returns400WithValidationError()
    {
        var request = ValidRequest(category: "");

        var response = await _client.PostAsJsonAsync("/api/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_DefaultDate_Returns400WithValidationError()
    {
        // Send a raw JSON body with an explicit year-0001 date (DateTime.MinValue serialized).
        // Using ValidRequest(date: default) would pass null, falling back to UtcNow via null-coalescing.
        // Raw JSON ensures the server receives an out-of-range year and the validator rejects it.
        var json = """
            {
              "title": "Test",
              "amount": 10.00,
              "category": "Food",
              "date": "0001-01-01T00:00:00"
            }
            """;
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/expenses", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── GET /api/expenses ────────────────────────────────────────────────────

    [Fact]
    public async Task Get_All_ReturnsListIncludingCreatedExpense()
    {
        await CreateExpenseAsync(ValidRequest("Coffee", 5m, "Drinks"));

        var response = await _client.GetAsync("/api/expenses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Should().Contain(e => e.Title == "Coffee");
    }

    [Fact]
    public async Task Get_All_ReturnsEmptyArray_WhenNoExpenses()
    {
        // Use a fresh factory instance to guarantee empty store
        await using var localFactory = new WebApplicationFactory<Program>();
        var localClient = localFactory.CreateClient();

        var response = await localClient.GetAsync("/api/expenses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    // ─── GET /api/expenses?category={category} ────────────────────────────────

    [Fact]
    public async Task Get_FilterByCategory_ReturnsMatchingExpenses()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Create two categories
        await client.PostAsJsonAsync("/api/expenses", ValidRequest("Taxi", 15m, "Transport"));
        await client.PostAsJsonAsync("/api/expenses", ValidRequest("Pizza", 18m, "Food"));

        var response = await client.GetAsync("/api/expenses?category=Transport");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>(JsonOptions);
        body.Should().HaveCount(1)
            .And.OnlyContain(e => e.Category == "Transport");
    }

    [Fact]
    public async Task Get_FilterByCategory_CaseInsensitive()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/expenses", ValidRequest("Sushi", 30m, "Food"));

        // Query with uppercase "FOOD"
        var response = await client.GetAsync("/api/expenses?category=FOOD");
        var body = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>(JsonOptions);

        body.Should().HaveCount(1)
            .And.OnlyContain(e => e.Category.Equals("Food", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Get_FilterByCategory_NoMatch_ReturnsEmptyArray()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/expenses", ValidRequest(category: "Food"));

        var response = await client.GetAsync("/api/expenses?category=Nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>(JsonOptions);
        body.Should().BeEmpty();
    }

    // ─── GET /api/expenses/total ──────────────────────────────────────────────

    [Fact]
    public async Task Get_Total_ReturnsCorrectSum()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 10m, category: "Food"));
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 20m, category: "Transport"));

        var response = await client.GetAsync("/api/expenses/total");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TotalResponse>(JsonOptions);
        body!.Total.Should().Be(30m);
        body.Category.Should().BeNull();
    }

    [Fact]
    public async Task Get_TotalByCategory_ReturnsCorrectCategorySum()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 10m, category: "Food"));
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 5m, category: "Food"));
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 20m, category: "Transport"));

        var response = await client.GetAsync("/api/expenses/total?category=Food");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TotalResponse>(JsonOptions);
        body!.Total.Should().Be(15m);
        body.Category.Should().Be("Food");
    }

    // ─── GET /api/expenses/totals-by-category ─────────────────────────────────

    [Fact]
    public async Task Get_TotalsByCategory_ReturnsDictionaryOfCategoryTotals()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 10m, category: "Food"));
        await client.PostAsJsonAsync("/api/expenses", ValidRequest(amount: 20m, category: "Transport"));

        var response = await client.GetAsync("/api/expenses/totals-by-category");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, decimal>>(JsonOptions);
        body.Should().NotBeNull();
        body!["Food"].Should().Be(10m);
        body["Transport"].Should().Be(20m);
    }

    // ─── DELETE /api/expenses/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingExpense_Returns204()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/expenses/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AfterDeletion_ExpenseNoLongerInList()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/expenses", ValidRequest("ToDelete", 10m, "Food"));
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        await client.DeleteAsync($"/api/expenses/{created!.Id}");

        var listResponse = await client.GetAsync("/api/expenses");
        var body = await listResponse.Content.ReadFromJsonAsync<List<ExpenseResponse>>(JsonOptions);
        body.Should().NotContain(e => e.Id == created.Id);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/expenses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_InvalidGuidFormat_Returns404Or400()
    {
        var response = await _client.DeleteAsync("/api/expenses/not-a-valid-guid");

        // ASP.NET Core route constraint {id:guid} returns 404 for non-GUID segments
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
