using DocumentManagementSystem.Storage.Exceptions;
using DocumentManagementSystem.Storage.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace DocumentManagementSystem.BusinessLogic.Storage
{
    public class MinioFileStorage : IFileStorage
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MinioFileStorage> _logger;

        public MinioFileStorage(IConfiguration configuration, ILogger<MinioFileStorage> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task UploadAsync(string objectName, Stream content, long size, string contentType)
        {
            try
            {
                string bucketName = _configuration["Minio:BucketName"]!;
                using var client = CreateClient();

                bool exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
                if (!exists)
                {
                    await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                }

                await client.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(content)
                    .WithObjectSize(size)
                    .WithContentType(contentType));

                _logger.LogInformation("File {ObjectName} stored in bucket {Bucket}", objectName, bucketName);
            }
            catch (Exception ex)
            {
                throw new StorageException($"Could not store file '{objectName}'.", ex);
            }
        }

        public async Task DeleteAsync(string objectName)
        {
            try
            {
                string bucketName = _configuration["Minio:BucketName"]!;
                using var client = CreateClient();

                await client.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName));

                _logger.LogInformation("File {ObjectName} removed from bucket {Bucket}", objectName, bucketName);
            }
            catch (Exception ex)
            {
                throw new StorageException($"Could not delete file '{objectName}'.", ex);
            }
        }

        public async Task<Stream> DownloadAsync(string objectName)
        {
            try
            {
                string bucketName = _configuration["Minio:BucketName"]!;
                using var client = CreateClient();
                var content = new MemoryStream();
                await client.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    // Mit zwei Parametern wird der asynchrone Callback abgewartet.
                    .WithCallbackStream((stream, cancellationToken) =>
                        stream.CopyToAsync(content, cancellationToken)));
                content.Position = 0;
                return content;
            }
            catch (Exception ex)
            {
                throw new StorageException($"Could not retrieve file '{objectName}'.", ex);
            }
        }

        private IMinioClient CreateClient()
        {
            return new MinioClient()
                .WithEndpoint(_configuration["Minio:Endpoint"]!)
                .WithCredentials(_configuration["Minio:AccessKey"]!, _configuration["Minio:SecretKey"]!)
                .WithSSL(false)
                .Build();
        }
    }
}
