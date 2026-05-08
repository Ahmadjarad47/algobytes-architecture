using algo.Application.Features.Auth.Dtos;
using algo.Domain.Identity.Entities;
using Mapster;

namespace algo.Application.Features.Auth.Mapping;

public sealed class AuthMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationUser, UserDto>()
            .Map(d => d.UserId, s => s.Id);
    }
}
