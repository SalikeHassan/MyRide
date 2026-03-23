using Microsoft.EntityFrameworkCore;
using MyRide.Application.Ports;
using MyRide.Domain.Sagas;

namespace MyRide.Infrastructure.Persistence;

public class SqlRequestRideSagaRepository : IRequestRideSagaRepository
{
    private readonly OrchestratorDbContext context;

    public SqlRequestRideSagaRepository(OrchestratorDbContext context)
    {
        this.context = context;
    }

    public async Task Save(RequestRideSagaState saga)
    {
        var existing = await context.RequestRideSagas.FindAsync(saga.SagaId);

        if (existing is null)
        {
            context.RequestRideSagas.Add(saga);
        }

        await context.SaveChangesAsync();
    }

    public Task<List<RequestRideSagaState>> GetStuck()
    {
        return context.RequestRideSagas
            .Where(s => s.Status == RequestRideSagaStatus.Compensating
                     || s.Status == RequestRideSagaStatus.CompensationFailed)
            .ToListAsync();
    }
}
