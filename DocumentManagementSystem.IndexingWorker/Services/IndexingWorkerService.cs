using System.Text.Json;
using Domain.Massages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentManagementSystem.IndexingWorker.Services;

public class IndexingWorkerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexingWorkerService> _logger;
    private readonly ElasticsearchIndexer _indexer;

    public IndexingWorkerService(IConfiguration configuration, ILogger<IndexingWorkerService> logger,
        ElasticsearchIndexer indexer)
    {
        _configuration = configuration;
        _logger = logger;
        _indexer = indexer;
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
        await channel.QueueDeclareAsync(QueueNames.Indexing, true, false, false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<OcrCompletedMessage>(ea.Body.Span)
                              ?? throw new InvalidOperationException("Indexing message was empty.");
                await _indexer.IndexAsync(message, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                _logger.LogInformation("Indexed document {DocumentId} ({FileName})",
                    message.DocumentId, message.FileName);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Indexing failed for message {DeliveryTag}; message will be retried", ea.DeliveryTag);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueNames.Indexing, false, consumer, stoppingToken);
        _logger.LogInformation("Indexing worker is waiting for messages on {Queue}", QueueNames.Indexing);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<IConnection> ConnectWithRetryAsync(ConnectionFactory factory, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await factory.CreateConnectionAsync(cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "RabbitMQ not reachable (attempt {Attempt}), retrying in 5 seconds", attempt);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
