using Agape.Auctions.Functions.Cars.BidScheduler.Models;
using Agape.Auctions.Functions.Cars.BidScheduler.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Agape.Auctions.Functions.Cars.BidScheduler
{
    public static class BidSchedulerFunction
    {
        public static readonly string apiBaseUrlCar = Environment.GetEnvironmentVariable("WebAPIBaseUrlCar");
        public static readonly string apiBaseUrlAuction = Environment.GetEnvironmentVariable("WebAPIBaseUrlAuction");
        public static readonly string subscriptionKey = Environment.GetEnvironmentVariable("SubscriptionKey");
        public static readonly string apiBaseUrlVin = Environment.GetEnvironmentVariable("WebAPIBaseUrlVin");
        public static readonly string apiBaseUrlBidding = Environment.GetEnvironmentVariable("WebAPIBaseUrlBidding");

        //public static void Run([TimerTrigger("0 0 1 * * * ")]TimerInfo myTimer, ILogger log)s
        //public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        [FunctionName("BidSchedulerFunction")]
        public static void Run([TimerTrigger("0 0 1 * * *")]TimerInfo myTimer, ILogger log)
        {
            UpdateCarAfterBidTimeOver(log);
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        public static async void UpdateCarAfterBidTimeOver(ILogger logHelper)
        {
            var lstApprovedCars = await GetAuctionApprovedCars(logHelper);

            var expiredCars = lstApprovedCars.Where(i => i.ApprovedDate.AddDays(i.AuctionDays).Date <= DateTime.Now.Date);
            if(expiredCars != null && expiredCars.Any())
            {
                foreach(var car in expiredCars)
                {
                    if(!(string.IsNullOrEmpty(car.CarId)))
                    {
                        var changeStatus = "Sold";
                        decimal highestBid = await GetHighestBidDetails(car.CarId,logHelper);
                        if (car.Reserve > highestBid)
                            changeStatus = "UnSold";

                        await UpdateCar(car.CarId, changeStatus, logHelper);
                        await UpdateAuctionStatus(car, logHelper, changeStatus);
                    }
                   
                }
            }
        }

        public static async Task<bool> UpdateAuctionStatus(Auction auctionModel, ILogger logHelper, string status)
        {
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(subscriptionKey)))
                {
                    auctionModel.Status = status;
                    var endpoint = apiBaseUrlAuction + auctionModel.Id;
                    StringContent content = new StringContent(JsonConvert.SerializeObject(auctionModel), Encoding.UTF8, "application/json");
                    using (var Response = await client.PutAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return true;
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Auction Service");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return false;
            }
        }

        public static async Task<List<Auction>> GetAuctionApprovedCars(ILogger logHelper)
        {
            var lstCars = new List<Auction>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(subscriptionKey)))
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<Auction>>();
                            if (response != null && response.Any())
                            {
                                var approvedCars = response.Where(i => i.Status == "Approved");
                                if (approvedCars != null && approvedCars.Any())
                                    lstCars = approvedCars.ToList();
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from Auction Service");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return lstCars;
        }

        public static async Task<bool> UpdateCar(string carId, string status, ILogger logHelper)
        {
            try
            {
                var carDetails = await GetCarDetails(carId, logHelper);


                //Get VehicleDetails from API
                var definition = new { result = false, errorMessage = "", carProperties = new CarProperties() };
                var apiResponse = await GetVehicleInformation(carDetails.Vin, logHelper);
                var deResponse = JsonConvert.DeserializeAnonymousType(JsonConvert.SerializeObject(apiResponse.Value), definition);

                var tempProperty = carDetails.Properties;
                deResponse.carProperties.Trim = tempProperty.Trim;
                if (string.IsNullOrEmpty(deResponse.errorMessage))
                {
                    carDetails.Properties = deResponse.carProperties;
                    carDetails.Status = status;

                    using HttpClient client = new HttpClient(new CustomHttpClientHandler(subscriptionKey));
                    StringContent content = new StringContent(JsonConvert.SerializeObject(carDetails), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlCar + carDetails.Id;

                    using (var Response = await client.PutAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return true;
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
                            return false;
                        }
                    }
                }
                else
                {
                    logHelper.LogError(deResponse.errorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return false;
            }
        }

        public static async Task<decimal> GetHighestBidDetails(string carId, ILogger logHelper)
        {
            decimal bid = 0.0M;
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(subscriptionKey)))
                {
                    string endpoint = apiBaseUrlBidding + "GetHighestBid/" + carId;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            bid = await Response.Content.ReadAsAsync<decimal>();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from bidding Service");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return bid;
        }

        public static async Task<Car> GetCarDetails(string id, ILogger logHelper)
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
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from Car Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return carDetails;
        }

        public static async Task<JsonResult> GetVehicleInformation(string vinDetail, ILogger logHelper)
        {
            var errorMessage = string.Empty;
            var properties = new CarProperties();
            try
            {
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(subscriptionKey)))
                {
                    string endpoint = apiBaseUrlVin + vinDetail + "?format=json";
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var result = Response.Content.ReadAsStringAsync();
                            var vechicleDetail = JsonConvert.DeserializeObject<VehicleDetails>(result.Result);
                            if (vechicleDetail != null && !(string.IsNullOrEmpty(vechicleDetail.Message)) && vechicleDetail.Results != null && vechicleDetail.Results.Length > 0)
                            {
                                if (vechicleDetail.Results[0].ErrorCode != "0")
                                {
                                    errorMessage = vechicleDetail.Results[0].ErrorText;
                                }
                                else
                                {
                                    properties = vechicleDetail.Results[0];
                                }
                            }
                            else
                            {
                                logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the vehicle Details");
                                errorMessage = "Error occurred while getting the vehicle Details Please try again later";
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the vehicle Details");
                            errorMessage = "Error occurred while getting the vehicle Details Please try again later";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return new JsonResult(new { returnValue = false });
            }
            return new JsonResult(new { result = (!(string.IsNullOrEmpty(errorMessage) ? false : true)), errorMessage = errorMessage, carProperties = properties });
        }

    }
}
