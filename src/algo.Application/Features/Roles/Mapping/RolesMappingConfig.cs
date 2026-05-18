using algo.Application.Features.Roles.Dtos;
using algo.Application.Common.CustomFields;
using algo.Domain.Identity.Entities;
using Mapster;

namespace algo.Application.Features.Roles.Mapping;

public sealed class RolesMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationRole, RoleDto>()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.Name, s => s.Name!)
            .Map(d => d.NormalizedName, s => s.NormalizedName)
            .Map(d => d.TrashedAt, s => s.TrashedAt)
            .Map(d => d.TrashExpiresAt, s => s.TrashExpiresAt)
            .Map(d => d.DeletedAt, s => s.DeletedAt)
            .Map(d => d.CustomFields, s => JsonDocumentHelpers.CloneToElement(s.CustomFields));
    }
}
