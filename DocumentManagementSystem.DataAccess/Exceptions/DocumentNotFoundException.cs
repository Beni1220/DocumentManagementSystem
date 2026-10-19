using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.DataAccess.Exceptions
{
    public class DocumentNotFoundException : DataAccessException
    {
        public DocumentNotFoundException(int documentId)
            : base($"Document with ID {documentId} not found.") { }
    }
}
