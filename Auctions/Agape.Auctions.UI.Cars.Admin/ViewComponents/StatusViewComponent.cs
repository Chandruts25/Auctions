using Agape.Auctions.UI.Cars.Admin.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using AgapeModel = Agape.Auctions.Models.Cars;
using AgapeModelImage = Agape.Auctions.Models.Images;
using Agape.Auctions.UI.Cars.Admin.Utilities;
using Microsoft.Extensions.Logging;
using AgapeModelUser = Agape.Auctions.Models.Users;
using AgapeModelCar = Agape.Auctions.Models.Cars;

using AgapeModelPayment = Agape.Auctions.Models.PaymentMethods;
using AgapeModelPurchase = Agape.Auctions.Models.Puchases;
using AgapeModelOffer = Agape.Auctions.Models.Offers;
using Model = Agape.Auctions.UI.Cars.Admin.Models;
using ModelAuctions = Agape.Auctions.Models.Auctions;
using AgapeModelBid = Agape.Auctions.Models.Biddings;

namespace Agape.Auctions.UI.Cars.Admin.ViewComponents
{
    public class StatusViewComponent : ViewComponent
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
        private readonly ILogger<StatusViewComponent> _logger;
        private LogHelperComponent logHelper;

        // private readonly string apiBaseUrlCarSearch;

        public StatusViewComponent(IConfiguration configuration, ILogger<StatusViewComponent> logger)
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
        public async Task<IViewComponentResult> InvokeAsync(string brand, string model, string fuel, string year, string status = "Submitted")
        {
            var Car = new List<AgapeModelCar.Car>();
            try
            {
                if (status == "All")
                {
                    var carDetails = await GetAllCarDetails();
                    Car = carDetails;
                }
                else
                {
                    var carDetails = await GetAllCarDetailsDealer(status);
                    Car = carDetails;
                }
                if(Car != null && Car.Any())
                {
                    if (!(string.IsNullOrEmpty(brand)) && brand != "all")
                    {
                        var lstCar = Car.Where(i => i.Make == brand);
                            Car = lstCar.ToList();
                    }
                    if (Car != null && Car.Any() && !(string.IsNullOrEmpty(model)) && model != "all")
                    {
                            var lstCar = Car.Where(i => i.Model == model);
                                Car = lstCar.ToList();
                    }
                    if (Car != null && Car.Any() && !(string.IsNullOrEmpty(fuel)) && fuel != "all")
                    {
                        var lstCar = Car.Where(i => i.IsPetrol == false);
                            Car = lstCar.ToList();
                    }
                    if (Car != null && Car.Any() && !(string.IsNullOrEmpty(year)) && year != "all")
                    {
                        var lstCar = Car.Where(i => i.Year == int.Parse(year));
                            Car = lstCar.ToList();
                    }
                }
               
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View("StatusList", Car);

        }
        public async Task<List<AgapeModelCar.Car>> GetAllCarDetailsDealer(string status)
        {
            logHelper.LogInformation("Method Name : GetAllCarDetailsDealer, Status :" + status);
            var CarDetails = new List<AgapeModelCar.Car>();
            var lstCarImages = await GetAllCarImages();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlCar + "FindCarsByStatus/" + status;
                    logHelper.LogInformation("Method Name : GetAllCarDetailsDealer, EndPoint :" + endpoint);
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            CarDetails = await Response.Content.ReadAsAsync<List<AgapeModelCar.Car>>();

                            logHelper.LogInformation("Method Name : GetAllCarDetailsDealer, CarDetails Count :" + CarDetails.Count());

                            if (CarDetails != null && CarDetails.Any())
                            {
                                foreach (var car in CarDetails)
                                {
                                    logHelper.LogInformation("Method Name : GetAllCarDetailsDealer, Car Loop, Current Car :" + car.Id);

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
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
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
        public async Task<List<AgapeModelCar.Car>> GetAllCarDetails()
        {
            logHelper.LogInformation("Method Name : GetAllCarDetails");
            var CarDetails = new List<AgapeModelCar.Car>();
            var lstCarImages = await GetAllCarImages();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlCar ;
                    logHelper.LogInformation("Method Name : GetAllCarDetails, EndPoint :" + endpoint);
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            CarDetails = await Response.Content.ReadAsAsync<List<AgapeModelCar.Car>>();

                            logHelper.LogInformation("Method Name : GetAllCarDetails, CarDetails Count :" + CarDetails.Count());

                            if (CarDetails != null && CarDetails.Any())
                            {
                                foreach (var car in CarDetails)
                                {
                                    logHelper.LogInformation("Method Name : GetAllCarDetailsDealer, Car Loop, Current Car :" + car.Id);

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
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
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
            logHelper.LogInformation("Method Name : GetAllCarImages (Parent Method : GetAllCarDetailsDealer)");
            var carImages = new List<AgapeModelImage.Image>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlCarImage;
                    logHelper.LogInformation("Method Name : GetAllCarImages (Parent Method : GetAllCarDetailsDealer), EndPoint :" +endpoint);

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carImages = await Response.Content.ReadAsAsync<List<AgapeModelImage.Image>>();
                            logHelper.LogInformation("Method Name : GetAllCarImages (Parent Method : GetAllCarDetailsDealer), carImages Count :" + carImages.Count());
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
