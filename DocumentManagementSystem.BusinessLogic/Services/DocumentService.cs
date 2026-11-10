﻿using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using DocumentManagementSystem.Storage.Exceptions;
using DocumentManagementSystem.Storage.Interfaces;
using Domain.Massages;
using Domain.Model;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.BusinessLogic.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IMessagePublisher _publisher;
        private readonly IFileStorage _fileStorage;
        private readonly ILogger<DocumentService> _logger;
        public DocumentService(IDocumentRepository documentRepository, IMessagePublisher publisher, IFileStorage fileStorage, ILogger<DocumentService> logger)
        {
            _documentRepository = documentRepository;
            _publisher = publisher;
            _fileStorage = fileStorage;
            _logger = logger;
        }
        public void DeleteDocument(int documentId)
        {
            _documentRepository.DeleteDocument(documentId);
            string objectName = $"{documentId}.pdf";
            try
            {
                _fileStorage.DeleteAsync(objectName);
            }
            catch (StorageException)
            {
                _logger.LogWarning("File {ObjectName} could not be removed from the storage", objectName);
            }
        }

        public Document GetDocument(int documentId)
        {
            return _documentRepository.GetDocument(documentId);
        }

        public List<Document> ListDocuments()
        {
            return _documentRepository.ListDocuments();
        }

        public void UpdateDocument(int documentId, Document document)
        {
            _documentRepository.UpdateDocument(documentId, document);
        }

        public Document CreateDocument(Document document)
        {
            return _documentRepository.CreateDocument(document);
        }

        public async Task<Document> UploadDocumentAsync(Document document, Stream content, long size, string contentType)
        {
            Document saved = _documentRepository.UploadDocument(document);
            _logger.LogInformation("Document {DocumentId} ({FileName}) saved to the database", saved.Id, saved.FileName);

            string objectName = $"{saved.Id}.pdf";
            bool fileStored = false;

            try
            {
                await _fileStorage.UploadAsync(objectName, content, size, contentType);
                fileStored = true;

                await _publisher.PublishAsync(QueueNames.Ocr, new DocumentUploadedMessage
                {
                    DocumentId = saved.Id,
                    FileName = saved.FileName,
                    ObjectName = objectName,
                    Description = saved.Description
                });
            }
            catch (Exception)
            {
                _logger.LogWarning("Upload of document {DocumentId} failed, rolling back", saved.Id);

                if (fileStored)
                {
                    await _fileStorage.DeleteAsync(objectName);
                }
                _documentRepository.DeleteDocument(saved.Id);
                throw;
            }

            return saved;
        }
    }
}
