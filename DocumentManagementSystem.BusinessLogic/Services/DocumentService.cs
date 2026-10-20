﻿using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using DocumentManagementSystem.DataAccess.Exceptions;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using Domain.Massages;
using Domain.Model;
using Domain.Model.DTO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.BusinessLogic.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IMessagePublisher _publisher;

        private readonly ILogger<DocumentService> _logger;
        public DocumentService(IDocumentRepository documentRepository, IMessagePublisher publisher, ILogger<DocumentService> logger)
        {
            _documentRepository = documentRepository;
            _publisher = publisher;
            _logger = logger;
        }
        public void DeleteDocument(int documentId)
        {
            _documentRepository.DeleteDocument(documentId);
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

        // auf create umbenennen, auch im controller, interface und repository

        public Document CreateDocument(Document document)
        {
            return _documentRepository.CreateDocument(document);
        }

        public Document UploadDocument(Document document)
        {
            return _documentRepository.UploadDocument(document);
        }

        public async Task<Document> UploadDocumentAsync(Document document)
        {
            Document saved = _documentRepository.UploadDocument(document);
            _logger.LogInformation("Document {DocumentId} ({FileName}) saved to the database", saved.Id, saved.FileName);

            try
            {
                await _publisher.PublishAsync(QueueNames.Ocr, new DocumentUploadedMessage
                {
                    DocumentId = saved.Id,
                    FileName = saved.FileName
                });
            }
            catch (MessagingException)
            {
                _logger.LogWarning("Message for document {DocumentId} could not be sent, deleting the saved document again", saved.Id);
                _documentRepository.DeleteDocument(saved.Id);
                throw;
            }

            return saved;
        }
    }
}
