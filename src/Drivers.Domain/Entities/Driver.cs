namespace Drivers.Domain.Entities;

public class Driver
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public DriverStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Driver() { }

    public static Driver Create(Guid id, string tenantId, string name, string phone)
    {
        return new Driver
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Phone = phone,
            Status = DriverStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Driver Seed(Guid id, string tenantId, string name, string phone, DateTime createdAt)
    {
        return new Driver
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Phone = phone,
            Status = DriverStatus.Available,
            CreatedAt = createdAt
        };
    }

    public void MakeAvailable()
    {
        Status = DriverStatus.Available;
    }

    public void MakeInProgress()
    {
        if (Status == DriverStatus.InProgress)
        {
            throw new InvalidOperationException($"Driver {Id} is already in progress.");
        }

        Status = DriverStatus.InProgress;
    }
}
