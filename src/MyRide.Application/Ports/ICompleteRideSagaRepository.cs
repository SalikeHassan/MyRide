using MyRide.Domain.Sagas;

namespace MyRide.Application.Ports;

public interface ICompleteRideSagaRepository
{
    Task Save(CompleteRideSagaState saga);
    Task<List<CompleteRideSagaState>> GetStuck();
}
