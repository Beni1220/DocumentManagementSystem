using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Domain.Model;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using Domain.Model.DTO;
using AutoMapper;
[ApiController]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _service;
    private readonly IMapper _mapper;

    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentService service, IMapper mapper, ILogger<DocumentController> logger)
    {
        _service = service;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        List<Document> documents = _service.ListDocuments();
        return Ok(new { message = "Get all documents", data = documents });
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
    public IActionResult Delete(int id)
    {
       _service.DeleteDocument(id);

        _logger.LogInformation("Deleting document {DocumentId}", id);

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

        DTODocument dtoDocument = new DTODocument
        {
            FileName = file.FileName,
            Description = description,
            sizeInBytes = file.Length,
            contentType = file.ContentType
        };

        Document document = _mapper.Map<Document>(dtoDocument);
        var saved = await _service.UploadDocumentAsync(document);

        _logger.LogInformation("Upload finished: document {DocumentId} created", saved.Id);

        return Created($"api/documents/{saved.Id}", saved);
    }
}
