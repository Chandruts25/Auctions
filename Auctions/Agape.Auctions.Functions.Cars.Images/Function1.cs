using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Linq;
using Agape.Auctions.Functions.Cars.Images.Utilities;
using System.IO;
using System.Drawing.Imaging;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using Agape.Auctions.Functions.Cars.Images.Models;

namespace Agape.Auctions.Functions.Cars.Images
{
    public static class Function1
    {
        public static readonly string apiBaseUrlCarImage = Environment.GetEnvironmentVariable("WebAPIBaseUrlCarImage");

        //, LeaseCollectionName = "leases", CreateLeaseCollectionIfNotExists = true
        [FunctionName("Function1")]
        public static void Run([CosmosDBTrigger(databaseName: "Tasks", collectionName: "Image", ConnectionStringSetting = "CosmosDbConnectionstring")] IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                for (int i = 0; i < input.Count; i++)
                {
                    log.LogInformation("Documents modified " + input.Count);
                    log.LogInformation("Current document Id " + input[i].Id);
                    var doc = input[i].ToString();
                    CarImage imageModel = JsonConvert.DeserializeObject<CarImage>(doc);
                    if (imageModel.Type.Equals("image"))
                    {
                        GetImageandCreateThumbnails(input[i].Id, log);
                    }
                }
            }
        }

        public static async void GetImageandCreateThumbnails(string id, ILogger log)
        {
            try
            {
                var currentImageFormat = string.Empty;
                var currentImageName = string.Empty;
                var currentImageDetails = await GetCarImageById(id, log);
                var currentImageUrl = currentImageDetails.Url;
                var imageName = currentImageUrl.Split("/");
                if (imageName.Length > 0)
                {
                    var imageDetail = imageName[imageName.Length - 1];
                    if (imageDetail != null)
                    {
                        var imageData = imageDetail.Split('.');
                        if (imageData != null && imageData.Length > 0)
                        {
                            currentImageName = imageData[0];
                            currentImageFormat = imageData[1];
                        }
                    }
                }

                var img = GetImageFromUrl(currentImageUrl);
                var imageFormat = GetImageFormat(currentImageFormat);
                var storageConfig = new AzureStorageConfig
                {
                    AccountKey = Environment.GetEnvironmentVariable("StorageAccountKey"),
                    AccountName = Environment.GetEnvironmentVariable("StorageAccountName"),
                    ImageContainer = Environment.GetEnvironmentVariable("StorageContainer")
                };
                CreateThumbnailImages(img, imageFormat, currentImageName + "_tn." + currentImageFormat, currentImageDetails.Owner, 121, 90, log, storageConfig);
                CreateThumbnailImages(img, imageFormat, currentImageName + "_small." + currentImageFormat, currentImageDetails.Owner, 270, 150, log, storageConfig);
                CreateThumbnailImages(img, imageFormat, currentImageName + "_listing." + currentImageFormat, currentImageDetails.Owner, 322, 230, log, storageConfig);
                CreateThumbnailImages(img, imageFormat, currentImageName + "_grid." + currentImageFormat, currentImageDetails.Owner, 260, 230, log, storageConfig);
                CreateThumbnailImages(img, imageFormat, currentImageName + "_medium." + currentImageFormat, currentImageDetails.Owner, 842, 551, log, storageConfig);

                currentImageDetails.ThumbnailUrl = "https://" + storageConfig.AccountName + ".blob.core.windows.net/" + storageConfig.ImageContainer + "/" + currentImageDetails.Owner + "/" + currentImageName + "_tn." + currentImageFormat;
                currentImageDetails.SmallUrl = "https://" + storageConfig.AccountName + ".blob.core.windows.net/" + storageConfig.ImageContainer + "/" + currentImageDetails.Owner + "/" + currentImageName + "_small." + currentImageFormat;
                currentImageDetails.Listing = "https://" + storageConfig.AccountName + ".blob.core.windows.net/" + storageConfig.ImageContainer + "/" + currentImageDetails.Owner + "/" + currentImageName + "_listing." + currentImageFormat;
                currentImageDetails.ListingGrid = "https://" + storageConfig.AccountName + ".blob.core.windows.net/" + storageConfig.ImageContainer + "/" + currentImageDetails.Owner + "/" + currentImageName + "_grid." + currentImageFormat;
                currentImageDetails.MediumUrl = "https://" + storageConfig.AccountName + ".blob.core.windows.net/" + storageConfig.ImageContainer + "/" + currentImageDetails.Owner + "/" + currentImageName + "_medium." + currentImageFormat;

                await SaveCarImages(currentImageDetails,log);

            }
            catch (Exception ex)
            {
                log.LogError("Error on GetImageandCreateThumbnails, Detaild message : " + ex.ToString());
            }
        }

        public static async void CreateThumbnailImages(Image image, ImageFormat imgFormat, string newImageName, string carId, int imageWidth, int imageHeight, ILogger log, AzureStorageConfig storageConfig)
        {
            try
            {
                //Image image = Image.FromFile(imagePath);
                Image thumbImage = image.GetThumbnailImage(imageWidth, imageHeight, () => false, IntPtr.Zero);
                var uploadResult = await StorageHelper.UploadFileToStorage(thumbImage.ToStream(imgFormat), newImageName, carId, storageConfig);
            }
            catch (Exception ex)
            {
                log.LogError("Error on CreateThumbnailImages, Detaild message : " + ex.ToString());
            }
        }
        public static ImageFormat GetImageFormat(string format)
        {
            ImageFormat imgFormat;
            switch (format)
            {
                case "jpg":
                    imgFormat = ImageFormat.Jpeg;
                    break;
                case "png":
                    imgFormat = ImageFormat.Png;
                    break;
                case "gif":
                    imgFormat = ImageFormat.Gif;
                    break;
                default:
                    imgFormat = ImageFormat.Jpeg;
                    break;
            }
            return imgFormat;
        }
        public static Image GetImageFromUrl(string imageUrl)
        {
            System.Drawing.Image img;
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData(imageUrl);
                var stream = new MemoryStream(data);
                img = System.Drawing.Image.FromStream(stream);
            }
            return img;
        }
        public static Stream ToStream(this Image image, ImageFormat format)
        {
            var stream = new System.IO.MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;
            return stream;
        }
        public static async Task<CarImage> GetCarImageById(string id, ILogger log)
        {
            var carImage = new CarImage();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler()))
                {
                    string endpoint = apiBaseUrlCarImage + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carImage = await Response.Content.ReadAsAsync<CarImage>();
                        }
                        else
                        {
                            log.LogError(Response.ReasonPhrase + " " + "Error from Car Image Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError("Error on GetCarImageById, Detaild message : " + ex.ToString());
            }
            return carImage;
        }

        public static async Task<(bool, string)> SaveCarImages(CarImage carImage, ILogger log)
        {
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler());
                StringContent content = new StringContent(JsonConvert.SerializeObject(carImage), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlCarImage + carImage.Id;

                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return (true, "success");
                    }
                    else
                    {
                        log.LogError(Response.ReasonPhrase + " " + "Error from Car Image Service");
                        return (false, "Error while saving the image details");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError("Error while saving the image detail : " + ex.ToString());
                return (false, $"Error while saving the image details, Details: {ex.Message }");
            }
        }
    }
}
