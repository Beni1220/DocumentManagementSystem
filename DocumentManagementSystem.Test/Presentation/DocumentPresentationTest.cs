using System;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using DocumentManagementSystem;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using Domain.Model;

public class DocumentPresentationTest
{
	private readonly Mock<IDocumentService> _mockDocumentService = new();
	private readonly DocumentController _controller;

    public DocumentPresentationTest()
	{
        _controller = new DocumentController(_mockDocumentService.Object);
	}

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
    }


    [Fact]
    public void Create_ReturnsCreatedResult_WhenDocumentIsValid()
    {
        // Arrange
        var document = new Document { Id = 1, FileName = "New Document" };
        // Act
        var result = _controller.Create(document);
        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);

    }

    [Fact]
    public void Create_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Title", "Required");
        var document = new Document { Id = 1 };
        // Act
        var result = _controller.Create(document);
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

    }
}
