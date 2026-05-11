using System.Security.Claims;
using algo.Application.Common.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace algo.RealTime;

[Authorize]
public sealed class SessionHub(
    UserPresenceTracker presenceTracker,
    IOperationalActivityNotifier operationalActivityNotifier,
    IRealtimeChatStore chatStore) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = ResolveUserId();
        var sessionId = ResolveSessionId();
        var displayName = ResolveDisplayName();
        var isAdmin = Context.User?.IsInRole(DefaultRoles.Admin) ?? false;

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
        var userId = ResolveUserId();
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
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Clients.Others.SendAsync("chatTyping", new ChatTypingEvent(
                userId,
                ResolveDisplayName(),
                Context.User?.IsInRole(DefaultRoles.Admin) ?? false,
                false,
                DateTimeOffset.UtcNow));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task LoadDirectChatHistory(string targetUserId)
    {
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            throw new HubException("User identity is missing.");
        }

        var receipt = await chatStore.MarkConversationAsReadAsync(userId, targetUserId);
        var messages = await chatStore.GetConversationAsync(userId, targetUserId);
        await Clients.Caller.SendAsync("directChatHistory", new { targetUserId, messages });
        if (receipt.MessageIds.Count > 0)
        {
            await Clients.Group(PresenceGroupNames.User(userId)).SendAsync("directChatRead", receipt);
            await Clients.Group(PresenceGroupNames.User(targetUserId)).SendAsync("directChatRead", receipt);
        }
    }

    public async Task SendDirectMessage(string targetUserId, string content, string? replyToMessageId = null)
    {
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            throw new HubException("User identity is missing.");
        }

        var trimmedContent = (content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            throw new HubException("Message cannot be empty.");
        }

        if (trimmedContent.Length > 2000)
        {
            throw new HubException("Message is too long.");
        }

        var message = new ChatMessage(
            Guid.NewGuid().ToString("N"),
            userId,
            targetUserId.Trim(),
            ResolveDisplayName(),
            Context.User?.IsInRole(DefaultRoles.Admin) ?? false,
            trimmedContent,
            DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(replyToMessageId) ? null : replyToMessageId.Trim(),
            IsRead: false,
            ReadAtUtc: null);

        var persisted = await chatStore.AppendDirectMessageAsync(message);
        await Clients.Group(PresenceGroupNames.User(userId)).SendAsync("directChatMessage", persisted);
        await Clients.Group(PresenceGroupNames.User(targetUserId.Trim())).SendAsync("directChatMessage", persisted);
    }

    public async Task SetDirectTyping(string targetUserId, bool isTyping)
    {
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            return;
        }

        await Clients.Group(PresenceGroupNames.User(targetUserId.Trim())).SendAsync("directChatTyping", new
        {
            targetUserId = userId,
            eventData = new ChatTypingEvent(
            userId,
            ResolveDisplayName(),
            Context.User?.IsInRole(DefaultRoles.Admin) ?? false,
            isTyping,
            DateTimeOffset.UtcNow)
        });
    }

    public async Task MarkDirectConversationRead(string targetUserId)
    {
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            return;
        }

        var receipt = await chatStore.MarkConversationAsReadAsync(userId, targetUserId);
        if (receipt.MessageIds.Count == 0)
        {
            return;
        }

        await Clients.Group(PresenceGroupNames.User(userId)).SendAsync("directChatRead", receipt);
        await Clients.Group(PresenceGroupNames.User(targetUserId)).SendAsync("directChatRead", receipt);
    }

    private string ResolveDisplayName()
    {
        return Context.User?.FindFirstValue("display_name")
            ?? Context.User?.FindFirstValue("name")
            ?? Context.User?.Identity?.Name
            ?? ResolveUserId()
            ?? "Unknown user";
    }

    private string? ResolveUserId()
    {
        return Context.User?.FindFirstValue("sub")
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.Identity?.Name;
    }

    private string? ResolveSessionId()
    {
        return Context.User?.FindFirstValue("sid")
            ?? Context.User?.FindFirstValue(ClaimTypes.Sid);
    }
}
