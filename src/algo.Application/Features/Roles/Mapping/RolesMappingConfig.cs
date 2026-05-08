using algo.Application.Features.Roles.Dtos;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace algo.Application.Features.Roles.Mapping;

public sealed class RolesMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IdentityRole, RoleDto>()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.Name, s => s.Name!)
            .Map(d => d.NormalizedName, s => s.NormalizedName);
    }
}
