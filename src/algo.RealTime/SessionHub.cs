using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace algo.RealTime;

[Authorize]
public sealed class SessionHub(UserPresenceTracker presenceTracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue("sub");
        var sessionId = Context.User?.FindFirstValue("sid");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PresenceGroupNames.User(userId));

            if (presenceTracker.SetConnected(userId))
            {
                await Clients.All.SendAsync("userPresenceChanged", new { userId, isOnline = true });
            }

            await Clients.Caller.SendAsync("presenceSnapshot", new { onlineUserIds = presenceTracker.OnlineUserIds() });
        }

        if (Guid.TryParse(sessionId, out var parsedSessionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PresenceGroupNames.Session(parsedSessionId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(userId) && presenceTracker.SetDisconnected(userId))
        {
            await Clients.All.SendAsync("userPresenceChanged", new { userId, isOnline = false });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
