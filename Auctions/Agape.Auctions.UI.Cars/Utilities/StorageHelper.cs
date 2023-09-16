using Agape.Auctions.UI.Cars.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Storage;

namespace Agape.Auctions.UI.Cars.Utilities
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

        public static async Task<string> UploadFileToStorage(IFormFile file, FireBaseStorageConfig config)
        {
            string filePath = "";
            var auth = new FirebaseAuthProvider(new FirebaseConfig(config.apiKey));
            var authResult = await auth.SignInWithEmailAndPasswordAsync(config.authEmail, config.authPassword);

            var storage = new FirebaseStorage(
            config.bucket,
            new FirebaseStorageOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(authResult.FirebaseToken),
                ThrowOnCancel = true
            });

            string folderName = "CarImages";
            string fileName = Path.GetFileName(file.FileName);

            // Upload the file to Firebase Storage
            using (var stream = file.OpenReadStream())
            {
                filePath = await storage
                    .Child(folderName)
                    .Child(fileName)
                    .PutAsync(stream)
;
            }
            return filePath;
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
