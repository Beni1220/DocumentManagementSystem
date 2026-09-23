namespace DocumentManagementSystem.Tests;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using DocumentManagementSystem.BusinessLogic.Services;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;

public class DocumentServiceTest
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository = new();
    private readonly DocumentService _service;

    public DocumentServiceTest()
    {
        _service = new DocumentService(_mockDocumentRepository.Object);
    }

    [Fact]
    public void GetByID_ReturnsDocument_WhenDocumentExists()
    {
        // immer 3-A befolgen: Arrange, Act, Assert
        
        // Arrange
        var document = new Document { Id = 1, Title = "Test Document" };
        _mockDocumentRepository.Setup(repo => repo.GetByID(document.Id)).Returns(document);
        
        // Act
        var result = _service.GetDocument(document.Id);

        // Assert
        var assertResult = Assert.Equal(document, result);
        
    }

    [Fact]
    public void GetByID_ReturnsNull_WhenDocumentDoesNotExist()
    {
        // Arrange
        int documentId = 1;
        _mockDocumentRepository.Setup(repo => repo.GetByID(documentId)).Returns((Document)null);
        // Act
        var result = _service.GetDocument(documentId);
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UploadDocument_CallsRepositoryUploadDocument()
    {
        // Arrange
        var document = new Document { Id = 1, Title = "Test Document" };
        _mockDocumentRepository.Setup(repo => repo.UploadDocument(document)).Verifiable();
        // Act
        _service.UploadDocument(document);
        // Assert
        _mockDocumentRepository.Verify(repo => repo.UploadDocument(document), Times.Once);
    }


}
