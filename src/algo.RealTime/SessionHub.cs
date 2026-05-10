using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace algo.RealTime;

[Authorize]
public sealed class SessionHub(
    UserPresenceTracker presenceTracker,
    IOperationalActivityNotifier operationalActivityNotifier) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue("sub");
        var sessionId = Context.User?.FindFirstValue("sid");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PresenceGroupNames.User(userId));

            if (await presenceTracker.SetConnectedAsync(userId))
            {
                await Clients.All.SendAsync("userPresenceChanged", new { userId, isOnline = true });
                await operationalActivityNotifier.NotifyAsync(new OperationalActivityEvent(
                    DateTimeOffset.UtcNow,
                    "info",
                    "websocket",
                    "User connected to live operations channel.",
                    UserId: userId));
            }

            await Clients.Caller.SendAsync("presenceSnapshot", new { onlineUserIds = await presenceTracker.OnlineUserIdsAsync() });
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
        if (!string.IsNullOrWhiteSpace(userId) && await presenceTracker.SetDisconnectedAsync(userId))
        {
            await Clients.All.SendAsync("userPresenceChanged", new { userId, isOnline = false });
            await operationalActivityNotifier.NotifyAsync(new OperationalActivityEvent(
                DateTimeOffset.UtcNow,
                "warn",
                "websocket",
                "User disconnected from live operations channel.",
                UserId: userId));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
