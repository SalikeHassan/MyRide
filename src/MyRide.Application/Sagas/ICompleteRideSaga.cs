using MyRide.Domain.Sagas;

namespace MyRide.Application.Sagas;

public interface ICompleteRideSaga
{
    Task<CompleteRideSagaState> Execute(Guid rideId, string tenantId);
    Task<CompleteRideSagaState> Resume(CompleteRideSagaState saga);
}
