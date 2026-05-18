using algo.Application.Features.Users.Dtos;
using algo.Application.Common.CustomFields;
using algo.Domain.Identity.Entities;
using Mapster;

namespace algo.Application.Features.Users.Mapping;

public sealed class UsersMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationUser, UserDetailsDto>()
            .Map(d => d.UserId, s => s.Id)
            .Map(d => d.IsLocked, s => s.LockoutEnd.HasValue && s.LockoutEnd > DateTimeOffset.UtcNow)
            .Map(d => d.CustomFields, s => JsonDocumentHelpers.CloneToElement(s.CustomFields))
            .Ignore(d => d.Roles);

        config.NewConfig<ApplicationUser, UserListItemDto>()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.IsLocked, s => s.LockoutEnd.HasValue && s.LockoutEnd > DateTimeOffset.UtcNow)
            .Map(d => d.CustomFields, s => JsonDocumentHelpers.CloneToElement(s.CustomFields))
            .Ignore(d => d.Roles);
    }
}
