using Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IDocumentRepository
    {
        Document UploadDocument(Document document);
        void UpdateDocument(int documentId, Document document);
        void DeleteDocument(int documentId);
        Document GetDocument(int documentId);
        List<Document> ListDocuments();

    }
}
