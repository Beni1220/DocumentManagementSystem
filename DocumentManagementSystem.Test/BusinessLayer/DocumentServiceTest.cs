namespace DocumentManagementSystem.Test.BusinessLayer;
using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using Domain.Massages;
using Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class DocumentServiceTest
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository = new();
    private readonly Mock<IMessagePublisher> _mockPublisher = new();
    private readonly DocumentService _service;

    public DocumentServiceTest()
    {
        _service = new DocumentService(_mockDocumentRepository.Object, _mockPublisher.Object);
    }

    [Fact]
    public void GetByID_ReturnsDocument_WhenDocumentExists()
    {
        // immer 3-A befolgen: Arrange, Act, Assert
        
        // Arrange
        var document = new Document { Id = 1, FileName = "Test Document" };
        _mockDocumentRepository.Setup(repo => repo.GetDocument(document.Id)).Returns(document);
        
        // Act
        var result = _service.GetDocument(document.Id);

        // Assert
        Assert.Equal(document, result);
        
    }

    [Fact]
    public void GetByID_ReturnsNull_WhenDocumentDoesNotExist()
    {
        // Arrange
        int documentId = 1;
        _mockDocumentRepository.Setup(repo => repo.GetDocument(documentId)).Returns((Document)null);
        // Act
        var result = _service.GetDocument(documentId);
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UploadDocument_CallsRepositoryUploadDocument()
    {
        // Arrange
        var document = new Document { Id = 1, FileName = "Test Document" };
        // Act
        _service.UploadDocument(document);
        // Assert
        _mockDocumentRepository.Verify(repo => repo.UploadDocument(document), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_SavesDocument_AndPublishesMessage()
    {
        // Arrange
        var document = new Document { FileName = "test.pdf" };
        var saved = new Document { Id = 5, FileName = "test.pdf" };
        _mockDocumentRepository.Setup(r => r.UploadDocument(document)).Returns(saved);

        // Act
        var result = await _service.UploadDocumentAsync(document);

        // Assert
        Assert.Equal(5, result.Id);
        _mockPublisher.Verify(p => p.PublishAsync(
            QueueNames.Ocr,
            It.Is<object>(m => ((DocumentUploadedMessage)m).DocumentId == 5)), Times.Once);
    }

}
