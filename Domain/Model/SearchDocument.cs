namespace Domain.Model;

public class SearchDocument
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = default!;
    public string? Description { get; set; }
    public string TextContent { get; set; } = default!;
}
