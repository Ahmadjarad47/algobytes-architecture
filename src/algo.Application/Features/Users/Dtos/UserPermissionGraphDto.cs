namespace algo.Application.Features.Users.Dtos;

public sealed record UserPermissionGraphDto(
    string UserId,
    IReadOnlyList<UserPermissionGraphNodeDto> Nodes,
    IReadOnlyList<UserPermissionGraphEdgeDto> Edges);

public sealed record UserPermissionGraphNodeDto(
    string Id,
    string Type,
    string Label,
    string? Resource = null,
    string? Action = null,
    string? Effect = null,
    string? ConditionJson = null,
    int? Priority = null,
    bool? IsEnabled = null);

public sealed record UserPermissionGraphEdgeDto(
    string From,
    string To,
    string Type);
