using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using algo.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace algo.Infrastructure.Security;

public sealed class AccessPolicyTokenResolver(IHttpContextAccessor httpContextAccessor) : IAccessPolicyTokenResolver
{
    private static readonly string[] RoleClaimTypes =
    [
        ClaimTypes.Role,
        "role",
        "roles",
    ];

    public string? CurrentUserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public IReadOnlyList<string> CurrentRoleNames
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return [];
            }

            return user.Claims
                .Where(claim => RoleClaimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase))
                .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public object? ResolveTokenValue(string token) => token switch
    {
        "@CurrentUserId" => CurrentUserId,
        "@CurrentRoleNames" => CurrentRoleNames,
        _ => null,
    };
}
