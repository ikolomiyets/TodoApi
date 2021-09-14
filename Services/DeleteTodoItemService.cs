using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class DeleteTodoItemService : BackgroundService
    {
        private readonly ILogger _logger;  
        private readonly DefaultObjectPool<IModel> _objectPool;
        private readonly TodoContext _context;
        
        public DeleteTodoItemService(ILogger<DeleteTodoItemService> logger,
            IPooledObjectPolicy<IModel> objectPolicy,
            TodoContext context)  
        {  
            _logger = logger;
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
            _context = context;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)  
        {  
            stoppingToken.ThrowIfCancellationRequested();

            var channel = _objectPool.Get();
            var consumer = new EventingBasicConsumer(channel);  
            consumer.Received += (ch, ea) =>  
            {  
                // received message  
                TodoItem todoItem = 
                    JsonSerializer.Deserialize<TodoItem>(ea.Body.ToArray());
  
                if (todoItem != null)
                {
                    // handle the received message  
                    HandleMessage(todoItem.Id);
                    channel.BasicAck(ea.DeliveryTag, false);
                }  
            };  
  
            consumer.Shutdown += OnConsumerShutdown;  
            consumer.Registered += OnConsumerRegistered;  
            consumer.Unregistered += OnConsumerUnregistered;  
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;  
  
            channel.BasicConsume("Todo.deleteQueue", false, consumer);  
            return Task.CompletedTask;  
        }  

        private async void HandleMessage(long id)  
        {  
            // we just print this message   
            _logger.LogInformation("Received request to delete Todo item with ID: {Id}", id);  
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem != null)
            {
                _context.TodoItems.Remove(todoItem);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation("Item with ID: {Id} not found", id);
            }
        }  
      
        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)  {  }  
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) {  }  
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) {  }

        private void OnConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogInformation("Consumer shutdown");
        }  
    }
}