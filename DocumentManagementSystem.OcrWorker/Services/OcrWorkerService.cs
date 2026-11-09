using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DocumentManagementSystem.Storage.Interfaces;
using Domain.Massages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentManagementSystem.OcrWorker.Services;

public class OcrWorkerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcrWorkerService> _logger;
    private readonly IFileStorage _fileStorage;

    public OcrWorkerService(IConfiguration configuration, ILogger<OcrWorkerService> logger, IFileStorage fileStorage)
    {
        _configuration = configuration;
        _logger = logger;
        _fileStorage = fileStorage;
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
        await channel.QueueDeclareAsync(QueueNames.Ocr, true, false, false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueNames.Indexing, true, false, false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<DocumentUploadedMessage>(ea.Body.Span)
                              ?? throw new InvalidOperationException("OCR message was empty.");
                _logger.LogInformation("Starting OCR for document {DocumentId} ({FileName})",
                    message.DocumentId, message.FileName);

                await using var pdf = await _fileStorage.DownloadAsync(message.ObjectName);
                var text = await RecognizePdfAsync(pdf, stoppingToken);
                var result = new OcrCompletedMessage
                {
                    DocumentId = message.DocumentId,
                    FileName = message.FileName,
                    Description = message.Description,
                    TextContent = text
                };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
                await channel.BasicPublishAsync("", QueueNames.Indexing, true,
                    new BasicProperties { Persistent = true }, body, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                _logger.LogInformation("OCR completed for document {DocumentId}; queued for indexing", message.DocumentId);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "OCR failed for message {DeliveryTag}; message will be retried", ea.DeliveryTag);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueNames.Ocr, false, consumer, stoppingToken);
        _logger.LogInformation("OCR worker is waiting for messages on {Queue}", QueueNames.Ocr);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<string> RecognizePdfAsync(Stream pdf, CancellationToken cancellationToken)
    {
        var workDirectory = Directory.CreateTempSubdirectory("dms-ocr-");
        try
        {
            var pdfPath = Path.Combine(workDirectory.FullName, "document.pdf");
            await using (var output = File.Create(pdfPath))
            {
                await pdf.CopyToAsync(output, cancellationToken);
                _logger.LogInformation("Downloaded PDF for OCR: {Size} bytes", output.Length);
            }

            var render = await RunProcessAsync("gs", new[]
            {
                "-dSAFER", "-dNOPAUSE", "-dBATCH", "-sDEVICE=pnggray", "-r300",
                "-sOutputFile=page-%04d.png", pdfPath
            }, workDirectory.FullName, cancellationToken);
            if (render.ExitCode != 0)
            {
                throw new InvalidOperationException($"Ghostscript failed: {render.Error}");
            }

            var pages = Directory.GetFiles(workDirectory.FullName, "page-*.png");
            Array.Sort(pages, StringComparer.Ordinal);
            if (pages.Length == 0)
            {
                throw new InvalidOperationException($"Ghostscript did not render any PDF pages: {render.Output} {render.Error}");
            }

            var recognizedPages = new List<string>(pages.Length);
            foreach (var page in pages)
            {
                var recognized = await RunProcessAsync("tesseract",
                    new[] { page, "stdout", "-l", "eng+deu", "--psm", "3" },
                    workDirectory.FullName, cancellationToken);
                if (recognized.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Tesseract failed: {recognized.Error}");
                }
                recognizedPages.Add(recognized.Output.Trim());
            }

            return string.Join(Environment.NewLine + Environment.NewLine, recognizedPages);
        }
        finally
        {
            workDirectory.Delete(true);
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(
        string fileName, IEnumerable<string> arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw;
        }

        return (process.ExitCode, await outputTask, await errorTask);
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
