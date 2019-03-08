using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LyricRobotCommon
{
    public static class BlobRepository<T> where T : class
    {
        public static async Task Create(string id, T data)
        {
            var connectionString = BlobRepositorySettings.ConnectionString;

            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();
                var cloudBlobContainer = cloudBlobClient.GetContainerReference("lyricrobot");

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

                // Get a reference to the blob address, then upload the file to the blob.
                // Use the value of localFileName for the blob name.
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(id);
                await cloudBlockBlob.UploadTextAsync(json);
            }
            else
            {
                throw new InvalidOperationException($"Invalid connections string {connectionString}");
            }
        }

        public static async Task<Stream> GetAsStream(string id)
        {
            var connectionString = BlobRepositorySettings.ConnectionString;

            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();
                var cloudBlobContainer = cloudBlobClient.GetContainerReference("lyricrobot");


                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(id);

                var ms = new MemoryStream();
                
                await cloudBlockBlob.DownloadToStreamAsync(ms);
                return ms;                
            }
            else
            {
                throw new InvalidOperationException($"Invalid connections string {connectionString}");
            }
        }

        public static async Task UploadFromStream(MemoryStream stream, string id)
        {
            var connectionString = BlobRepositorySettings.ConnectionString;

            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();
                var cloudBlobContainer = cloudBlobClient.GetContainerReference("lyricrobot");

                // Get a reference to the blob address, then upload the file to the blob.
                // Use the value of localFileName for the blob name.
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(id);

                stream.Position = 0;
                await cloudBlockBlob.UploadFromStreamAsync(stream);
            }
            else
            {
                throw new InvalidOperationException($"Invalid connections string {connectionString}");
            }
        }

        public static async Task<T> Get(string id)
        {
            var connectionString = BlobRepositorySettings.ConnectionString;

            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();
                var cloudBlobContainer = cloudBlobClient.GetContainerReference("lyricrobot");


                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(id);
                var data = await cloudBlockBlob.DownloadTextAsync();

                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);

                return obj;
            }
            else
            {
                throw new InvalidOperationException($"Invalid connections string {connectionString}");
            }
        }
    }
}
