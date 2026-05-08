using System.Diagnostics;
using algo.Application.Common.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace algo.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        logger.LogInformation("MediatR handling started {MediatRRequest}", requestName);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "MediatR request summary (masked, shallow) {MediatRRequest}: {@MaskedProperties}",
                requestName,
                SensitiveDataMasker.ToMaskedPropertyBag(request));
        }

        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation(
                "MediatR handling completed {MediatRRequest} in {ElapsedMs} ms",
                requestName,
                sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(
                ex,
                "MediatR handling failed {MediatRRequest} after {ElapsedMs} ms",
                requestName,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}
