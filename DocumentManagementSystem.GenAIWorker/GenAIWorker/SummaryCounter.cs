using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GenAIWorker;

public class SummaryConsumer(IConfiguration config, GeminiClient gemini, ILogger<SummaryConsumer> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "rabbitmq",
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        IConnection connection = null!;
        for (var attempt = 1; ; attempt++)
        {
            try { connection = await factory.CreateConnectionAsync(ct); break; }
            catch (Exception ex) when (attempt < 10)
            {
                log.LogWarning("RabbitMQ not reachable (attempt {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }

        await using var _ = connection;
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync(Queues.SummaryRequest, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueDeclareAsync(Queues.SummaryResult, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.BasicQosAsync(0, 1, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            SummaryRequestMessage? msg = null;
            try
            {
                msg = JsonSerializer.Deserialize<SummaryRequestMessage>(Encoding.UTF8.GetString(ea.Body.Span));
                if (msg is null) throw new JsonException("Message was null");

                log.LogInformation("Generating summary for document {DocumentId}", msg.DocumentId);
                var summary = await gemini.SummarizeAsync(msg.Text, ct);

                await PublishResultAsync(channel, new SummaryResultMessage(msg.DocumentId, true, summary, null), ct);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                log.LogInformation("Summary for document {DocumentId} done", msg.DocumentId);
            }
            catch (GeminiException ex) when (ex.IsTransient && !ea.Redelivered)
            {
                // einmal zurück in die Queue, bevor wir aufgeben
                log.LogWarning(ex, "Transient Gemini error for {DocumentId}, requeueing once", msg?.DocumentId);
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Summary failed for document {DocumentId}", msg?.DocumentId);
                if (msg is not null)
                    await PublishResultAsync(channel, new SummaryResultMessage(msg.DocumentId, false, null, ex.Message), ct);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
        };

        await channel.BasicConsumeAsync(Queues.SummaryRequest, autoAck: false, consumer, ct);
        log.LogInformation("GenAI worker listening on {Queue}", Queues.SummaryRequest);
        await Task.Delay(Timeout.Infinite, ct);
    }

    private static async Task PublishResultAsync(IChannel channel, SummaryResultMessage result, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: Queues.SummaryResult,
            mandatory: false,
            basicProperties: new BasicProperties { Persistent = true },
            body: body,
            cancellationToken: ct);
    }
}