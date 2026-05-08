using algo.Application.Features.ErrorLogs.Dtos;
using algo.Domain.Logging.Entities;
using Mapster;

namespace algo.Application.Features.ErrorLogs.Mapping;

public sealed class ErrorLogsMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ErrorLog, ErrorLogDto>();
    }
}
