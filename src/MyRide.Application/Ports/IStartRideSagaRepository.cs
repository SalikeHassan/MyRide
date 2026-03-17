using MyRide.Domain.Sagas;

namespace MyRide.Application.Ports;

public interface IStartRideSagaRepository
{
    Task Save(StartRideSagaState saga);
    Task<List<StartRideSagaState>> GetStuck();
}
