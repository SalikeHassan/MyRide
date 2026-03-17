using Microsoft.EntityFrameworkCore;
using MyRide.Application.Ports;
using MyRide.Domain.Sagas;

namespace MyRide.Infrastructure.Persistence;

public class SqlStartRideSagaRepository : IStartRideSagaRepository
{
    private readonly OrchestratorDbContext context;

    public SqlStartRideSagaRepository(OrchestratorDbContext context)
    {
        this.context = context;
    }

    public async Task Save(StartRideSagaState saga)
    {
        var existing = await context.StartRideSagas.FindAsync(saga.SagaId);

        if (existing is null)
        {
            context.StartRideSagas.Add(saga);
        }

        await context.SaveChangesAsync();
    }

    public Task<List<StartRideSagaState>> GetStuck()
    {
        return context.StartRideSagas
            .Where(s => s.Status == StartRideSagaStatus.Compensating
                     || s.Status == StartRideSagaStatus.CompensationFailed)
            .ToListAsync();
    }
}
