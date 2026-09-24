namespace algo.SharedKernel.Abstractions;

/// <summary>
/// Dispatches domain events collected from aggregate roots.
/// Implementations live in Infrastructure; this contract belongs in SharedKernel so
/// any layer can reference it without introducing an infrastructure dependency.
/// </summary>
public interface IEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
