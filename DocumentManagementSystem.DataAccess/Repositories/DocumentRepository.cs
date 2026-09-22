using DocumentManagementSystem.DataAccess.Context;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.DataAccess.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DMSContext _context;
        public DocumentRepository(DMSContext context)
        {
            _context = context;
        }
        public void DeleteDocument(int documentId)
        {
            var document = _context.Documents.Find(documentId);

            if (document != null)
            {
                _context.Documents.Remove(document);
                _context.SaveChanges();
            }
            else
            {
                throw new Exception($"Document with ID {documentId} not found.");
            }
        }

        public Document GetDocument(int documentId)
        {
            var document = _context.Documents.Find(documentId);

            if (document != null)
            {
                return document;
            }
            else
            {
                throw new Exception($"Document with ID {documentId} not found.");
            }
        }

        public List<Document> ListDocuments()
        {
            return _context.Documents.ToList();
        }

        public void UpdateDocument(int documentId, Document newDocument)
        {
            var document = _context.Documents.Find(documentId);

            if(document != null)
            {
                document.FileName = newDocument.FileName;
                document.Description = newDocument.Description;
                _context.SaveChanges();
            }
            else
            {
                throw new Exception($"Document with ID {documentId} not found.");
            }
        }

        public Document UploadDocument(Document document)
        {
            _context.Documents.Add(document);
            _context.SaveChanges();
            return document;
        }
    }
}
