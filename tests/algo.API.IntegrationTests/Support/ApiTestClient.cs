using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace algo.API.IntegrationTests.Support;

internal static class ApiTestClient
{
    public const string ApiV1 = "/api/v1";

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync($"{ApiV1}/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        var token = body?["tokens"]?["accessToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        TestApiFactory factory,
        string email,
        string password)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
