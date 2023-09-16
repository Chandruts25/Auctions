using Agape.Auctions.UI.Cars.Models;
using Agape.Auctions.UI.Cars.Utilities;
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
using AgageModelCarReview = Agape.Auctions.Models.Cars.CarReview;
using ModelAuctions = Agape.Auctions.Models.Auctions;
using System.Security.Claims;
using System.Text;
using Stripe;
using Microsoft.AspNetCore.Authorization;
using AgapeAPI.Core;
using Stripe.Checkout;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using Agape.Auctions.UI.Cars.ViewComponents;

namespace Agape.Auctions.UI.Cars.Controllers
{
    public class AuctionsController : Controller
    {
        private readonly ILogger<CarController> _logger;
        private readonly ILogger<CarViewComponent> _carViewComponentLogger;
        private readonly IConfiguration configure;
        private readonly AzureStorageConfig storageConfig = null;
        private readonly string apiBaseUrl;
        private readonly string apiBaseUrlCarImage;
        private readonly string apiBaseUrlBidding;
        private readonly string defaultCarImageUrl;
        private LogHelper logHelper;
        private readonly string[] invalidStatustoShow = { "CLOSED", "SOLD", "HOLD", "PAYMENTPENDING", "SUBMITTED", "UNSOLD" };
        private readonly string apiBaseUrlUser;
        private readonly string apiBaseUrlCarReview;
        private readonly string apiBaseUrlAuction;
        private readonly string apiBaseUrlVin;
        private readonly string stripeAPIKey;
        private readonly string bidRefreshFunctionUrl;
        private readonly IServiceManager _serviceManager;
        HubConnection connection = null;

        public AuctionsController(IConfiguration configuration, ILogger<CarController> logger, IServiceManager serviceManager, ILogger<CarViewComponent> carComponentLogger)
        {
            configure = configuration;
            apiBaseUrl = configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlUser = configure.GetValue<string>("WebAPIBaseUrlUser");
            apiBaseUrlBidding = configure.GetValue<string>("WebAPIBaseUrlBidding");
            apiBaseUrlCarReview = configure.GetValue<string>("WebAPIBaseUrlCarReview");
            apiBaseUrlAuction = configure.GetValue<string>("WebAPIBaseUrlAuction");
            apiBaseUrlVin = configure.GetValue<string>("WebAPIBaseUrlVin");
            stripeAPIKey = configure.GetValue<string>("StripeAPIKey");
            bidRefreshFunctionUrl = configure.GetValue<string>("FunctionBidRefreshUrl");
            _serviceManager = serviceManager;
            _carViewComponentLogger = carComponentLogger;
            // apiBaseUrlCarSearch = configure.GetValue<string>("WebAPIBaseUrlCarSearch");
            _logger = logger;
            logHelper = new LogHelper(configure, _logger);
            StripeConfiguration.ApiKey = stripeAPIKey;
        }

        public async Task<IActionResult> Index()
        {
            //var make = await GetSearchFilters("1");
            //var models = await GetSearchFilters("2");
            //var year = await GetSearchFilters("3");
            //ViewBag.Makes = make;
            //ViewBag.Models = models;
            //ViewBag.ModelYear = year;

            ViewBag.BidRefreshFunctionUrl = bidRefreshFunctionUrl;

            var lstCars = new List<AgapeModel.Car>();
            var auctionSubmittedCars = await GetAuctionSubmittedCars();
            var carResponse = await GetAllCars();

            var viewComponent = new AuctionsViewComponent(configure, _carViewComponentLogger);
            var yearTo = DateTime.Now.Year;
            lstCars = await viewComponent.GetCars("0", "0", 0, yearTo);

           // if (auctionSubmittedCars != null && auctionSubmittedCars.Any())
           // {
           //     var auctionCars = auctionSubmittedCars.Select(i => i.CarId).ToArray();
           //     if (string.IsNullOrEmpty(carResponse.Item2))
           //     {
           //         var cars = carResponse.Item1.Where(i => !invalidStatustoShow.Contains(i.Status.ToUpper()) && auctionCars.Contains(i.Id)).ToList();
           //         if(cars != null && cars.Any())
           //         {
           //             lstCars = cars;
           //         }
           //     }
           //}

            var make = await GetSearchFiltersAuctions("1", lstCars);
            var models = await GetSearchFiltersAuctions("2", lstCars);
            var year = await GetSearchFiltersAuctions("3", lstCars);
            ViewBag.Makes = make;
            ViewBag.Models = models;
            ViewBag.ModelYear = year;


            return View();
        }

        public async Task<(List<AgapeModel.Car>, string)> GetAllCars()
        {
            var lstCars = new List<AgapeModel.Car>();
            string error = string.Empty;
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            lstCars = await Response.Content.ReadAsAsync<List<AgapeModel.Car>>();
                        }
                        else
                        {
                            error = "Error from Car Service";
                            logHelper.LogError(Response.ReasonPhrase + " " + error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = "Exception while get all cars";
                logHelper.LogError(ex.ToString());
            }
            return (lstCars, error);
        }

        public async Task<SelectList> GetSearchFiltersAuctions(string id, List<AgapeModel.Car> lstCars)
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

                if(lstCars != null && lstCars.Any())
                {
                    if (id == "1")
                    {
                        lstDetails = lstCars.Select(i => i.Make).Distinct().ToList();
                    }
                    else if (id == "2")
                    {
                        lstDetails = lstCars.Select(i => i.Model).Distinct().ToList();
                    }
                    else if (id == "3")
                    {
                        lstDetails = lstCars.Select(i => i.Year.ToString()).Distinct().ToList();
                    }
                }

                foreach (var item in lstDetails.OrderBy(i => i.ToString()))
                {
                    lookUp.Add(new LookUpValue() { Id = item, Value = item });
                }
                //if (id == "3")
                //{s
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
                foreach (var item in lstDetails.OrderBy(i => i.ToString()))
                {
                    lookUp.Add(new LookUpValue() { Id = item, Value = item });
                }
                //if (id == "3")
                //{s
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

        public IActionResult AuctionGallery(string id)
        {
            return ViewComponent("Auctions", new { viewName = "AuctionGallery", id = id });
        }

        public async Task<IActionResult> Details(string carId)
        {
            var modelData = new List<AgapeModelImage.Image>();
            try
            {
                var carDetails = await GetCarDetails(carId);
                ViewBag.CarDetails = carDetails;

                if (carDetails != null)
                {
                    var carOwner = carDetails.Owner;
                    var user = await GetUserByIdentity(carOwner);
                    ViewBag.UserDetails = user;
                }
                var carReviews = GetReviewsAndComments(carId);
                ViewBag.CarReviews = carReviews;

                var auction = await GetAuctionSubmittedCars();
                var det = new ModelAuctions.Auction();

                foreach (var item in auction)
                {
                    if (item.CarId == carId)
                    {
                        det = item;
                        DateTime expiryDate = (!string.IsNullOrEmpty(det.ApprovedDate.ToString()) && det.ApprovedDate.Year != 0001) ? det.ApprovedDate : det.CreatedDate;
                        expiryDate = expiryDate.AddDays(det.AuctionDays);
                        det.Status = expiryDate.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                ViewBag.inc = det.Increment;
                ViewBag.StartingPrice = det.StartAmount;
                ViewBag.Status_Date = det.Status;
                ViewBag.CurrentHighestBid = await GetHighestBidDetails(carId);
                ViewBag.BidRefreshFunctionUrl = bidRefreshFunctionUrl;
                var bidDetails = await GetCarBidDetails(carId);
                ViewBag.BidDetails = bidDetails;
                try
                {
                    if (!string.IsNullOrEmpty(carId))
                        modelData = await GetCarImageByOnwer(carId);

                }
                catch (Exception ex)
                {
                    logHelper.LogError(ex.ToString());
                }
                var additionalCarDetails = string.Empty;
                if (carDetails != null && !string.IsNullOrEmpty(carDetails.PagePartId))
                {
                    // var pagePartDetails = await _serviceManager.GetPagePart(int.Parse(carDetails.PagePartId));
                    // additionalCarDetails = pagePartDetails.Body;
                    additionalCarDetails = await GetPagePartDetails(int.Parse(carDetails.PagePartId));
                }
                if (carDetails != null && modelData != null && modelData.Any())
                {
                    //carDetails.Thumbnail = modelData[0].Url;
                    carDetails.Thumbnail = (modelData.Where(i => i.Order == 1) != null && modelData.Where(i => i.Order == 1).Any()) ? modelData.Where(i => i.Order == 1).Select(i => i.Url).FirstOrDefault().ToString() : modelData.FirstOrDefault().Url;
                }
                else if (carDetails != null)
                {
                    carDetails.Thumbnail = defaultCarImageUrl;
                }

                ViewBag.AdditionalCarDetails = additionalCarDetails;
            }
            catch (Exception ex)
            {

            }
            return View(modelData);
        }



        public async Task<string> GetPagePartDetails(int pagePartId)
        {
            var bodyDetails = string.Empty;
            try
            {
                var pagePartDetails = await _serviceManager.GetPagePart(pagePartId);
            }
            catch (Exception ex)
            {

            }
            return bodyDetails;
        }
        public async Task<IActionResult> BidCount(string carId)
        {
            var bidDetails = await GetCarBidDetails(carId);
            return PartialView("_BidCount", bidDetails);
        }
        public async Task<IActionResult> CommentCount(string carId)
        {
            var carReviews = new List<AgageModelCarReview>();
            carReviews = await GetReviewsAndComments(carId);
            return PartialView("_CommentCount", carReviews);
        }

        public async Task<List<AgapeModelBid.Bid>> GetCarBidDetails(string carId)
        {
            var bidDetails = new List<AgapeModelBid.Bid>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlBidding;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var lstBid = await Response.Content.ReadAsAsync<List<AgapeModelBid.Bid>>();
                            if (lstBid != null && lstBid.Any())
                            {
                                var result = lstBid.Where(i => i.CarId == carId);
                                if (result != null && result.Any())
                                {
                                    bidDetails = result.ToList();
                                }
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Bidding Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return bidDetails;
        }

        public async Task<List<AgageModelCarReview>> GetReviewsAndComments(string carId)
        {
            List<AgageModelCarReview> lstCarReview = new List<AgageModelCarReview>();
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                string endpoint = apiBaseUrlCarReview + "GetReviewByCar/" + carId;
                using (var Response = await client.GetAsync(endpoint))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        lstCarReview = await Response.Content.ReadAsAsync<List<AgageModelCarReview>>();
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " " + "Error from CarImage Service");
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return lstCarReview;
        }

        public async Task<bool> UpdateReviewsAndComments(AgageModelCarReview carReview)
        {
            using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
            StringContent content = new StringContent(JsonConvert.SerializeObject(carReview),
                                                      Encoding.UTF8,
                                                      "application/json");
            string endpoint = apiBaseUrlCarReview + "/" + carReview.Id;
            try
            {
                using HttpResponseMessage Response = await client.PutAsync(endpoint, content);
                if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    logHelper.LogError(Response.ReasonPhrase + " Error from car review service");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteReviewsAndComments(string reviewId)
        {
            using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
            string endpoint = apiBaseUrlCarReview + "/" + reviewId;
            try
            {
                using HttpResponseMessage Response = await client.DeleteAsync(endpoint);
                if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    logHelper.LogError(Response.ReasonPhrase + " Error from car review service");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return false;
            }
        }

        public async Task<decimal> GetHighestBidDetails(string carId)
        {
            decimal bid = 0.0M;
            var auction = await GetAuctionSubmittedCars();
            var det = new ModelAuctions.Auction();
            foreach (var item in auction)
            {
                if (item.CarId == carId)
                {
                    det = item;
                }
            }
           var currentBid = det.StartAmount;
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
                            if (bid <= 0)
                                bid = currentBid;
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from bidding Service");
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

        public async Task<IActionResult> HighestBid(string carId)
        {
            decimal bid = 0.0M;
            var auction = await GetAuctionSubmittedCars();
            var det = new ModelAuctions.Auction();
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
            return PartialView("_Bid", bid);
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
        public async Task<List<ModelAuctions.Auction>> GetAuctionSubmittedCars()
        {
            var lstCars = new List<ModelAuctions.Auction>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<ModelAuctions.Auction>>();
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

        //public async Task<IActionResult> AuthorizeBidding(string userEmail)
        //{
        //    var options = new SessionCreateOptions
        //    {
        //        LineItems = new List<SessionLineItemOptions>
        //                        {
        //                          new SessionLineItemOptions
        //                          {
        //                            PriceData = new SessionLineItemPriceDataOptions
        //                            {
        //                              UnitAmountDecimal = 0,
        //                              Currency = "usd",
        //                              ProductData = new SessionLineItemPriceDataProductDataOptions
        //                              {
        //                                Name = "BADASS Carz"
        //                              },

        //                            },
        //                            Quantity = 1,
        //                          },
        //                        },
        //        Mode = "payment",
        //        SuccessUrl = "test.html",
        //        CancelUrl = "test.html"
        //    };

        //    var service = new SessionService();
        //    Session session = service.Create(options);

        //    Response.Headers.Add("Location", session.Url);
        //    return new StatusCodeResult(303);
        //}


        public async Task<List<ModelAuctions.Auction>> GetAuctionCars(string stat)
        {
            var lstCars = new List<ModelAuctions.Auction>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<ModelAuctions.Auction>>();
                            if (response != null && response.Any())
                            {
                                var approvedCars = response.Where(i => i.Status == stat);
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

        [Authorize]
        public async Task<JsonResult> AddWatchlist(string carId)
        {
            var result = true;
            var existingUserData = await GetAuctionWatchlistCar(carId);
            if (!existingUserData)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var model = new ModelAuctions.Auction();
                model.Type = "Watch";
                model.CarId = carId;
                model.CreatedBy = userId;

                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(model),
                                                          Encoding.UTF8,
                                                          "application/json");
                string endpoint = apiBaseUrlAuction;
                try
                {
                    using HttpResponseMessage Response = await client.PostAsync(endpoint, content);
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = true;
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from auction service");
                        result = false;
                    }
                }
                catch (Exception ex)
                {
                    logHelper.LogError(ex.ToString());
                    result = false;
                }
            }
            return Json(new { result = result, isNew = !existingUserData });

        }

        public async Task<bool> GetAuctionWatchlistCar(string carId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<ModelAuctions.Auction>>();
                            if (response != null && response.Any())
                            {
                                var userList = response.Where(i => i.Type == "Watch" && i.CreatedBy == userId && i.CarId == carId);
                                if (userList != null && userList.Any())
                                {
                                    return true;
                                }
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
            return false;
        }
    }
}
