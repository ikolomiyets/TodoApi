using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TodoApi.RabbitMq
{
    public class RabbitModelPooledObjectPolicy : IPooledObjectPolicy<IModel> 
    {
        private readonly RabbitOptions _options;  
        private readonly IConnection _connection;
        private readonly ILogger<RabbitModelPooledObjectPolicy> _logger;
  
        public RabbitModelPooledObjectPolicy(IOptions<RabbitOptions> optionsAccs,
            ILogger<RabbitModelPooledObjectPolicy> logger)  
        {  
            _options = optionsAccs.Value;  
            _logger = logger;
            _connection = GetConnection();
        }  
  
        private IConnection GetConnection()
        {
            var password = _options.Password.Substring(0, 2) + "********";
            _logger.LogInformation("Password: {Password}", _options.Password);
            _logger?.LogInformation("Connecting to the RabbitMq instance at {Host}:{Port}. Credentials: {Username}/{Password}", _options.HostName, _options.Port, _options.UserName, password);
            var factory = new ConnectionFactory()  
            {  
                HostName = _options.HostName,  
                UserName = _options.UserName,  
                Password = _options.Password,  
                Port = _options.Port,  
                VirtualHost = _options.VHost,
            };  
  
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            
            channel.ExchangeDeclare(
                exchange: _options.Prefix + ".exchange", 
                type: "direct", 
                durable: true, 
                autoDelete: false);
                
            channel.QueueDeclare(
                queue: _options.Prefix + ".createQueue", 
                durable: true,
                exclusive: false, 
                autoDelete: false);
                
            channel.QueueBind(
                queue: _options.Prefix + ".createQueue", 
                exchange: _options.Prefix + ".exchange", 
                routingKey: _options.Prefix + ".create");
                
            channel.QueueDeclare(
                queue: _options.Prefix + ".deleteQueue", 
                durable: true,
                exclusive: false, 
                autoDelete: false);
                
            channel.QueueBind(
                queue: _options.Prefix + ".deleteQueue", 
                exchange: _options.Prefix + ".exchange", 
                routingKey: _options.Prefix + ".delete");

            return connection;
        }  
  
        public IModel Create()  
        {  
            return _connection.CreateModel();
        }  
  
        public bool Return(IModel obj)  
        {  
            if (obj.IsOpen)  
            {  
                return true;  
            }  
            else  
            {  
                obj?.Dispose();  
                return false;  
            }  
        }
    }
}