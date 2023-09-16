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
using AgapeModelAuction = DataAccessLayer.Models;
using Agape.Auctions.UI.Cars.Admin.Utilities;
using Microsoft.Extensions.Logging;


namespace Agape.Auctions.UI.Cars.Admin.ViewComponents
{
    public class AuctionsViewComponent : ViewComponent
    {
        private readonly IConfiguration configure;
        private readonly string apiBaseUrlCarImage;
        private readonly string apiBaseUrlCar;
        private readonly string apiBaseUrlAuction;
        private readonly string defaultCarImageUrl;
        private readonly string[] invalidStatustoShow = { "Closed", "Sold", "UnSold" };
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
            logHelper = new LogHelperComponent(configure, _logger);
            // apiBaseUrlCarSearch = configure.GetValue<string>("WebAPIBaseUrlCarSearch");

        }

        public async Task<IViewComponentResult> InvokeAsync(string id, string viewName, string make, string model, int yearFrom, int yearTo)
        {

            if (viewName == "AuctionResults")
            {
                var lstCars = new List<AgapeModel.Car>();
                try
                {
                    if (yearTo == 0)
                        yearTo = DateTime.Now.Year;

                    lstCars = await GetCars(make, model, yearFrom, yearTo);
                    lstCars = lstCars.Where(i => !invalidStatustoShow.Contains(i.Status)).ToList();
                    var lstImages = await GetAllCarImages();
                    if (lstCars != null && lstCars.Any())
                    {
                        foreach (var car in lstCars)
                        {
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

        public async Task<List<AgapeModel.Car>> GetCars(string make, string model, int yearFrom, int yearTo)
        {
            var lstCarIds = new List<string>();
            var auctionCars = await GetAuctionApprovedCars();
            if(auctionCars != null && auctionCars.Any())
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
                            if (responseCar != null && responseCar.Any())
                            {
                                var response = responseCar.Where(i => lstCarIds.Contains(i.Id));
                                var finalResponse = response.Where(i => ((!string.IsNullOrEmpty(make) && make != "0") ? i.Make == make : 1 == 1)
                                && ((!string.IsNullOrEmpty(model) && model != "0") ? i.Model == model : 1 == 1)
                                && i.Year >= yearFrom && i.Year <= yearTo);

                                if (finalResponse != null && finalResponse.Any())
                                    lstCar = finalResponse.ToList();

                                foreach(var car in lstCar)
                                {
                                    var expiry = auctionCars.Where(i => i.CarId == car.Id);
                                    if(expiry != null && expiry.Any())
                                    {
                                        DateTime expiryDate = (!string.IsNullOrEmpty(expiry.FirstOrDefault().ApprovedDate.ToString()) && expiry.FirstOrDefault().ApprovedDate.Year != 0001) ? expiry.FirstOrDefault().ApprovedDate : expiry.FirstOrDefault().CreatedDate;
                                        expiryDate = expiryDate.AddDays(expiry.FirstOrDefault().AuctionDays);
                                        car.Status = expiryDate.ToString("yyyy-MM-dd");
                                        var date = DateTime.Now.Date;
                                        var compare = expiryDate.Date.CompareTo(date);
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
                            if(response != null && response.Any())
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
