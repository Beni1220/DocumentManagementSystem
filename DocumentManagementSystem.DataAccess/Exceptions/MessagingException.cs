using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.DataAccess.Exceptions
{
    public class MessagingException : Exception
    {
        public MessagingException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
