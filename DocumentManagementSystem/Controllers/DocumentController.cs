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

    public DocumentController(IDocumentService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
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

        

        try
        {
            var document = _mapper.Map<Document>(dtoDocument);
            _service.UploadDocument(document);
            return Created($"api/documents/{document.Id}", document);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while uploading the document", error = ex.Message });
        }
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

    
}
