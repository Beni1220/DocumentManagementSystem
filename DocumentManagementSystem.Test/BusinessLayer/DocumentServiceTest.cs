namespace DocumentManagementSystem.Test.BusinessLayer;
using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services;
using DocumentManagementSystem.DataAccess.Exceptions;
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
    public void GetDocument_Throws_WhenDocumentDoesNotExist()
    {
        // Arrange
        _mockDocumentRepository.Setup(r => r.GetDocument(1)).Throws(new DocumentNotFoundException(1));

        // Act & Assert
        Assert.Throws<DocumentNotFoundException>(() => _service.GetDocument(1));
    }

    [Fact]
    public void UploadDocument_CallsRepositoryUploadDocument()
    {
        // Arrange
        var document = new Document { Id = 1, FileName = "Test Document" };
        // Act
        _service.CreateDocument(document);
        // Assert
        _mockDocumentRepository.Verify(repo => repo.CreateDocument(document), Times.Once);
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
    [Fact]
    public async Task UploadDocumentAsync_DeletesDocument_WhenPublishFails()
    {
        // Arrange
        var document = new Document { FileName = "test.pdf" };
        var saved = new Document { Id = 5, FileName = "test.pdf" };
        _mockDocumentRepository.Setup(r => r.UploadDocument(document)).Returns(saved);
        _mockPublisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new MessagingException("queue down", new Exception()));

        // Act & Assert
        await Assert.ThrowsAsync<MessagingException>(() => _service.UploadDocumentAsync(document));
        _mockDocumentRepository.Verify(r => r.DeleteDocument(5), Times.Once);
    }
}
