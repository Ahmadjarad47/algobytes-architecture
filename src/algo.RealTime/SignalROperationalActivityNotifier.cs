using algo.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace algo.RealTime;

public sealed class SignalROperationalActivityNotifier(IHubContext<SessionHub> hubContext) : IOperationalActivityNotifier
{
    public Task NotifyAsync(OperationalActivityEvent activity, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.All.SendAsync("operationalActivity", activity, cancellationToken);
    }
}
