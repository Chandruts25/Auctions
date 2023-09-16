using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using AgapeModel = Agape.Auctions.Models.Cars;
using AgapeModelImage = Agape.Auctions.Models.Images;
using Agape.Auctions.UI.Cars.Utilities;
using Microsoft.Extensions.Logging;
using AgapeModelUser = Agape.Auctions.Models.Users;
using AgapeModelCar = Agape.Auctions.Models.Cars;





using AgapeModelPayment = Agape.Auctions.Models.PaymentMethods;
using AgapeModelPurchase = Agape.Auctions.Models.Puchases;
using AgapeModelOffer = Agape.Auctions.Models.Offers;
using Model = Agape.Auctions.UI.Cars.Models;
using ModelAuctions = Agape.Auctions.Models.Auctions;
using AgapeModelBid = Agape.Auctions.Models.Biddings;





namespace Agape.Auctions.UI.Cars.ViewComponents
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
        public async Task<IViewComponentResult> InvokeAsync(string userId, string status = "All")
        {


            var Car = new List<AgapeModelCar.Car>();


            var carDetails = await GetCarDetailsDealer(userId);

            try
            {
                if (status == "All")
                {
                    foreach (var item in carDetails)
                    {
                        var bidDetails = await GetCarOfferCount(item.Id);
                        item.Mileage = bidDetails;
                        var bidCount = await GetBidCount(item.Id);
                        item.Version = bidCount.ToString();
                        var watchCount = await GetWatchCount(item.Id);
                        item.Description = watchCount.ToString();

                    }
                    Car = carDetails;
                   
                    ViewBag.status_all = "All";
                }
                else
                {
                    foreach (var item in carDetails)
                    {
                        if (item.Status == status)
                        {
                            var bidDetails = await GetCarOfferCount(item.Id);
                            item.Mileage = bidDetails;
                            var bidCount = await GetBidCount(item.Id);
                            item.Version = bidCount.ToString();
                            var watchCount = await GetWatchCount(item.Id);
                            item.Description = watchCount.ToString();
                            Car.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View("StatusList", Car);

        }

        public async Task<int> GetBidCount(string carId)
        {
            var bidCount = 0;
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlBidding;



                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var lstBidding = await Response.Content.ReadAsAsync<List<AgapeModelBid.Bid>>();
                            if (lstBidding != null && lstBidding.Any())
                            {
                                bidCount = lstBidding.Where(i => i.CarId == carId && i.Type == "Bid").Count();
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Offer Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return bidCount;
        }


        public async Task<int> GetCarOfferCount(string carId)
        {
            var offerDetails = new List<AgapeModelOffer.Offer>();
            var OfferCount = 0;
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlOffers;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var lstOffers = await Response.Content.ReadAsAsync<List<AgapeModelOffer.Offer>>();
                            var result = lstOffers.Where(i => i.CarId == carId && i.Type == "Offer");
                            if (result != null && result.Any())
                            {
                                offerDetails = result.ToList();

                            }
                            OfferCount = offerDetails.Count();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Offer Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return OfferCount;
        }

        public async Task<int> GetWatchCount(string carId)
        {
            var watchDetails = new List<ModelAuctions.Auction>();
            var OfferCount = 0;
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlAuction;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var lstWatch = await Response.Content.ReadAsAsync<List<ModelAuctions.Auction>>();
                            var result = lstWatch.Where(i => i.CarId == carId && i.Type == "Watch");
                            if (result != null && result.Any())
                            {
                                watchDetails = result.ToList();
                            }
                            OfferCount = watchDetails.Count();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Auction service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return OfferCount;
        }

        public async Task<List<AgapeModelCar.Car>> GetCarDetailsDealer(string currentUserId)
        {
            var lstCar = new List<AgapeModelCar.Car>();
            var lstCarImages = await GetAllCarImages();
            var lstSubmittedCars = await GetAuctionSubmittedCars();
            var lstSubmittedCarIds = new List<string>();
            if (lstSubmittedCars != null && lstSubmittedCars.Any())
            {
                lstSubmittedCarIds = lstSubmittedCars.Select(i => i.CarId).ToList();
            }



            using (HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure)))
            {
                string endpoint = apiBaseUrlCar + "Dealer/" + currentUserId;



                using (var Response = await client.GetAsync(endpoint))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        lstCar = await Response.Content.ReadAsAsync<List<AgapeModelCar.Car>>();
                        if (lstCar != null && lstCar.Any())
                        {
                            lstCar = lstCar.ToList();
                            foreach (var car in lstCar)
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



                                //if (lstSubmittedCarIds.Contains(car.Id))
                                // car.Status = "Submitted";
                            }
                        }
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from CarService");
                    }
                }
            }
            return lstCar;
        }
        public async Task<List<ModelAuctions.Auction>> GetAuctionSubmittedCars()
        {
            var lstCars = new List<ModelAuctions.Auction>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<ModelAuctions.Auction>>();
                            if (response != null && response.Any())
                            {
                                var approvedCars = response.Where(i => i.Status == "Submitted");
                                if (approvedCars != null && approvedCars.Any())
                                    lstCars = approvedCars.ToList();
                            }
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
            return lstCars;
        }
        public async Task<AgapeModelUser.User> GetUserByIdentity(string id)
        {
            var user = new AgapeModelUser.User();
            user.Address = new Auctions.Models.Address();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlUser + "idp/" + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var lstUser = await Response.Content.ReadAsAsync<List<AgapeModelUser.User>>();
                            if (lstUser != null && lstUser.Any())
                                user = lstUser.FirstOrDefault();
                            if (user.Address == null)
                            {
                                user.Address = new Auctions.Models.Address();
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from User Service");
                        }
                    }
                }



            }
            catch (Exception ex)
            {



            }
            return user;
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