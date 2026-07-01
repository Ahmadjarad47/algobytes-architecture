using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using algo.API.IntegrationTests.Support;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Identity;

namespace algo.API.IntegrationTests;

public sealed class AuthorizationBehaviorTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory factory;

    public AuthorizationBehaviorTests(TestApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task UserWithOnlyLogsRead_IsForbiddenForOtherAdminResources()
    {
        await factory.EnsureLogsReaderAsync();
        using var client = factory.CreateClient();
        var token = await ApiTestClient.LoginAsync(client, "logs.reader@algo.bytes", "Reader@123456");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var logsResponse = await client.GetAsync($"{ApiTestClient.ApiV1}/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var listAccessPoliciesResponse = await client.GetAsync($"{ApiTestClient.ApiV1}/AccessPolicies");
        Assert.Equal(HttpStatusCode.Forbidden, listAccessPoliciesResponse.StatusCode);

        var createAccessPolicyResponse = await client.PostAsJsonAsync($"{ApiTestClient.ApiV1}/AccessPolicies", new
        {
            resource = AccessPolicyResources.Logs,
            action = AccessPolicyActions.Read,
            effect = "allow",
            subjectType = "role",
            subjectKey = "LogsReader",
            conditionJson = (string?)null,
            priority = 10,
            isEnabled = true,
            description = "test",
            validFrom = (DateTime?)null,
            validTo = (DateTime?)null,
        });
        Assert.Equal(HttpStatusCode.Forbidden, createAccessPolicyResponse.StatusCode);

        var rolesResponse = await client.GetAsync($"{ApiTestClient.ApiV1}/roles");
        Assert.Equal(HttpStatusCode.Forbidden, rolesResponse.StatusCode);

        var usersResponse = await client.GetAsync($"{ApiTestClient.ApiV1}/users");
        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
    }

    [Fact]
    public async Task AdminWithWildcard_IsAllowedForAdminAndBusinessResources()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = factory.CreateClient();
        var token = await ApiTestClient.LoginAsync(client, DefaultAdmin.Email, DefaultAdmin.Password);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var accessPoliciesResponse = await client.GetAsync($"{ApiTestClient.ApiV1}/AccessPolicies");
        Assert.Equal(HttpStatusCode.OK, accessPoliciesResponse.StatusCode);

        var createAccessPolicyResponse = await client.PostAsJsonAsync($"{ApiTestClient.ApiV1}/AccessPolicies", new
        {
            resource = AccessPolicyResources.Logs,
            action = AccessPolicyActions.Read,
            effect = "allow",
            subjectType = "role",
            subjectKey = "Admin",
            conditionJson = (string?)null,
            priority = 42,
            isEnabled = true,
            description = "admin test policy",
            validFrom = (DateTime?)null,
            validTo = (DateTime?)null,
        });
        Assert.Equal(HttpStatusCode.OK, createAccessPolicyResponse.StatusCode);

        var usersResponse = await client.GetAsync($"{ApiTestClient.ApiV1}/users");
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);

        var logsResponse = await client.GetAsync($"{ApiTestClient.ApiV1}/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);
    }

    [Fact]
    public async Task LoginRateLimit_ReturnsProblemDetails()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.77");

        HttpResponseMessage response = null!;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            response = await client.PostAsJsonAsync($"{ApiTestClient.ApiV1}/auth/login", new
            {
                email = "missing.user@algo.bytes",
                password = "WrongPassword123!",
            });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(429, body?["status"]?.GetValue<int>());
        Assert.Equal("Too many requests.", body?["title"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(body?["traceId"]?.GetValue<string>()));
    }
}
