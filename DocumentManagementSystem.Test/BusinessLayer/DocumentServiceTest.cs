namespace DocumentManagementSystem.Test.BusinessLayer;
using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services;
using DocumentManagementSystem.DataAccess.Exceptions;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using Domain.Massages;
using Domain.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class DocumentServiceTest
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository = new();
    private readonly Mock<IMessagePublisher> _mockPublisher = new();
    private readonly DocumentService _service;

    public DocumentServiceTest()
    {
        _service = new DocumentService(
            _mockDocumentRepository.Object,
            _mockPublisher.Object,
            NullLogger<DocumentService>.Instance);
    }

    // ---------- Get ----------

    [Fact]
    public void GetDocument_ReturnsDocument_WhenDocumentExists()
    {
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
        _mockDocumentRepository.Setup(repo => repo.GetDocument(1))
            .Throws(new DocumentNotFoundException(1));

        // Act & Assert
        Assert.Throws<DocumentNotFoundException>(() => _service.GetDocument(1));
    }

    // ---------- List ----------

    [Fact]
    public void ListDocuments_ReturnsDocumentsFromRepository()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document { Id = 1, FileName = "Document 1" },
            new Document { Id = 2, FileName = "Document 2" }
        };
        _mockDocumentRepository.Setup(repo => repo.ListDocuments()).Returns(documents);

        // Act
        var result = _service.ListDocuments();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(documents, result);
    }

    // ---------- Create ----------

    [Fact]
    public void CreateDocument_CallsRepository_AndReturnsCreatedDocument()
    {
        // Arrange
        var document = new Document { FileName = "Test Document" };
        var created = new Document { Id = 1, FileName = "Test Document" };
        _mockDocumentRepository.Setup(repo => repo.CreateDocument(document)).Returns(created);

        // Act
        var result = _service.CreateDocument(document);

        // Assert
        Assert.Equal(1, result.Id);
        _mockDocumentRepository.Verify(repo => repo.CreateDocument(document), Times.Once);
    }

    // ---------- Update ----------

    [Fact]
    public void UpdateDocument_CallsRepository()
    {
        // Arrange
        var document = new Document { FileName = "Updated" };

        // Act
        _service.UpdateDocument(1, document);

        // Assert
        _mockDocumentRepository.Verify(repo => repo.UpdateDocument(1, document), Times.Once);
    }

    // ---------- Delete ----------

    [Fact]
    public void DeleteDocument_CallsRepository()
    {
        // Act
        _service.DeleteDocument(1);

        // Assert
        _mockDocumentRepository.Verify(repo => repo.DeleteDocument(1), Times.Once);
    }

    [Fact]
    public void DeleteDocument_Throws_WhenDocumentDoesNotExist()
    {
        // Arrange
        _mockDocumentRepository.Setup(repo => repo.DeleteDocument(1))
            .Throws(new DocumentNotFoundException(1));

        // Act & Assert
        Assert.Throws<DocumentNotFoundException>(() => _service.DeleteDocument(1));
    }

    // ---------- Upload ----------

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
        _mockDocumentRepository.Verify(r => r.UploadDocument(document), Times.Once);
        _mockPublisher.Verify(p => p.PublishAsync(
            QueueNames.Ocr,
            It.Is<object>(m => m is DocumentUploadedMessage
                               && ((DocumentUploadedMessage)m).DocumentId == 5
                               && ((DocumentUploadedMessage)m).FileName == "test.pdf")),
            Times.Once);
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

    [Fact]
    public async Task UploadDocumentAsync_DoesNotPublish_WhenSavingFails()
    {
        // Arrange
        var document = new Document { FileName = "test.pdf" };
        _mockDocumentRepository.Setup(r => r.UploadDocument(document))
            .Throws(new DataAccessException("db down"));

        // Act & Assert
        await Assert.ThrowsAsync<DataAccessException>(() => _service.UploadDocumentAsync(document));
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}