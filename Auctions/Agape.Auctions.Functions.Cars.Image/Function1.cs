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
//using Agape.Auctions.Functions.Cars.Images.Utilities;
using System.IO;
using System.Drawing.Imaging;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using AgapeModelImage = Agape.Auctions.Models.Images;


namespace Agape.Auctions.Functions.Cars.Image
{
    public static class Function1
    {
        public static readonly string apiBaseUrlCarImage = Environment.GetEnvironmentVariable("WebAPIBaseUrlCarImage");

        //, LeaseCollectionName = "leases", CreateLeaseCollectionIfNotExists = true
        [FunctionName("Function1")]
        public static void Run([CosmosDBTrigger(databaseName: "Tasks", collectionName: "Image", ConnectionStringSetting = "CosmosDbConnectionstring", LeaseCollectionName = "leases")] IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                for (int i = 0; i < input.Count; i++)
                {
                    log.LogInformation("Documents modified " + input.Count);
                    log.LogInformation("First document Id " + input[i].Id);
                   // GetImageandCreateThumbnails(input[i].Id, log);
                }
            }
        }
    }
}
