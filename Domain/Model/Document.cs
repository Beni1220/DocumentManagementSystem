using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Model
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; } = default!;
        //public string ContentType { get; set; } = default!;
        //public long SizeInBytes { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
