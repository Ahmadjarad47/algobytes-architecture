namespace algo.RealTime;

public interface IRealtimeChatStore
{
    Task<IReadOnlyList<ChatMessage>> GetConversationAsync(string userAId, string userBId, CancellationToken cancellationToken = default);
    Task<ChatMessage> AppendDirectMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<ChatReadReceipt> MarkConversationAsReadAsync(string readerUserId, string counterpartUserId, CancellationToken cancellationToken = default);
}
