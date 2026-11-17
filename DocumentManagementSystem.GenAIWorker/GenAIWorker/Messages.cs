namespace GenAIWorker;

public static class Queues
{
    public const string SummaryRequest = "summary.request";
    public const string SummaryResult = "summary.result";
}


public record SummaryRequestMessage(int DocumentId, string Text);
public record SummaryResultMessage(int DocumentId, bool Success, string? Summary, string? Error);