using algo.SharedKernel.Abstractions;

namespace algo.SharedKernel.BaseClasses;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private uint _version;
    public uint Version => _version;

    public new void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        base.RaiseDomainEvent(domainEvent);
        _version++;
    }
}
