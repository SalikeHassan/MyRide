using Drivers.Application.Queries;
using Drivers.Domain.Entities;
using Drivers.Domain.Ports;

namespace Drivers.Application.Handlers;

public class GetAvailableDriverHandler
{
    private readonly IDriverRepository driverRepository;

    public GetAvailableDriverHandler(IDriverRepository driverRepository)
    {
        this.driverRepository = driverRepository;
    }

    public async Task<Driver?> HandleAsync(GetAvailableDriverQuery query)
    {
        return await driverRepository.GetAvailableAsync(query.TenantId);
    }
}
