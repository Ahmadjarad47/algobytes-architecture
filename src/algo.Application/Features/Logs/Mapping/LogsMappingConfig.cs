using algo.Application.Features.Logs.Dtos;
using algo.Domain.Logging.Entities;
using Mapster;

namespace algo.Application.Features.Logs.Mapping;

public sealed class LogsMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationLog, ApplicationLogDto>();
    }
}
