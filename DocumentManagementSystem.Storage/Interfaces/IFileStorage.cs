using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.Storage.Interfaces
{
    public interface IFileStorage
    {
        Task UploadAsync(string objectName, Stream content, long size, string contentType);
        Task DeleteAsync(string objectName);
    }
}
