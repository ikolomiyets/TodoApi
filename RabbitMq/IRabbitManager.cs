namespace TodoApi.RabbitMq
{
    public interface IRabbitManager
    {
        void Publish<T>(T message, string exchangeName, string routeKey) where T : class;
    }
}