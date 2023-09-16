using Agape.Auctions.UI.Cars.Admin.Models;
using Agape.Auctions.UI.Cars.Admin.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AgapeModel = Agape.Auctions.Models.Cars;
using AgapeModelImage = Agape.Auctions.Models.Images;
using AgapeModelUser = Agape.Auctions.Models.Users;
using AgapeModelBid = Agape.Auctions.Models.Biddings;
using System.Security.Claims;
using System.Text;


namespace Agape.Auctions.UI.Cars.Admin.Controllers
{
    public class AuctionsController : Controller
    {
        private readonly ILogger<CarController> _logger;
        private readonly IConfiguration configure;
        private readonly AzureStorageConfig storageConfig = null;
        private readonly string apiBaseUrl;
        private readonly string apiBaseUrlCarImage;
        private readonly string apiBaseUrlBidding;
        private readonly string defaultCarImageUrl;
        private LogHelper logHelper;
        private readonly string apiBaseUrlUser;


        public AuctionsController(IConfiguration configuration, ILogger<CarController> logger)
        {
            configure = configuration;
            apiBaseUrl = configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlUser = configure.GetValue<string>("WebAPIBaseUrlUser");
            apiBaseUrlBidding = configure.GetValue<string>("WebAPIBaseUrlBidding");

            // apiBaseUrlCarSearch = configure.GetValue<string>("WebAPIBaseUrlCarSearch");
            _logger = logger;
            logHelper = new LogHelper(configure, _logger);
        }

        public async Task<IActionResult> Index()
        {
            var makeResult = await GetSearchFilters("1");
            ViewBag.Makes = makeResult;
            var modelResult = await GetSearchFilters("2");
            ViewBag.Models = modelResult;
            var modelYearResult = await GetSearchFilters("3");
            ViewBag.ModelYear = modelYearResult;
            return View();
        }

        public async Task<SelectList> GetSearchFilters(string id)
        {
            var lookUp = new List<LookUpValue>();
            var defaultValue = "Any Make";
            var defaultId = "0";
            if (id == "2")
                defaultValue = "Any Model";
            else if (id == "3")
                defaultValue = "Any Year";
            try
            {
                var lstDetails = new List<string>();
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrl + "FindMakeModelYear/" + id;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            lstDetails = await Response.Content.ReadAsAsync<List<string>>();
                        }
                    }
                }
                foreach (var item in lstDetails)
                {
                    lookUp.Add(new LookUpValue() { Id = item, Value = item });
                }
                //if (id == "3")
                //{
                //    defaultId = lstDetails.ConvertAll(int.Parse).Max().ToString();
                //}
                lookUp.Insert(0, new LookUpValue() { Id = defaultId, Value = defaultValue });
                return new SelectList(lookUp, "Id", "Value");
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return new SelectList(lookUp, "Id", "Value");
            }
        }

        public IActionResult AuctionResults(string make, string model, int yearFrom, int yearTo)
        {
            return ViewComponent("Auctions", new { viewName = "AuctionResults", make = make, model = model, yearFrom = yearFrom, yearTo = yearTo });
        }

        public async Task<IActionResult> Details(string carId)
        {
            var carDetails = await GetCarDetails(carId);
            ViewBag.CarDetails = carDetails;
            if (carDetails != null)
            {
                var carOwner = carDetails.Owner;
                var user = await GetUserByIdentity(carOwner);
                ViewBag.UserDetails = user;
            }
            
            var modelData = new List<AgapeModelImage.Image>();
            try
            {
                if (!string.IsNullOrEmpty(carId))
                    modelData = await GetCarImageByOnwer(carId);

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }

            return View(modelData);
        }

        public async Task<AgapeModel.Car> GetCarDetails(string id)
        {
            var carDetails = new AgapeModel.Car();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrl + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carDetails = await Response.Content.ReadAsAsync<AgapeModel.Car>();
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

        public async Task<AgapeModelUser.User> GetUserByIdentity(string id)
        {
            var user = new AgapeModelUser.User();
            user.Address = new Auctions.Models.Address();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
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

        protected List<string> AllClaimsFromAzure()
        {
            ClaimsIdentity claimsIdentity = ((ClaimsIdentity)User.Identity);
            return claimsIdentity.Claims.Select(x => x.Value).ToList();
        }
        protected string GetCurrentNameFromAzureClaims()
        {
            return AllClaimsFromAzure()[4];
        }

        public async Task<JsonResult> AddBidding()
        {
            try
            {
                var dealerDetails = Request.Form["biddingDetails"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var bidDetails = (AgapeModelBid.Bid)jsonSerializer.Deserialize(reader, typeof(AgapeModelBid.Bid));
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bidDetails.CreatedBy = userId;
                bidDetails.CreatedDate = DateTime.Now;
                bidDetails.UpdatedBy = GetCurrentNameFromAzureClaims();

                StringContent content = new StringContent(JsonConvert.SerializeObject(bidDetails), Encoding.UTF8, "application/json");

                var endpoint = apiBaseUrlBidding;
                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Bidding Service");
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
