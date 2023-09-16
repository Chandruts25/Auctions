using Agape.Auctions.UI.Cars.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using AgapeAPI.Core;
using AgapeModel = DataAccessLayer.Models;
using AgapeModelImage = DataAccessLayer.Models;
//using AgapeModelDealer = DataAccessLayer.Models;
using AgapeModelUser = DataAccessLayer.Models;
using DALModels = DataAccessLayer.Models;
using Agape.Auctions.UI.Cars.Admin.Utilities;
using Microsoft.Extensions.Logging;


namespace Agape.Auctions.UI.Cars.Admin.Controllers
{
    public class DealersController : Controller
    {
        private readonly IConfiguration _configure;
        private readonly string apiBaseUrl;
        private readonly string apiBaseUrlUser;
        private readonly string apiBaseUrlCarImage;
        private readonly string defaultCarImageUrl;
        private readonly string apiBaseUrlCar;
        private readonly string[] invalidStatustoShow = { "Closed", "Sold", "UnSold" };
        private readonly IServiceManager _serviceManager;
        private readonly ILogger<DealersController> _logger;
        private LogHelper logHelper;

        public DealersController(IConfiguration configuration, IServiceManager serviceManager, ILogger<DealersController> logger)
        {
            _configure = configuration;
            apiBaseUrl = _configure.GetValue<string>("WebAPIBaseUrlDealer");
            apiBaseUrlUser = _configure.GetValue<string>("WebAPIBaseUrlUser");
            apiBaseUrlCarImage = _configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = _configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlCar = _configure.GetValue<string>("WebAPIBaseUrlCar");
            _serviceManager = serviceManager;
            _logger = logger;
            logHelper = new LogHelper(_configure, _logger);
        }
       

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Dealers";
            ViewBag.Description = "Dealers";
            ViewBag.Keywords = "Dealers";
            try
            {
                var pagePartDealerInfo = await _serviceManager.GetPagePart((int)AgapePageEnum.DealerInfo);
                ViewBag.DealerInfo = pagePartDealerInfo.Body;

                var lstDealers = new List<AgapeModelUser.User>();
                using (var client =new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    var endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var tmpDealers = await Response.Content.ReadAsAsync<List<AgapeModelUser.User>>(); //Address didn't retun now 
                            //Get all list addresses
                            foreach(var item in tmpDealers)
                            {
                                var dealer = await GetDealerDetails(item.Id);
                                lstDealers.Add(dealer);
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from Dealer Service");
                        }
                    }
                }
                foreach(var item in lstDealers)
                {
                    if (item.Address == null)
                    {
                        item.Address = new DALModels.Address();
                    }
                }
                return View(lstDealers);
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View();
        }

        public async Task<IActionResult> Dealers()
        {
            ViewBag.Title = "Dealers";
            ViewBag.Description = "Dealers";
            ViewBag.Keywords = "Dealers";
            var lstDealers = new List<AgapeModelUser.User>();

            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    var endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            lstDealers = await Response.Content.ReadAsAsync<List<AgapeModelUser.User>>();

                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from Dealer Service");
                        }
                    }
                }
                foreach (var item in lstDealers)
                {
                    if (item.Address == null)
                    {
                        item.Address = new DALModels.Address();
                    }
                }
            }
            catch(Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(lstDealers);
        }

        public async Task<IActionResult> Dealer(string id)
        {
            var lstCars = new List<AgapeModel.Car>();
            try
            {
                lstCars = await GetDealerCar(id);
                lstCars = lstCars.Where(i => !invalidStatustoShow.Contains(i.Status)).ToList();
                var lstCarImages = await GetAllCarImages();

                if (lstCars != null && lstCars.Any())
                {
                    foreach (var car in lstCars)
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
                            car.Video = new AgapeModel.Video();
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(lstCars);
        }

        public IActionResult AddEditDealer(string id)
        {
            return ViewComponent("Dealers", new { id = id });
        }
       

        //DELETE - Remove the Dealer
        public async Task<JsonResult> RemoveDealer()
        {
            try
            {
                var dealerId = Request.Form["dealerId"];

                using (HttpClient client =new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrl + dealerId;

                    using (var Response = await client.DeleteAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return Json(new { SaveResult = true });
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from Dealer Service");
                            return Json(new { SaveResult = false });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { SaveResult = false });
            }

        }

        //PUT - Update the User
        public async Task<bool> UpdateUser(AgapeModelUser.User user)
        {
            try
            {
                user.ConfirmPassword = user.Password;
                using HttpClient client =new HttpClient(new CustomHttpClientHandler(_configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlUser + user.Id;

                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " " + "Error from User Service");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return false;
            }
        }

        public async Task<AgapeModelUser.User> GetDealerDetails(string id)
        {
            var dealer = new AgapeModelUser.User();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrl + id;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            dealer = await Response.Content.ReadAsAsync<AgapeModelUser.User>();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from Dealer Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return dealer;
        }

        public async Task<List<AgapeModel.Car>> GetDealerCar(string dealerId)
        {
            var lstCar = new List<AgapeModel.Car>();
            try
            {
                using (HttpClient client =new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlCar + "Dealer/" + dealerId;

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

        public async Task<List<AgapeModelImage.Image>> GetAllCarImages()
        {
            var carImages = new List<AgapeModelImage.Image>();
            try
            {
                using (var client =new HttpClient(new CustomHttpClientHandler(_configure)))
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
                logHelper.LogError(ex.ToString());
            }
            return carImages;
        }

    }
}
