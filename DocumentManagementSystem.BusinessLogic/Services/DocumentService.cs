using DocumentManagementSystem.BusinessLogic.Services.Interfaces;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
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
        public DocumentService(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
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

        public Document UploadDocument(Document document)
        {
            return _documentRepository.UploadDocument(document);
        }
    }
}
