using algo.Application.Abstractions;
using algo.RealTime.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace algo.RealTime.Services;

public sealed class SignalROperationalActivityNotifier(IHubContext<SessionHub> hubContext) : IOperationalActivityNotifier
{
    public Task NotifyAsync(OperationalActivityEvent activity, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.All.SendAsync("operationalActivity", activity, cancellationToken);
    }
}
