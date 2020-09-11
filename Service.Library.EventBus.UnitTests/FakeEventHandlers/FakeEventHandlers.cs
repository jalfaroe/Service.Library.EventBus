using System.Threading.Tasks;

using Service.Library.EventBus.UnitTests.FakeEvents;

namespace Service.Library.EventBus.UnitTests.FakeEventHandlers
{
    public class FakeEvent1Handler : IIntegrationEventHandler<FakeEvent1>
    {
        public Task HandleAsync(FakeEvent1 @event)
        {
            return Task.CompletedTask;
        }
    }

    public class FakeEvent2Handler : IIntegrationEventHandler<FakeEvent2>
    {
        public Task HandleAsync(FakeEvent2 @event)
        {
            return Task.CompletedTask;
        }
    }
}