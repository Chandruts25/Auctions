using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServiceReference;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Agape.Auctions.UI.Cars.Utilities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using AgapeModel = Agape.Auctions.Models.Users;
using AgapeModelAddress = Agape.Auctions.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using AgapeModelBid = Agape.Auctions.Models.Biddings;
using ModelAuctions = Agape.Auctions.Models.Auctions;
using Agape.Auctions.UI.Cars.ViewComponents;

namespace Agape.Auctions.UI.Cars.Controllers
{
    [Authorize]
    public class ActionsCheckoutController : Controller
    {
        private readonly IConfiguration configure;
        private readonly ILogger<HomeController> _logger;
        private readonly ILogger<CarController> _auctionLogger;
        private readonly ILogger<CarViewComponent> _carComponentLogger;
        private readonly IServiceManager _serviceManager;
        private readonly string apiBaseUrlUser;
        private LogHelper logHelper;
        private readonly string apiBaseUrlBidding;
        private readonly string apiBaseUrlAuction;
        private readonly string stripeAPIKey;

        public ActionsCheckoutController(ILogger<HomeController> logger, IServiceManager serviceManager, IConfiguration configuration, ILogger<CarController> auctiionLogger, ILogger<CarViewComponent> carViewComponetLogger)
        {
            _logger = logger;
            _auctionLogger = auctiionLogger;
            configure = configuration;
            _serviceManager = serviceManager;
            _carComponentLogger = carViewComponetLogger;
            apiBaseUrlUser = configure.GetValue<string>("WebAPIBaseUrlUser");
            apiBaseUrlBidding = configure.GetValue<string>("WebAPIBaseUrlBidding");
            apiBaseUrlAuction = configure.GetValue<string>("WebAPIBaseUrlAuction");
            stripeAPIKey = configure.GetValue<string>("StripeAPIPublishedKey");
            logHelper = new LogHelper(configure, _logger);
        }

        public async Task<IActionResult> Summary(string carId, decimal bidAmount)
        {
            string paymentMethod = "false";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var objHomeController = new HomeController(_logger, _serviceManager, configure);
            if (!string.IsNullOrEmpty(userId))
            {
                var users = await objHomeController.GetUserByIdentity(userId);

                if (string.IsNullOrEmpty(users.Item2))
                {
                    if (users.Item1 == null || !users.Item1.Any() || users.Item1.Count <= 0)
                    {
                        await CreateandAddUser(userId);
                    }
                }

                if(users.Item1 != null && users.Item1.Any())
                {
                    var currentUser = users.Item1.FirstOrDefault();
                    if(currentUser != null && currentUser.PaymentMethods != null && currentUser.PaymentMethods.Any())
                    {
                        paymentMethod = "true";
                    }
                }

            }
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.CarId = carId;
            ViewBag.BidAmount = bidAmount;
            ViewBag.StripeAPIKey = stripeAPIKey;
            ViewBag.CurrentUserEmail = GetClaimValueByType("emails");
            return View();
        }

        public async Task<JsonResult> UpdateUser(AgapeModel.User user)
        {
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlUser + user.Id;

                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true, userId = user.Id });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from User Service");
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

        public async Task<JsonResult> AddBidding()
        {
            try
            {
                var dealerDetails = Request.Form["biddingDetails"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var bidDetails = (AgapeModelBid.Bid)jsonSerializer.Deserialize(reader, typeof(AgapeModelBid.Bid));

                var actionController = new AuctionsController(configure, _auctionLogger, _serviceManager,_carComponentLogger);
                var highestBid = await actionController.GetHighestBidDetails(bidDetails.CarId);

                var getSubmittedCars = await actionController.GetAuctionSubmittedCars();
                var currentCarDetail = getSubmittedCars.Where(i => i.CarId == bidDetails.CarId);
                if(currentCarDetail != null && currentCarDetail.Any())
                {
                    decimal incrementAmount = currentCarDetail.FirstOrDefault().Increment;
                    decimal maxBid = highestBid + incrementAmount;
                    if(bidDetails.BiddingAmount < maxBid)
                    {
                        return Json(new { result = false, maxBid =false });
                    }
                }

                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!bidDetails.Deleted)
                {
                    var objHomeController = new HomeController(_logger, _serviceManager, configure);
                    var users = await objHomeController.GetUserByIdentity(userId);

                    if (users.Item1 != null && users.Item1.Any())
                    {
                        var currentUser = users.Item1.FirstOrDefault();
                        var paymentMethod = new List<string>() { bidDetails.Type };
                        currentUser.PaymentMethods = paymentMethod;
                        await UpdateUser(currentUser);
                    }
                }
                var auction = await GetAuctionCars("Approved");
                var detail = new ModelAuctions.Auction();
                foreach(var item in auction)
                {
                    if(item.CarId == bidDetails.CarId)
                    {
                        detail = item;
                    }
                }
                bidDetails.Deleted = false;
                bidDetails.Type = "Bid";
                bidDetails.CreatedBy = userId;
                bidDetails.CreatedDate = DateTime.Now;
                bidDetails.AuctionDays = detail.AuctionDays;
                bidDetails.UpdatedBy = GetCurrentNameFromAzureClaims();

                StringContent content = new StringContent(JsonConvert.SerializeObject(bidDetails), Encoding.UTF8, "application/json");

                var endpoint = apiBaseUrlBidding;
                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true, maxBid = true });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Bidding Service");
                        return Json(new { result = false, maxBid = true });
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, maxBid = true });
            }
        }
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
        public async Task<string> CreateandAddUser(string userId)
        {
            string error = string.Empty;
            try
            {
                var objHomeController = new HomeController(_logger, _serviceManager, configure);
                var emailAddress = GetClaimValueByType("emails");
                var name = GetClaimValueByType("name");
                var user = new AgapeModel.User();
                user.Idp = userId;
                user.Email = emailAddress;
                user.FirstName = GetClaimValueByType("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
                user.LastName = GetClaimValueByType("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");
                if (IsLoggedWithGmail())
                    user.UserType = "Guest";    
                var address = new AgapeModelAddress.Address();
                address.Country = GetClaimValueByType("country");
                user.Address = address;
                bool response = await objHomeController.AddNewUser(user);
                if (!response)
                {
                    error = "Error while add the new user, Please contact administrator for more details";
                }

            }
            catch (Exception ex)
            {
                error = "Exception while validate and add the user, Please contact administrator for more details";
                logHelper.LogError(ex.ToString());
            }
            return error;
        }

        public string GetClaimValueByType(string claimType)
        {
            var currentValue = string.Empty;
            ClaimsIdentity claimsIdentity = ((ClaimsIdentity)User.Identity);
            foreach (Claim claim in claimsIdentity.Claims)
            {
                if (claim.Type.Contains(claimType))
                {
                    currentValue = claim.Value;
                    break;
                }
            }
            return currentValue;
        }


        public List<string> AllClaimsFromAzure()
        {
            ClaimsIdentity claimsIdentity = ((ClaimsIdentity)User.Identity);
            return claimsIdentity.Claims.Select(x => x.Value).ToList();
        }

        public bool IsLoggedWithGmail()
        {
            bool isLoggedinWithGmail = false;
            var claimsDetails = AllClaimsFromAzure();
            if (claimsDetails.Count > 8 && claimsDetails[8] != null && !string.IsNullOrEmpty(claimsDetails[8]))
            {
                if (claimsDetails[8].ToString().ToUpper().Contains("GMAIL"))
                    isLoggedinWithGmail = true;
            }
            return isLoggedinWithGmail;
        }

        public string GetCurrentEmailFromAzureClaims()
        {
            if(IsLoggedWithGmail())
                return AllClaimsFromAzure()[8];
            else
               return AllClaimsFromAzure()[5];
        }
        public string GetCurrentNameFromAzureClaims()
        {
            if (IsLoggedWithGmail())
                return AllClaimsFromAzure()[5];
            else
                return AllClaimsFromAzure()[4];
        }

        public string GetCurrentCountryFromAzureClaims()
        {
            if (IsLoggedWithGmail())
                return AllClaimsFromAzure()[7];
            else
                return AllClaimsFromAzure()[2];
        }
    }
}
