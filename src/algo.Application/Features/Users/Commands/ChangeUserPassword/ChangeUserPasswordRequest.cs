namespace algo.Application.Features.Users.Commands.ChangeUserPassword;

public sealed record ChangeUserPasswordRequest(string NewPassword, string ConfirmPassword);
