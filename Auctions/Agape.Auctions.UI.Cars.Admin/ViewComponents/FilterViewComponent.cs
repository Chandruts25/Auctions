using Agape.Auctions.UI.Cars.Admin.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using AgapeModel = DataAccessLayer.Models;
using AgapeModelImage = DataAccessLayer.Models;
using Agape.Auctions.UI.Cars.Admin.Utilities;
using Microsoft.Extensions.Logging;
using AgapeModelUser = DataAccessLayer.Models;
using AgapeModelCar = DataAccessLayer.Models;

using AgapeModelPayment = DataAccessLayer.Models;
using AgapeModelPurchase = DataAccessLayer.Models;
using AgapeModelOffer = DataAccessLayer.Models;
using Model = Agape.Auctions.UI.Cars.Admin.Models;
using ModelAuctions = DataAccessLayer.Models;
using AgapeModelBid = DataAccessLayer.Models;
namespace Agape.Auctions.UI.Cars.Admin.ViewComponents
{
    public class FilterViewComponent : ViewComponent
    {
        private readonly IConfiguration _configure;

        private readonly string apiBaseUrlDealer;
        private readonly string apiBaseUrlUser;
        private readonly string apiBaseUrlCar;
        private readonly string apiBaseUrlCarImage;
        private readonly string apiBaseUrlPayment;
        private readonly string defaultCarImageUrl;
        private readonly string apiBaseUrlOffers;
        private readonly string apiBaseUrlAuction;
        private readonly string apiBaseUrlBidding;
        private readonly string apiBaseUrlPurchase;
        private readonly string apiBaseUrlVin;
        private readonly string closedStatus = "Closed";
        private readonly ILogger<FilterViewComponent> _logger;
        private LogHelperComponent logHelper;

        // private readonly string apiBaseUrlCarSearch;

        public FilterViewComponent(IConfiguration configuration, ILogger<FilterViewComponent> logger)
        {
            _logger = logger;
            _configure = configuration;
            apiBaseUrlDealer = _configure.GetValue<string>("WebAPIBaseUrlDealer");
            apiBaseUrlUser = _configure.GetValue<string>("WebAPIBaseUrlUser");
            apiBaseUrlCar = _configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = _configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = _configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlVin = _configure.GetValue<string>("WebAPIBaseUrlVin");
            apiBaseUrlPayment = _configure.GetValue<string>("WebAPIBaseUrlPayment");
            apiBaseUrlPurchase = _configure.GetValue<string>("WebAPIBaseUrlPurchase");
            apiBaseUrlOffers = _configure.GetValue<string>("WebAPIBaseUrlOffers");
            apiBaseUrlAuction = _configure.GetValue<string>("WebAPIBaseUrlAuction");
            apiBaseUrlBidding = _configure.GetValue<string>("WebAPIBaseUrlBidding");
            defaultCarImageUrl = _configure.GetValue<string>("DefaultCarImageUrl");
            logHelper = new LogHelperComponent(_configure, _logger);
            // apiBaseUrlCarSearch = configure.GetValue<string>("WebAPIBaseUrlCarSearch");

        }
        public async Task<IViewComponentResult> InvokeAsync(string status = "Submitted")
        {



            var Car = new List<AgapeModelCar.Car>();


            try
            {

                var carDetails = await GetAllCarDetailsDealer(status);
                Car = carDetails;




            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View("FilterList", Car);

        }
        public async Task<List<AgapeModelCar.Car>> GetAllCarDetailsDealer(string status)
        {

            var CarDetails = new List<AgapeModelCar.Car>();
            var lstCarImages = await GetAllCarImages();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlCar + "FindCarsByStatus/" + status; ;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            CarDetails = await Response.Content.ReadAsAsync<List<AgapeModelCar.Car>>();
                            if (CarDetails != null && CarDetails.Any())
                            {
                                foreach (var car in CarDetails)
                                {
                                    var carImage = lstCarImages.Where(i => i.Owner == car.Id);
                                    if (carImage != null && carImage.Any())
                                    {
                                        car.Thumbnail = (carImage.Where(i => i.Order == 1) != null && carImage.Where(i => i.Order == 1).Any()) ? carImage.Where(i => i.Order == 1).Select(i => i.Url).FirstOrDefault().ToString() : carImage.FirstOrDefault().Url;
                                    }
                                    else
                                    {
                                        car.Thumbnail = defaultCarImageUrl;
                                    }
                                    if (car.Video == null)
                                        car.Video = new AgapeModelCar.Video();
                                }
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Image Service");
                            return CarDetails;
                        }
                    }
                }
                return CarDetails;
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return CarDetails;
            }
        }
        public async Task<List<AgapeModelImage.Image>> GetAllCarImages()
        {
            var carImages = new List<AgapeModelImage.Image>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlCarImage;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carImages = await Response.Content.ReadAsAsync<List<AgapeModelImage.Image>>();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Image Service");
                            return carImages;
                        }
                    }
                }
                return carImages;
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return carImages;
            }
        }
    }
}
