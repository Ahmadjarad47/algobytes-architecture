namespace algo.Application.Features.AccessPolicies.Dtos;

public sealed record ValidateAccessPolicyConditionResultDto(bool IsValid, string? ErrorMessage);
