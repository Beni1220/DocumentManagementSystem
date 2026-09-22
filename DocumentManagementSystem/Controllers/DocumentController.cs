using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Domain.Model;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using Domain.Model.DTO;
[ApiController]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _service;
    public DocumentController(IDocumentService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        List<Document> documents = _service.ListDocuments();
        return Ok(new { message = "Get all documents", data = documents });
    }

    [HttpPost]
    public IActionResult Create(Document document)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            _service.UploadDocument(document);
            return Created($"api/documents/{document.Id}", document);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while uploading the document", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Document document)
    {
        if (id != document.Id)
        {
            return BadRequest(new { message = "Document ID mismatch" });
        }
        _service.UpdateDocument(id, document);
        return Ok(document);
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
