namespace SharedKernel;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> domainEvents = new();

    public Guid Id { get; protected set; }
    public string TenantId { get; protected set; } = string.Empty;
    public int Version { get; private set; } = 0;

    public IReadOnlyList<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        domainEvents.Add(domainEvent);
        Apply(domainEvent);
        Version++;
    }

    protected abstract void Apply(IDomainEvent domainEvent);

    public void Rehydrate(IEnumerable<IDomainEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
            Version++;
        }
    }

    public void ClearDomainEvents() => domainEvents.Clear();
}
