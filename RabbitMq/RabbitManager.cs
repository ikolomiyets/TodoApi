using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace TodoApi.RabbitMq
{
    public class RabbitManager : IRabbitManager
    {
        private readonly DefaultObjectPool<IModel> _objectPool;  
        private readonly RabbitOptions _options;  

        public RabbitManager(IPooledObjectPolicy<IModel> objectPolicy,
            IOptions<RabbitOptions> options)  
        {  
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
            _options = options.Value;
        }  
  
        public void Publish<T>(T message, string exchangeName, string routeKey, string returnQueue)   
            where T : class  
        {  
            if (message == null)  
                return;  
  
            var channel = _objectPool.Get();  
  
            try  
            {
                if (returnQueue != null)
                {
                    IDictionary<string, object> arguments = new Dictionary<string, object>()
                    {
                        { "x-expires", _options.Ttl * 1000L }
                    };
                    channel.QueueDeclare(returnQueue, true, false, true, arguments);  
                    channel.QueueBind(
                        queue: returnQueue, 
                        exchange: _options.Prefix + ".exchange", 
                        routingKey: returnQueue);
                }
  
                var sendBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));  
  
                var properties = channel.CreateBasicProperties();  
                properties.Persistent = true;
                if (returnQueue != null)
                {
                    properties.Headers = new Dictionary<string, object>()
                    {
                        {"x-return-queue", returnQueue}
                    };
                }
  
                channel.BasicPublish(exchangeName, routeKey, properties, sendBytes);  
            }  
            catch (Exception ex)  
            {  
                throw ex;  
            }  
            finally  
            {  
                _objectPool.Return(channel);                  
            }  
        }

        public T Receive<T>(string queueName) where T : class
        {
            if (queueName == null)  
                return null;  
  
            var channel = _objectPool.Get();  
  
            try
            {
                long startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long now = startedAt;
                BasicGetResult result = null;
                long timeout = _options.RetrieveTimeout * 1000L;
                // received message  
                do
                {
                    result = channel.BasicGet(queueName, true);
                    Thread.Sleep(500);
                    now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                } while (result == null && now - startedAt < timeout);

                if (result == null)
                {
                    return null;
                }
                
                using (var stream = new MemoryStream(result.Body.ToArray()))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return JsonSerializer.Create().Deserialize(new JsonTextReader(reader), typeof(T)) as T;
            }  
            catch (Exception ex)  
            {  
                throw ex;  
            }  
            finally  
            {  
                _objectPool.Return(channel);                  
            }  
        }

        public void DeleteQueue(string queueName)
        {
            if (queueName == null)  
                return;  
  
            var channel = _objectPool.Get();  
  
            try
            {
                channel.QueueDelete(queueName, false, false);
            }  
            catch (Exception ex)  
            {  
                throw ex;  
            }  
            finally  
            {  
                _objectPool.Return(channel);                  
            }  
        }
    }
}