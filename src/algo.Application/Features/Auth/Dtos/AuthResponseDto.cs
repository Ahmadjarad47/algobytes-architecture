namespace algo.Application.Features.Auth.Dtos;

public sealed record AuthResponseDto(UserDto User, TokenDto Tokens);
