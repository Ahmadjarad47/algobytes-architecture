using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.SetAccessPolicyEnabled;

public sealed record SetAccessPolicyEnabledCommand(Guid Id, bool IsEnabled) : IRequest<AccessPolicyAdminDto?>;
