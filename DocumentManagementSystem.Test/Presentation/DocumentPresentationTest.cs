using AutoMapper;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using Domain.Model;
using Domain.Model.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class DocumentPresentationTest
{
    private readonly Mock<IDocumentService> _mockDocumentService = new();
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly DocumentController _controller;

    public DocumentPresentationTest()
    {
        _controller = new DocumentController(
            _mockDocumentService.Object,
            _mockMapper.Object,
            NullLogger<DocumentController>.Instance);
    }

    private static Mock<IFormFile> CreateFile(string fileName, string contentType, long length)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        return file;
    }

    // ---------- GetAll ----------

    [Fact]
    public void GetAll_ReturnsOkResult_WithListOfDocuments()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document { Id = 1, FileName = "Document 1" },
            new Document { Id = 2, FileName = "Document 2" }
        };
        _mockDocumentService.Setup(service => service.ListDocuments()).Returns(documents);

        // Act
        var result = _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockDocumentService.Verify(s => s.ListDocuments(), Times.Once);
    }

    // ---------- GetById ----------

    [Fact]
    public void GetById_ReturnsOkResult_WhenDocumentExists()
    {
        // Arrange
        _mockDocumentService.Setup(s => s.GetDocument(1))
            .Returns(new Document { Id = 1, FileName = "Document 1" });

        // Act
        var result = _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ReturnsCreatedResult_WhenDocumentIsValid()
    {
        // Arrange
        var dto = new DTODocument { FileName = "New Document", Description = "test" };
        var document = new Document { Id = 1, FileName = "New Document" };
        _mockMapper.Setup(m => m.Map<Document>(dto)).Returns(document);

        // Act
        var result = _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("api/documents/1", createdResult.Location);
        _mockDocumentService.Verify(s => s.CreateDocument(document), Times.Once);
    }

    [Fact]
    public void Create_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("FileName", "Required");

        // Act
        var result = _controller.Create(new DTODocument());

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        _mockDocumentService.Verify(s => s.CreateDocument(It.IsAny<Document>()), Times.Never);
    }

    // ---------- Update ----------

    [Fact]
    public void Update_ReturnsOkResult_AndCallsService()
    {
        // Arrange
        var dto = new DTODocument { FileName = "Updated" };
        var mapped = new Document { FileName = "Updated" };
        _mockMapper.Setup(m => m.Map<Document>(dto)).Returns(mapped);
        _mockDocumentService.Setup(s => s.GetDocument(1))
            .Returns(new Document { Id = 1, FileName = "Updated" });

        // Act
        var result = _controller.Update(1, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockDocumentService.Verify(s => s.UpdateDocument(1, mapped), Times.Once);
    }

    // ---------- Delete ----------

    [Fact]
    public void Delete_ReturnsNoContent_AndCallsService()
    {
        // Act
        var result = _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockDocumentService.Verify(s => s.DeleteDocument(1), Times.Once);
    }

    // ---------- Upload ----------

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileIsMissing()
    {
        // Act
        var result = await _controller.Upload(null!, "desc");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockDocumentService.Verify(s => s.UploadDocumentAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var file = CreateFile("empty.pdf", "application/pdf", 0);

        // Act
        var result = await _controller.Upload(file.Object, "desc");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockDocumentService.Verify(s => s.UploadDocumentAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileIsNotPdf()
    {
        // Arrange
        var file = CreateFile("image.png", "image/png", 1024);

        // Act
        var result = await _controller.Upload(file.Object, "desc");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockDocumentService.Verify(s => s.UploadDocumentAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ReturnsCreated_AndCallsService_WhenFileIsPdf()
    {
        // Arrange
        var file = CreateFile("test.pdf", "application/pdf", 1024);
        var document = new Document { Id = 7, FileName = "test.pdf" };

        _mockMapper
            .Setup(m => m.Map<Document>(It.Is<DTODocument>(d =>
                d.FileName == "test.pdf" &&
                d.Description == "desc" &&
                d.sizeInBytes == 1024 &&
                d.contentType == "application/pdf")))
            .Returns(document);
        _mockDocumentService.Setup(s => s.UploadDocumentAsync(document)).ReturnsAsync(document);

        // Act
        var result = await _controller.Upload(file.Object, "desc");

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("api/documents/7", createdResult.Location);
        _mockDocumentService.Verify(s => s.UploadDocumentAsync(document), Times.Once);
    }
}