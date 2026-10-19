using DocumentManagementSystem.DataAccess.Context;
using DocumentManagementSystem.DataAccess.Exceptions;
using DocumentManagementSystem.DataAccess.Repositories.Interfaces;
using Domain.Model;
using Microsoft.EntityFrameworkCore;
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
            var document = FindDocument(documentId);
            _context.Documents.Remove(document);
            Save($"Could not delete document {documentId}.");
        }

        public Document GetDocument(int documentId)
        {
            return FindDocument(documentId);
        }

        public List<Document> ListDocuments()
        {
            try
            {
                return _context.Documents.ToList();
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Could not load the documents from the database.", ex);
            }
        }

        public void UpdateDocument(int documentId, Document newDocument)
        {
            var document = FindDocument(documentId);
            document.FileName = newDocument.FileName;
            document.Description = newDocument.Description;
            Save($"Could not update document {documentId}.");
        }

        public Document CreateDocument(Document document)
        {
            _context.Documents.Add(document);
            Save("Could not create the document.");
            return document;
        }

        public Document UploadDocument(Document document)
        {
            _context.Documents.Add(document);
            Save("Could not save the uploaded document.");
            return document;
        }

        private Document FindDocument(int documentId)
        {
            Document? document;
            try
            {
                document = _context.Documents.Find(documentId);
            }
            catch (Exception ex)
            {
                throw new DataAccessException($"Could not read document {documentId} from the database.", ex);
            }

            return document ?? throw new DocumentNotFoundException(documentId);
        }

        private void Save(string errorMessage)
        {
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                throw new DataAccessException(errorMessage, ex);
            }
        }
    }
}
