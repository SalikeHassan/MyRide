using MyRide.Domain.Sagas;

namespace MyRide.Application.Ports;

public interface IRequestRideSagaRepository
{
    Task Save(RequestRideSagaState saga);
    Task<List<RequestRideSagaState>> GetStuck();
}
