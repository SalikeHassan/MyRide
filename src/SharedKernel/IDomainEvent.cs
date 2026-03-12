namespace SharedKernel;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string TenantId { get; }
}
