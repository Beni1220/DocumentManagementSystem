using Domain.Model;
using Domain.Model.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.BusinessLogic.Services.Interfaces
{
    public interface IDocumentService
    {
        Document CreateDocument(Document document);

        void UpdateDocument(int documentId, Document document);

        void DeleteDocument(int documentId);

        Document GetDocument(int documentId);

        List<Document> ListDocuments();
<<<<<<< Updated upstream
=======

        Task<Document> UploadDocumentAsync(Document document);
>>>>>>> Stashed changes

        Document UploadDocument(Document document);
    }
}
