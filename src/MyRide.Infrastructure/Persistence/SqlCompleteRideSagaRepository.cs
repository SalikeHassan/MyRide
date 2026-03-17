using Microsoft.EntityFrameworkCore;
using MyRide.Application.Ports;
using MyRide.Domain.Sagas;

namespace MyRide.Infrastructure.Persistence;

public class SqlCompleteRideSagaRepository : ICompleteRideSagaRepository
{
    private readonly OrchestratorDbContext context;

    public SqlCompleteRideSagaRepository(OrchestratorDbContext context)
    {
        this.context = context;
    }

    public async Task Save(CompleteRideSagaState saga)
    {
        var existing = await context.CompleteRideSagas.FindAsync(saga.SagaId);

        if (existing is null)
        {
            context.CompleteRideSagas.Add(saga);
        }

        await context.SaveChangesAsync();
    }

    public Task<List<CompleteRideSagaState>> GetStuck()
    {
        return context.CompleteRideSagas
            .Where(s => s.Status == CompleteRideSagaStatus.FreeDriverFailed
                     || s.Status == CompleteRideSagaStatus.PaymentFailed
                     || s.Status == CompleteRideSagaStatus.PayoutFailed)
            .ToListAsync();
    }
}
