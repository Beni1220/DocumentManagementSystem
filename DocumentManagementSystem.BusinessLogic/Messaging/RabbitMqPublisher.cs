using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.DataAccess.Exceptions;
using Domain.Massages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DocumentManagementSystem.BusinessLogic.Messaging
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task PublishAsync(string queue, object message)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMq:HostName"]!,
                    Port = int.Parse(_configuration["RabbitMq:Port"]!),
                    UserName = _configuration["RabbitMq:UserName"]!,
                    Password = _configuration["RabbitMq:Password"]!
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                var properties = new BasicProperties { Persistent = true };

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queue,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);
            }
            catch (Exception ex)
            {
                throw new MessagingException($"Could not publish message to queue '{queue}'.", ex);
            }

        }
    }
}