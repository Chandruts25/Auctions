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
using System.IO;
using System.Drawing.Imaging;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using Agape.Auctions.Models.Cars;
using System.Threading;
using Agape.Auctions.Functions.Cars.Payment.Utilities;

namespace Agape.Auctions.Functions.Cars.Payment
{
    public static class FunctionPayment
    {
        public static readonly string apiBaseUrlCar = Environment.GetEnvironmentVariable("WebAPIBaseUrlCar");
        public static readonly int HoldStatusCheckInterval = int.Parse(Environment.GetEnvironmentVariable("HoldStatusCheckInterval"));
        public static readonly int PaymentPendingStatusCheckInterval = int.Parse(Environment.GetEnvironmentVariable("PaymentPendingStatusCheckInterval"));
        public static readonly string subscriptionKey = Environment.GetEnvironmentVariable("SubscriptionKey");


        [FunctionName("FunctionPayment")]
        public static void Run([CosmosDBTrigger(databaseName: "Tasks", collectionName: "Car", ConnectionStringSetting = "CosmosDbConnectionstring")] IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                for (int i = 0; i < input.Count; i++)
                {
                    log.LogInformation("Documents modified " + input.Count);
                    log.LogInformation("Current document Id " + input[i].Id);
                    var doc = input[i].ToString();
                    Car carModel = JsonConvert.DeserializeObject<Car>(doc);
                    if (carModel.Status.Equals("Hold"))
                    {
                        CreateThredToMonitorHoldingPayment(input[i].Id, log);
                    }
                    else if(carModel.Status.Equals("PaymentPending"))
                    {
                        CreateThredToMonitorPendingPayment(input[i].Id, log);
                    }
                }
            }
        }

        public static async void CreateThredToMonitorHoldingPayment(string carId, ILogger log)
        {
            try
            {
                Thread.Sleep(HoldStatusCheckInterval * 60000);
                var carDetails = await GetCarDetails(carId, log);
                if (carDetails != null)
                {
                    if (carDetails.Status == "Hold")
                    {
                        UpdateCarStatus(carDetails, "open", log);
                    }
                }


            }
            catch (Exception ex)
            {
                log.LogError("Error on CreateThredToMonitorHoldingPayment, Detaild message : " + ex.ToString());
            }
        }

        public static async void CreateThredToMonitorPendingPayment(string carId, ILogger log)
        {
            try
            {
                Thread.Sleep(PaymentPendingStatusCheckInterval * 60000);
                var carDetails = await GetCarDetails(carId,log);
                if(carDetails != null)
                {
                    if(carDetails.Status == "PaymentPending")
                    {
                        UpdateCarStatus(carDetails, "open", log);
                    }
                }

            }
            catch (Exception ex)
            {
                log.LogError("Error on CreateThredToMonitorPendingPayment, Detaild message : " + ex.ToString());
            }
        }

        public static async Task<Car> GetCarDetails(string id, ILogger log)
        {
            var carDetails = new Car();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(subscriptionKey)))
                {
                    string endpoint = apiBaseUrlCar + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carDetails = await Response.Content.ReadAsAsync<Car>();
                        }
                        else
                        {
                            log.LogError(Response.ReasonPhrase + " " + "Error from Car Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
            return carDetails;
        }

        public static async void UpdateCarStatus(Car car, string status, ILogger log)
        {
            try
            {
                if (car != null)
                {
                    car.Status = status;
                    using HttpClient client = new HttpClient(new CustomHttpClientHandler(subscriptionKey));
                    StringContent content = new StringContent(JsonConvert.SerializeObject(car), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlCar + car.Id;

                    using (var Response = await client.PutAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                        }
                        else
                        {
                            log.LogError(Response.ReasonPhrase + " Error from Car Service");
                        }
                    }
                }
                else
                {
                    log.LogError("Error while retreive the car, CarId : " + car.Id);
                }

            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }


    }
}
