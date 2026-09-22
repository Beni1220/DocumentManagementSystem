using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Model.DTO
{
    public class DTODocument
    {
        public string FileName { get; set; } = default!;
        //public string ContentType { get; set; } = default!;
        //public long SizeInBytes { get; set; }
        public string? Description { get; set; }
    }
}
