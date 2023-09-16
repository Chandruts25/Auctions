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

namespace Agape.Auctions.UI.Cars.Admin.ViewComponents
{

    public class CarViewComponent : ViewComponent
    {
        private readonly IConfiguration configure;
        private readonly string apiBaseUrlCarImage;
        private readonly string apiBaseUrlCar;
        private readonly string defaultCarImageUrl;
        private readonly string closedStatus = "Closed";
        private readonly ILogger<CarViewComponent> _logger;
        private LogHelperComponent logHelper;

        // private readonly string apiBaseUrlCarSearch;

        public CarViewComponent(IConfiguration configuration, ILogger<CarViewComponent> logger)
        {
            _logger = logger;
            configure = configuration;
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            apiBaseUrlCar = configure.GetValue<string>("WebAPIBaseUrlCar");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            logHelper = new LogHelperComponent(configure, _logger);
            // apiBaseUrlCarSearch = configure.GetValue<string>("WebAPIBaseUrlCarSearch");

        }

        public async Task<IViewComponentResult> InvokeAsync(string id, string viewName, string make, string model, string startPrice, string endPrice, int yearFrom, int yearTo)
        {
          
            if (viewName == "SearchResults")
            {
                var lstCars = new List<AgapeModel.Car>();
                try
                {
                    if (yearTo == 0)
                        yearTo = DateTime.Now.Year;
                  
                    lstCars = await GetCars(make, model, startPrice, endPrice, yearFrom, yearTo);
                    lstCars = lstCars.Where(i => i.Status != closedStatus).ToList();
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

                return View("SearchResults", lstCars);
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
                catch(Exception ex)
                {
                    logHelper.LogError(ex.ToString());
                }
                return View("CarImages", modelData);
            }
        }

        public async Task<List<AgapeModel.Car>> GetCars(string make, string model, string startPrice, string endPrice, int yearFrom, int yearTo)
        {
            var lstCar = new List<AgapeModel.Car>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlCar + "FindCarsByFilter/" + make + "/" + model + "/" + startPrice + "/" + endPrice + "/" + yearFrom + "/" + yearTo;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            lstCar = await Response.Content.ReadAsAsync<List<AgapeModel.Car>>();
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
            return lstCar;
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
