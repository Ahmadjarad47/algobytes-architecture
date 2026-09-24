using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using algo.API.IntegrationTests.Support;
using algo.Application.Common.Identity;

namespace algo.API.IntegrationTests;

public sealed class CustomFieldDefinitionsWriteApiTests : IClassFixture<TestApiFactory>
{
    private const string Endpoint = $"{ApiTestClient.ApiV1}/custom-field-definitions";
    private readonly TestApiFactory factory;

    public CustomFieldDefinitionsWriteApiTests(TestApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateUpdateDelete_RoundTrip_SucceedsForAdmin()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = await ApiTestClient.CreateAuthenticatedClientAsync(
            factory,
            DefaultAdmin.Email,
            DefaultAdmin.Password);

        var key = $"dept_{Guid.NewGuid():N}"[..12];

        var createResponse = await client.PostAsJsonAsync(Endpoint, new
        {
            entity = "users",
            key,
            label = "Department",
            type = "text",
            required = false,
            searchable = true,
            filterable = true,
            sortable = false,
            visibleInTable = true,
            visibleInForm = true,
            visibleInDetails = true,
            options = (string[]?)null,
            defaultValue = (string?)null,
            validation = (object?)null,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonObject>();
        var id = created?["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal(key, created?["key"]?.GetValue<string>());
        Assert.Equal("Department", created?["label"]?.GetValue<string>());
        Assert.Equal("text", created?["type"]?.GetValue<string>());

        var listAfterCreate = await client.GetFromJsonAsync<JsonArray>($"{Endpoint}?entity=users");
        Assert.Contains(listAfterCreate!, node => node?["id"]?.GetValue<string>() == id);

        var updateResponse = await client.PutAsJsonAsync($"{Endpoint}/{id}", new
        {
            label = "Department Name",
            type = "text",
            required = true,
            searchable = true,
            filterable = false,
            sortable = true,
            visibleInTable = false,
            visibleInForm = true,
            visibleInDetails = false,
            options = (string[]?)null,
            defaultValue = "General",
            validation = new { minLength = 2 },
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Department Name", updated?["label"]?.GetValue<string>());
        Assert.True(updated?["required"]?.GetValue<bool>());
        Assert.False(updated?["filterable"]?.GetValue<bool>());
        Assert.True(updated?["sortable"]?.GetValue<bool>());
        Assert.False(updated?["visibleInTable"]?.GetValue<bool>());
        Assert.Equal("General", updated?["defaultValue"]?.GetValue<string>());

        var deleteResponse = await client.DeleteAsync($"{Endpoint}/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listAfterDelete = await client.GetFromJsonAsync<JsonArray>($"{Endpoint}?entity=users");
        Assert.DoesNotContain(listAfterDelete!, node => node?["id"]?.GetValue<string>() == id);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenDefinitionMissing()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = await ApiTestClient.CreateAuthenticatedClientAsync(
            factory,
            DefaultAdmin.Email,
            DefaultAdmin.Password);

        var response = await client.PutAsJsonAsync($"{Endpoint}/{Guid.NewGuid()}", ValidUpdateBody("Missing Field"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenDefinitionMissing()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = await ApiTestClient.CreateAuthenticatedClientAsync(
            factory,
            DefaultAdmin.Email,
            DefaultAdmin.Password);

        var response = await client.DeleteAsync($"{Endpoint}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsValidationProblem_WhenDuplicateKey()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = await ApiTestClient.CreateAuthenticatedClientAsync(
            factory,
            DefaultAdmin.Email,
            DefaultAdmin.Password);

        var key = $"dup_{Guid.NewGuid():N}"[..10];
        var payload = new
        {
            entity = "roles",
            key,
            label = "First Label",
            type = "text",
            required = false,
            searchable = false,
            filterable = false,
            sortable = false,
            visibleInTable = true,
            visibleInForm = true,
            visibleInDetails = true,
            options = (string[]?)null,
            defaultValue = (string?)null,
            validation = (object?)null,
        };

        var firstResponse = await client.PostAsJsonAsync(Endpoint, payload);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync(Endpoint, payload with { label = "Second Label" });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.Equal("application/problem+json", duplicateResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Update_ReturnsValidationProblem_WhenLabelEmpty()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = await ApiTestClient.CreateAuthenticatedClientAsync(
            factory,
            DefaultAdmin.Email,
            DefaultAdmin.Password);

        var key = $"lbl_{Guid.NewGuid():N}"[..10];
        var createResponse = await client.PostAsJsonAsync(Endpoint, new
        {
            entity = "accessPolicies",
            key,
            label = "Policy Field",
            type = "boolean",
            required = false,
            searchable = false,
            filterable = false,
            sortable = false,
            visibleInTable = true,
            visibleInForm = true,
            visibleInDetails = true,
            options = (string[]?)null,
            defaultValue = (string?)null,
            validation = (object?)null,
        });
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<JsonObject>();
        var id = created?["id"]?.GetValue<string>();

        var updateResponse = await client.PutAsJsonAsync($"{Endpoint}/{id}", ValidUpdateBody("   "));
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        Assert.Equal("application/problem+json", updateResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task WriteEndpoints_ReturnUnauthorized_WhenNotAuthenticated()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(Endpoint, new
        {
            entity = "users",
            key = "unauth",
            label = "Unauth",
            type = "text",
            required = false,
            searchable = false,
            filterable = false,
            sortable = false,
            visibleInTable = true,
            visibleInForm = true,
            visibleInDetails = true,
            options = (string[]?)null,
            defaultValue = (string?)null,
            validation = (object?)null,
        });

        var updateResponse = await client.PutAsJsonAsync($"{Endpoint}/{Guid.NewGuid()}", ValidUpdateBody("Unauth"));
        var deleteResponse = await client.DeleteAsync($"{Endpoint}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    private static object ValidUpdateBody(string label) => new
    {
        label,
        type = "text",
        required = false,
        searchable = false,
        filterable = false,
        sortable = false,
        visibleInTable = true,
        visibleInForm = true,
        visibleInDetails = true,
        options = (string[]?)null,
        defaultValue = (string?)null,
        validation = (object?)null,
    };
}
