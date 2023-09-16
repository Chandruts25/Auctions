using Agape.Auctions.UI.Cars.Admin.Utilities;
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
using AgapeModelOffer = Agape.Auctions.Models.Offers;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Text;
using AgapeAPI.Core;
using Agape.Auctions.UI.Cars.Admin.Models;
using System.IO;
using System.Security.Claims;

namespace Agape.Auctions.UI.Cars.Admin.Controllers
{
    public class OffersController : Controller
    {
        private readonly ILogger<OffersController> _logger;
        private readonly IConfiguration configure;
        private readonly string apiBaseUrlCar;
        private readonly string apiBaseUrlCarImage;
        private readonly string defaultCarImageUrl;
        private readonly string apiBaseUrlOffers;
        private LogHelper logHelper;

        public OffersController(IConfiguration configuration, ILogger<OffersController> logger)
        {
            configure = configuration;
            apiBaseUrlCar = configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlOffers = configure.GetValue<string>("WebAPIBaseUrlOffers");
            _logger = logger;
            logHelper = new LogHelper(configure, _logger);
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Summary(string carId)
        {
            ViewBag.Title = "Offer Summary";
            ViewBag.Description = "Offer Summary";
            ViewBag.Keywords = "Offer Summary";

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
                            defaultImageUrl = defaultImage.FirstOrDefault().MediumUrl;
                        }
                    }
                    if (string.IsNullOrEmpty(defaultImageUrl))
                        defaultImageUrl = defaultCarImageUrl;

                    car.Thumbnail = defaultImageUrl;
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

        public async Task<JsonResult> AddOfferDetails()
        {
            try
            {
                var dealerDetails = Request.Form["offerDetails"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var offerDetails = (AgapeModelOffer.Offer)jsonSerializer.Deserialize(reader, typeof(AgapeModelOffer.Offer));
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                offerDetails.CreatedBy = userId;
                offerDetails.CreatedDate = DateTime.Now;
                StringContent content = new StringContent(JsonConvert.SerializeObject(offerDetails), Encoding.UTF8, "application/json");

                //Save to offer details table implementation
                var endpoint = apiBaseUrlOffers;
                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Offer Service");
                        return Json(new { result = false });
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false });
            }
        }
    }
}
