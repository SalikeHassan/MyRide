using Common.Application;
using Drivers.Application.Queries;
using Drivers.Domain.Entities;
using Drivers.Domain.Ports;

namespace Drivers.Application.Handlers;

public class GetAvailableDriverHandler : IQueryHandler<GetAvailableDriverQuery, Driver?>
{
    private readonly IDriverRepository driverRepository;

    public GetAvailableDriverHandler(IDriverRepository driverRepository)
    {
        this.driverRepository = driverRepository;
    }

    public async Task<Driver?> Handle(GetAvailableDriverQuery query)
    {
        return await driverRepository.GetAvailable(query.TenantId);
    }
}
