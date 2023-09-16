using Agape.Auctions.UI.Cars.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AgapeModelCar = Agape.Auctions.Models.Cars;
using AgapeModelImage = Agape.Auctions.Models.Images;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Text;
using AgapeAPI.Core;
using Agape.Auctions.UI.Cars.Models;

namespace Agape.Auctions.UI.Cars.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly ILogger<PurchaseController> _logger;
        private readonly IConfiguration configure;
        private readonly string apiBaseUrlCar;
        private readonly string apiBaseUrlCarImage;
        private readonly string defaultCarImageUrl;
        private readonly int carAdvanceAmountPercentage;
        private readonly int carTaxPercentage;
        private readonly int purchasePageExpiryTime;
        private readonly int advanceThresholdAmount;
        private readonly string[] paymentProgressStatus = { "Hold", "Sold", "PaymentPending" };

        private LogHelper logHelper;

        public PurchaseController(IConfiguration configuration, ILogger<PurchaseController> logger)
        {
            configure = configuration;
            apiBaseUrlCar = configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            carAdvanceAmountPercentage = configure.GetValue<int>("CarAdvanceAmountPercentage");
            // carTaxPercentage = configure.GetValue<int>("CarTaxPercentage");
            purchasePageExpiryTime = configure.GetValue<int>("PurchasePageExpiryTime");
            advanceThresholdAmount = configure.GetValue<int>("AdvanceThresholdAmount");
            _logger = logger;
            logHelper = new LogHelper(configure, _logger);
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Summary(string carId)
        {
            ViewBag.Title = "Purchase Summary";
            ViewBag.Description = "Purchase Summary";
            ViewBag.Keywords = "Purchase Summary";

            ViewBag.CarAdvanceAmountPercentage = carAdvanceAmountPercentage;
            //  ViewBag.CarTaxPercentage = carTaxPercentage;
            ViewBag.PurchasePageExpiryTime = purchasePageExpiryTime;
            ViewBag.AdvanceThresholdAmount = advanceThresholdAmount;

            var car = new AgapeModelCar.Car();
            try
            {
                if (!string.IsNullOrEmpty(carId))
                    car = await GetCarDetails(carId);
                string defaultImageUrl = string.Empty;
                if (car != null)
                {
                    var lstImages = await GetCarImageByOnwer(carId);
                    if (lstImages != null && lstImages.Any())
                    {
                        var defaultImage = lstImages.Where(i => i.Order == 1);
                        if (defaultImage != null && defaultImage.Any())
                        {
                            defaultImageUrl = defaultImage.FirstOrDefault().Url;
                        }
                    }
                    if (string.IsNullOrEmpty(defaultImageUrl))
                        defaultImageUrl = defaultCarImageUrl;

                    car.Thumbnail = defaultImageUrl;

                    //If the current car status belongs to any on of the payment progress then redirect to home
                    if (paymentProgressStatus.Contains(car.Status))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    //Update the car table status to "Hold"
                    UpdateCarStatus(carId, "Hold");
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(car);
        }

        public async Task<AgapeModelCar.Car> GetCarDetails(string id)
        {
            var carDetails = new AgapeModelCar.Car();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlCar + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carDetails = await Response.Content.ReadAsAsync<AgapeModelCar.Car>();
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

        public async Task<List<AgapeModelImage.Image>> GetCarImageByOnwer(string id)
        {
            var carImages = new List<AgapeModelImage.Image>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlCarImage + "FindImagesByUser/" + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carImages = await Response.Content.ReadAsAsync<List<AgapeModelImage.Image>>();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from CarImage Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return carImages;
        }

        public async void UpdateCarStatus(string carId, string status)
        {
            try
            {
                AgapeModelCar.Car car = await GetCarDetails(carId);
                if (car != null)
                {
                    car.Status = status;
                    using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                    StringContent content = new StringContent(JsonConvert.SerializeObject(car), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlCar + car.Id;

                    using (var Response = await client.PutAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
                        }
                    }
                }
                else
                {
                    logHelper.LogError("Error while retreive the car, CarId : " + carId);
                }

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
        }
    }
}
