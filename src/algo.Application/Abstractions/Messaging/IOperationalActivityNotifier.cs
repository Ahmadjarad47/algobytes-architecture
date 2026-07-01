namespace algo.Application.Abstractions.Messaging;

public interface IOperationalActivityNotifier
{
    Task NotifyAsync(OperationalActivityEvent activity, CancellationToken cancellationToken = default);
}

