﻿using DocumentManagementSystem.BusinessLogic.Messaging.Interfaces;
using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using Domain.Massages;
using Domain.Model;
using Domain.Model.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.BusinessLogic.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IMessagePublisher _publisher;
        public DocumentService(IDocumentRepository documentRepository, IMessagePublisher publisher)
        {
            _documentRepository = documentRepository;
            _publisher = publisher;
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

            await _publisher.PublishAsync(QueueNames.Ocr, new DocumentUploadedMessage
            {
                DocumentId = saved.Id,
                FileName = saved.FileName
            });

            return saved;
        }
    }
}
