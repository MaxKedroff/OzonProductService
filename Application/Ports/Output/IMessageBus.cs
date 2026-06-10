namespace Application.Ports.Output
{
    public interface IMessageBus
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
        Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
            where T : class
            where TH : IEventHandler<T>;
    }

    public interface IEventHandler<T> where T : class
    {
        Task HandleAsync(T eventData, CancellationToken cancellationToken = default);
    }
}
