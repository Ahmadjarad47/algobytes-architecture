using System.Text.Json;

namespace algo.Application.Features.Roles.Commands.UpdateRole;

public sealed record UpdateRoleRequest(string Name, JsonElement? CustomFields);
