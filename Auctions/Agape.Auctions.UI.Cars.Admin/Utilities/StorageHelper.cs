using Agape.Auctions.UI.Cars.Admin.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace Agape.Auctions.UI.Cars.Admin.Utilities
{
    public static class StorageHelper
    {
        //Validate the image format
        public static bool IsImage(IFormFile file)
        {
            if (file.ContentType.Contains("image"))
            {
                return true;
            }

            string[] formats = new string[] { ".jpg", ".png", ".gif", ".jpeg" };

            return formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        //Upload the image to blob storage
        public static async Task<bool> UploadFileToStorage(Stream fileStream, string fileName,string carId,
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

        public static async Task<bool> RemoveFileFromStorage(List<string> fileNames, string carId,
                                                           AzureStorageConfig storageConfig)
        {
            var finalResult = true;
            //foreach(var imageFileName in fileNames)
            //{
            //    // Create a URI to the blob
            //    var blobUri = new Uri("https://" + storageConfig.AccountName +
            //                          ".blob.core.windows.net/" + storageConfig.ImageContainer +
            //                          "/" + carId + "/" + imageFileName);

            //    var storageCredentials = new StorageSharedKeyCredential(storageConfig.AccountName, storageConfig.AccountKey);

            //    var blobClient = new BlobClient(blobUri, storageCredentials);
            //    // Delete the file
            //    var response = blobClient.DeleteIfExists();
            //    if (!response.Value)
            //        finalResult = false;
            //}
            return finalResult;
        }
    }
}
