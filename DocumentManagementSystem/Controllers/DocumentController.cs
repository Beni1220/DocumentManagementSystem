using Microsoft.AspNetCore.Mvc;
using Domain.Model;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using Domain.Model.DTO;
using AutoMapper;
using DocumentManagementSystem.Services;
[ApiController]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _service;
    private readonly IMapper _mapper;

    private readonly ILogger<DocumentController> _logger;
    private readonly IDocumentSearchService _searchService;

    public DocumentController(IDocumentService service, IMapper mapper, ILogger<DocumentController> logger,
        IDocumentSearchService searchService)
    {
        _service = service;
        _mapper = mapper;
        _logger = logger;
        _searchService = searchService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        List<Document> documents = _service.ListDocuments();
        return Ok(new { message = "Get all documents", data = documents });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { message = "A search term is required." });
        }

        if (q.Trim().Length < 3)
        {
            return BadRequest(new { message = "Search terms must be at least 3 characters long." });
        }

        var searchResults = await _searchService.SearchAsync(q.Trim(), cancellationToken);

        // PostgreSQL sagt, welche Dokumente es noch gibt. Elasticsearch kann
        // alte Treffer enthalten, z. B. wenn ein Worker erst nach dem Löschen fertig wird.
        var documentsById = _service.ListDocuments().ToDictionary(document => document.Id);
        var results = new List<SearchDocument>();
        foreach (var searchResult in searchResults)
        {
            if (documentsById.TryGetValue(searchResult.DocumentId, out var document))
            {
                results.Add(new SearchDocument
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Description = document.Description
                });
            }
        }
        return Ok(new
        {
            message = "Search documents",
            data = results.Select(document => new
            {
                document.DocumentId,
                document.FileName,
                document.Description
            })
        });
    }

    [HttpPost]
    public IActionResult Create(DTODocument dtoDocument)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var document = _mapper.Map<Document>(dtoDocument);
        _service.CreateDocument(document);
        return Created($"api/documents/{document.Id}", document);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, DTODocument dtoDocument)
    {

        var existing = _service.GetDocument(id);
        if (existing == null)
            return NotFound(new { message = $"Document with ID {id} not found" });

        _service.UpdateDocument(id, _mapper.Map<Document>(dtoDocument));

        return Ok(_service.GetDocument(id));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _service.DeleteDocument(id);
        await _searchService.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Deleted document {DocumentId} from database and search index", id);

        return NoContent();
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var document = _service.GetDocument(id);
        if (document == null)
        {
            return NotFound(new { message = $"Document with ID {id} not found" });
        }
        return Ok(new { message = $"Get document with ID {id}", data = document });
    }

    [Consumes("multipart/form-data")]
    [HttpPost("uploadDocument")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? description)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload rejected: no file provided");
            return BadRequest(new { message = "No file uploaded" });
        }

        bool isPdf = file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
        {
            _logger.LogWarning("Upload rejected: {FileName} has content type {ContentType}", file.FileName, file.ContentType);
            return BadRequest(new { message = "Only PDF files are allowed" });
        }

        _logger.LogInformation("Upload started: {FileName} ({Size} bytes)", file.FileName, file.Length);

        var dtoDocument = new DTODocument
        {
            FileName = file.FileName,
            Description = description,
            sizeInBytes = file.Length,
            contentType = file.ContentType
        };

        var document = _mapper.Map<Document>(dtoDocument);

        using var stream = file.OpenReadStream();
        var saved = await _service.UploadDocumentAsync(document, stream, file.Length, file.ContentType);

        _logger.LogInformation("Upload finished: document {DocumentId} created", saved.Id);
        return Created($"api/documents/{saved.Id}", saved);
    }
}
