using Agape.Auctions.Functions.Cars.Images.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Agape.Auctions.Functions.Cars.Images.Utilities
{
    public static class StorageHelper
    {
        public static async Task<bool> UploadFileToStorage(Stream fileStream, string fileName, string carId,
                                                            AzureStorageConfig storageConfig)
        {
            // Create a URI to the blob
            var blobUri = new Uri("https://" + storageConfig.AccountName +
                                  ".blob.core.windows.net/" + storageConfig.ImageContainer +
                                  "/" + carId + "/" + fileName);

            var storageCredentials = new StorageSharedKeyCredential(storageConfig.AccountName, storageConfig.AccountKey);

            var blobClient = new BlobClient(blobUri, storageCredentials);

            // Upload the file
            await blobClient.UploadAsync(fileStream);

            return await Task.FromResult(true);
        }
    }
}
