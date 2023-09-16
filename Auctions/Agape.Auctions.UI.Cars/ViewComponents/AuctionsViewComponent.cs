using Agape.Auctions.UI.Cars.Models;
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
using AgapeModelAuction = Agape.Auctions.Models.Auctions;
using Agape.Auctions.UI.Cars.Utilities;
using Microsoft.Extensions.Logging;


namespace Agape.Auctions.UI.Cars.ViewComponents
{
    public class AuctionsViewComponent : ViewComponent
    {
        private readonly IConfiguration configure;
        private readonly string apiBaseUrlCarImage;
        private readonly string apiBaseUrlCar;
        private readonly string apiBaseUrlAuction;
        private readonly string defaultCarImageUrl;
        private readonly string apiBaseUrlBidding;
        private readonly string[] invalidStatustoShow = { "CLOSED", "SOLD", "HOLD", "PAYMENTPENDING", "SUBMITTED", "UNSOLD" };
        private readonly ILogger<CarViewComponent> _logger;
        private LogHelperComponent logHelper;


        public AuctionsViewComponent(IConfiguration configuration, ILogger<CarViewComponent> logger)
        {
            _logger = logger;
            configure = configuration;
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            apiBaseUrlCar = configure.GetValue<string>("WebAPIBaseUrlCar");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlAuction = configure.GetValue<string>("WebAPIBaseUrlAuction");
            apiBaseUrlBidding = configure.GetValue<string>("WebAPIBaseUrlBidding");
            logHelper = new LogHelperComponent(configure, _logger);
            // apiBaseUrlCarSearch = configure.GetValue<string>("WebAPIBaseUrlCarSearch");

        }

        public async Task<IViewComponentResult> InvokeAsync(string id, string viewName, string make, string model, int yearFrom, int yearTo)
        {

            if (viewName == "AuctionResults")
            {
                var lstCars = new List<AgapeModel.Car>();
                var auction = await GetAuctionSubmittedCars();
                var det = new AgapeModelAuction.Auction();

                try
                {
                    if (yearTo == 0)
                        yearTo = DateTime.Now.Year;

                    lstCars = await GetCars(make, model, yearFrom, yearTo);
                    lstCars = lstCars.Where(i => !invalidStatustoShow.Contains(i.Status.ToUpper())).ToList();
                    var lstImages = await GetAllCarImages();
                    if (lstCars != null && lstCars.Any())
                    {
                        foreach (var car in lstCars)
                        {
                            foreach (var item in auction)
                            {
                                if (item.CarId == car.Id)
                                {
                                    det = item;
                                }
                            }
                            var amt = HighestBid(car.Id);
                            var amount = amt.Result;
                            if (amount == 0)
                            {
                                car.SalePrice = (double)det.StartAmount;
                            }
                            else
                            {

                                car.SalePrice = (double)amount;
                            }

                            var carImage = lstImages.Where(i => i.Owner == car.Id);
                            if (carImage != null && carImage.Any())
                            {
                                car.Thumbnail = (carImage.Where(i => i.Order == 1) != null && carImage.Where(i => i.Order == 1).Any()) ? carImage.Where(i => i.Order == 1).Select(i => i.Url).FirstOrDefault().ToString() : carImage.FirstOrDefault().Url;
                            }
                            else
                            {
                                car.Thumbnail = defaultCarImageUrl;
                            }

                            if (car.Video == null)
                                car.Video = new AgapeModel.Video();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logHelper.LogError(ex.ToString());
                }

                return View("AuctionResults", lstCars);
            }
            else if (viewName == "LatestAuction")
            {
                var modelData = new List<AgapeModel.Car>();
                var lstCars = new List<AgapeModel.Car>();
                var auction = await GetAuctionSubmittedCars();
                if (auction != null && auction.Any())
                {
                    var latestAuction = auction.OrderByDescending(i => Convert.ToDateTime(i.ApprovedDate)).Take(3);
                    if (latestAuction != null && latestAuction.Any())
                    {
                        lstCars = await GetCars("0", "0", 0, 3000);
                        var lstImages = await GetAllCarImages();

                        foreach (var car in lstCars)
                        {
                            var auctionCar = auction.Where(i => i.CarId == car.Id);
                            var amt = HighestBid(car.Id);
                            var amount = amt.Result;
                            if (amount == 0)
                            {
                                car.SalePrice = (double)auctionCar.FirstOrDefault().StartAmount;
                            }
                            else
                            {
                                car.SalePrice = (double)amount;
                            }
                            car.SalePrice = (double)amount;
                            var carImage = lstImages.Where(i => i.Owner == car.Id);
                            if (carImage != null && carImage.Any())
                            {
                                car.Thumbnail = (carImage.Where(i => i.Order == 1) != null && carImage.Where(i => i.Order == 1).Any()) ? carImage.Where(i => i.Order == 1).Select(i => i.Url).FirstOrDefault().ToString() : carImage.FirstOrDefault().Url;
                            }
                            else
                            {
                                car.Thumbnail = defaultCarImageUrl;
                            }

                            if (car.Video == null)
                                car.Video = new AgapeModel.Video();
                        }
                    }
                }
                if (lstCars != null && lstCars.Any())
                    modelData = lstCars.OrderBy(i => Convert.ToDateTime(i.Status)).Take(3).ToList();

                return View("LatestAuction", modelData);
            }
            else if (viewName == "AuctionGallery")
            {
                var modelData = new List<AgapeModelImage.Image>();
                try
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        modelData = await GetCarImageByOnwer(id);
                    }
                }
                catch (Exception ex)
                {
                    logHelper.LogError(ex.ToString());
                }
                return View("AuctionGallery", modelData);
            }
            else
            {
                var modelData = new List<AgapeModelImage.Image>();
                try
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        modelData = await GetCarImageByOnwer(id);
                    }
                }
                catch (Exception ex)
                {
                    logHelper.LogError(ex.ToString());
                }
                return View("CarImages", modelData);
            }
        }
        public async Task<decimal> HighestBid(string carId)
        {
            decimal bid = 0.0M;
            var auction = await GetAuctionSubmittedCars();
            var det = new AgapeModelAuction.Auction();
            foreach (var item in auction)
            {
                if (item.CarId == carId)
                {
                    det = item;
                }
            }
            ViewBag.StartPrice = det.StartAmount;
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
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
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from Car Service");
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
        public async Task<List<AgapeModelAuction.Auction>> GetAuctionSubmittedCars()
        {
            var lstCars = new List<AgapeModelAuction.Auction>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<AgapeModelAuction.Auction>>();
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

        public async Task<List<AgapeModel.Car>> GetCars(string make, string model, int yearFrom, int yearTo)
        {
            var lstCarIds = new List<string>();
            var auctionCars = await GetAuctionApprovedCars();
            if (auctionCars != null && auctionCars.Any())
            {
                lstCarIds = auctionCars.Select(i => i.CarId).ToList();
            }
            var lstCar = new List<AgapeModel.Car>();
            var lstCar1 = new List<AgapeModel.Car>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlCar;
                    using (var Response = await client.GetAsync(endpoint))
                    {   
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var responseCar = await Response.Content.ReadAsAsync<List<AgapeModel.Car>>();
                            responseCar = responseCar.Where(i => !invalidStatustoShow.Contains(i.Status.ToUpper())).ToList();
                            if (responseCar != null && responseCar.Any())
                            {
                                var response = responseCar.Where(i => lstCarIds.Contains(i.Id));
                                var finalResponse = response.Where(i => ((!string.IsNullOrEmpty(make) && make != "0") ? i.Make == make : 1 == 1)
                                && ((!string.IsNullOrEmpty(model) && model != "0") ? i.Model == model : 1 == 1)
                                && i.Year >= yearFrom && i.Year <= yearTo);

                                if (finalResponse != null && finalResponse.Any())
                                    lstCar = finalResponse.ToList();

                                foreach (var car in lstCar)
                                {
                                    var expiry = auctionCars.Where(i => i.CarId == car.Id);
                                    if (expiry != null && expiry.Any())
                                    {
                                        DateTime expiryDate = (!string.IsNullOrEmpty(expiry.FirstOrDefault().ApprovedDate.ToString()) && expiry.FirstOrDefault().ApprovedDate.Year != 0001) ? expiry.FirstOrDefault().ApprovedDate : expiry.FirstOrDefault().CreatedDate;
                                        expiryDate = expiryDate.AddDays(expiry.FirstOrDefault().AuctionDays);
                                        car.Status = expiryDate.ToString("yyyy-MM-dd HH:mm:ss");
                                        var date = DateTime.Now;
                                        var compare = expiryDate.CompareTo(date);
                                        if (compare > 0)
                                        {
                                            lstCar1.Add(car);
                                        }
                                    }
                                }
                            }
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
            return lstCar1;
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
                return carImages;
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return carImages;
        }

        public async Task<List<AgapeModelAuction.Auction>> GetAuctionApprovedCars()
        {
            var lstCars = new List<AgapeModelAuction.Auction>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<AgapeModelAuction.Auction>>();
                            if (response != null && response.Any())
                            {
                                var approvedCars = response.Where(i => i.Status == "Approved");
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

            }
            return lstCars;
        }


        public async Task<List<AgapeModelImage.Image>> GetAllCarImages()
        {
            var carImages = new List<AgapeModelImage.Image>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
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
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from CarImage Service");
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }
            return carImages;

        }
    }
}
