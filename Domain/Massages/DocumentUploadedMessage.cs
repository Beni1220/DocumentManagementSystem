using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Massages
{
    public class DocumentUploadedMessage
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = default!;
    }
}
