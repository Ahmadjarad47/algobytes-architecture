namespace algo.Application.Features.Users.Commands.LockUser;

public sealed record LockUserRequest(DateTimeOffset LockoutEnd);
