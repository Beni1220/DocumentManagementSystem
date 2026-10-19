using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.DataAccess.Exceptions
{
    public class DataAccessException : Exception
    {
        public DataAccessException(string message) : base(message) { }

        public DataAccessException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
