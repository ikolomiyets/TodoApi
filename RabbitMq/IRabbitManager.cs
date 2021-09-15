namespace TodoApi.RabbitMq
{
    public interface IRabbitManager
    {
        void Publish<T>(T message, string exchangeName, string routeKey, string returnQueue) where T : class;

        void Publish<T>(T message, string exchangeName, string routeKey) where T : class
        {
            Publish(message, exchangeName, routeKey, null);
        }
        T Receive<T>(string queueName) where T : class;
        void DeleteQueue(string queueName);
    }
}