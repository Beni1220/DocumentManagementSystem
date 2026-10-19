using Domain.Massages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Domain.Massages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace DocumentManagementSystem.OcrWorker.Services
{
    public class OcrWorkerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OcrWorkerService> _logger;

        public OcrWorkerService(IConfiguration configuration, ILogger<OcrWorkerService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"]!,
                Port = int.Parse(_configuration["RabbitMq:Port"]!),
                UserName = _configuration["RabbitMq:UserName"]!,
                Password = _configuration["RabbitMq:Password"]!
            };

            await using var connection = await ConnectWithRetryAsync(factory, stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: QueueNames.Ocr,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<DocumentUploadedMessage>(ea.Body.Span)
                                  ?? throw new InvalidOperationException("Message is empty");

                    _logger.LogInformation("OCR request received for document {DocumentId} ({FileName})",
                        message.DocumentId, message.FileName);

                    // TODO Sprint 4: PDF aus MinIO holen und OCR durchführen

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not process message {DeliveryTag}", ea.DeliveryTag);
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: QueueNames.Ocr,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("OCR worker is waiting for messages on {Queue}", QueueNames.Ocr);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task<IConnection> ConnectWithRetryAsync(ConnectionFactory factory, CancellationToken ct)
        {
            const int maxAttempts = 10;

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await factory.CreateConnectionAsync(ct);
                }
                catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "RabbitMQ not reachable (attempt {Attempt}/{Max}), retrying in 5 seconds",
                        attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
        }
    }
}
