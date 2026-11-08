using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.Storage.Exceptions
{
    public class StorageException : Exception
    {
        public StorageException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
