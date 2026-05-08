using System.Security.Claims;
using algo.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace algo.Infrastructure.Security;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
