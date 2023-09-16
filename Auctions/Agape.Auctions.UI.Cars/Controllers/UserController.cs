using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AgapeModelUser = DataAccessLayer.Models;
using DALModels = DataAccessLayer.Models;
using AgapeModelCar = DataAccessLayer.Models;
using AgapeModelImage = DataAccessLayer.Models;
using Agape.Auctions.UI.Cars.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Microsoft.Extensions.Logging;
using AgapeModelPayment = DataAccessLayer.Models;
using AgapeModelPurchase = DataAccessLayer.Models;
using AgapeModelOffer = DataAccessLayer.Models;
using Model = Agape.Auctions.UI.Cars.Models;
using ModelAuctions = DataAccessLayer.Models;
using AgapeModelBid = DataAccessLayer.Models;
using AgapeModel = DataAccessLayer.Models;
using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Agape.Auctions.UI.Cars.Controllers
{
    [Authorize]
    public class UserController : Controller
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
        private readonly string apiBaseUrlVin;
        private readonly string[] invalidStatustoShow = { "Closed", "Sold", "UnSold" };
        private readonly IServiceManager _serviceManager;
        private readonly ILogger<UserController> _logger;
        private LogHelper logHelper;
        private readonly string apiBaseUrlPurchase;
        private readonly string stripeAPIKey;
        private readonly string paymentSuccessRedirectUrl;
        private readonly string paymentErrorRedirectUrl;
        private IHttpContextAccessor _httpContextAccessor;
        private readonly AzureStorageConfig storageConfig = null;
        private readonly FireBaseStorageConfig _firebaseConfig;


        public UserController(IConfiguration configuration, IOptions<FireBaseStorageConfig> firebaseConfig, IServiceManager serviceManager, ILogger<UserController> logger, IHttpContextAccessor httpContextAccessor, IOptions<AzureStorageConfig> config)
        {
            _logger = logger;
            _configure = configuration;
            storageConfig = config.Value;
            apiBaseUrlDealer = _configure.GetValue<string>("WebAPIBaseUrlDealer");
            apiBaseUrlUser = _configure.GetValue<string>("WebAPIBaseUrlUser");
            apiBaseUrlCar = _configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = _configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = _configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlPayment = _configure.GetValue<string>("WebAPIBaseUrlPayment");
            apiBaseUrlPurchase = _configure.GetValue<string>("WebAPIBaseUrlPurchase");
            apiBaseUrlOffers = _configure.GetValue<string>("WebAPIBaseUrlOffers");
            apiBaseUrlAuction = _configure.GetValue<string>("WebAPIBaseUrlAuction");
            apiBaseUrlBidding = _configure.GetValue<string>("WebAPIBaseUrlBidding");
            apiBaseUrlVin = _configure.GetValue<string>("WebAPIBaseUrlVin");
            _serviceManager = serviceManager;
            stripeAPIKey = _configure.GetValue<string>("StripeAPIKey");
            paymentSuccessRedirectUrl = _configure.GetValue<string>("DealerShipSuccessRedirectUrl");
            paymentErrorRedirectUrl = _configure.GetValue<string>("DealerShipErrorRedirectUrl");
            logHelper = new LogHelper(_configure, _logger);
            _httpContextAccessor = httpContextAccessor;

            StripeConfiguration.ApiKey = stripeAPIKey;
            _firebaseConfig = firebaseConfig.Value;
        }

        public async Task<IActionResult> PaymentSummary()
        {
            var modelData = await GetPaymentInformation();
            return View(modelData);
        }

        public async Task<IActionResult> PurchaseSummary()
        {
            var modelData = await GetPurchaseInformation();
            return View(modelData);
        }

        public async Task<IActionResult> MakeOffer()
        {
            return View();
        }
        public ActionResult StatusList(string view, string status = "All")
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return ViewComponent("Status", new { view = view, userId = userId, status = status });
        }
        public async Task<IActionResult> MyBidding(string carId)
        {

            ViewBag.Title = "Bidding Details";
            ViewBag.Description = "Bidding Details";
            ViewBag.Keywords = "Bidding Details";
            var carDetails = await GetCarDetails(carId);
            ViewBag.CarDetail = carDetails;
            var bidDetails = await GetCarBidDetails(carId);
            return View(bidDetails);
        }
        public async Task<IActionResult> BidDetails(string BidId)
        {
            var details = await ViewBid(BidId);
            var userDetail = await GetUserByIdentity(details.CreatedBy);
            ViewBag.UserDetail = userDetail;
            var carDetails = await GetCarDetails(details.CarId);
            ViewBag.CarDetail = carDetails;
            return View(details);
        }
        public async Task<AgapeModelBid.Bid> ViewBid(string BidId)
        {
            var offerDetails = new AgapeModelBid.Bid();

            try
            {

                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlBidding + BidId;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            offerDetails = await Response.Content.ReadAsAsync<AgapeModelBid.Bid>();

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
            return offerDetails;

        }

        [HttpPost]
        public async Task<IActionResult> UploadUserPhoto(IList<IFormFile> files)
        {
            bool uploadResult = false;
            var imageUrl = "";

            try
            {
                if (files.Count == 0)
                    return Json(new { result = false, message = "No profile photo available to upload, Please check the input and try again", count = files.Count });

                var userIdp = Request.Form["userIdp"];

                if (!string.IsNullOrEmpty(userIdp))
                {
                    if (StorageHelper.IsImage(files[0]))
                    {
                        if (files[0].Length > 0)
                        {
                            imageUrl = await StorageHelper.UploadFileToStorage(files[0], _firebaseConfig);
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                                var user = await GetUserByIdentity(userId);
                                //var purchases = new List<string>();
                                //purchases.Add(imageUrl);
                                user.ProfileUrl = imageUrl;
                                //Call the api to save the data in to cosmosdb
                                var response = await UpdateDealerDetails(user);
                                if (!response)
                                    return Json(new { result = false, message = "Error while upload the user profile photo, Please try again later", count = files.Count });
                            }
                        }
                    }
                    else
                    {
                        logHelper.LogError("Upload user profile photo - Trying to upload the invalid image format");
                        return Json(new { result = false, message = "Invalid image format type, Please try again with valid image", count = files.Count });
                    }
                }
                else
                {
                    return Json(new { result = true, message = "UserId didn't passed with profile photo upload", count = files.Count });
                }

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, message = ex.Message, count = files.Count });
            }
            return Json(new { result = true, message = "success", count = files.Count, imageUrl = imageUrl });
        }

        public async Task<IActionResult> Profile()
        {
            var dealer = new AgapeModelUser.User();
            dynamic mymodel = new ExpandoObject();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await GetUserByIdentity(userId);
            mymodel.Profile = user;
            try
            {
                //dealer = await GetDealerDetails(userId);
                //mymodel.Dealer = dealer;
                mymodel.Dealer = user;
                if (!(string.IsNullOrEmpty(user.CompanyName)))
                {
                    var carDetails = await GetCarDetailsDealer(userId);
                    mymodel.Car = carDetails;
                    mymodel.IsDealer = true;
                }
                else
                {
                    var carDetails = await GetCarDetailsUser(userId);
                    mymodel.Car = carDetails.Item1;
                    mymodel.Images = carDetails.Item2;
                    mymodel.IsDealer = false;
                }

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(mymodel);
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
                            lstCar = lstCar.Where(i => !invalidStatustoShow.Contains(i.Status)).ToList();
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

                                if (lstSubmittedCarIds.Contains(car.Id))
                                    car.Status = "Submitted";
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

        public async Task<AgapeModelCar.Car> GetCarDetails(string id)
        {
            var carDetails = new AgapeModelCar.Car();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlCar + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carDetails = await Response.Content.ReadAsAsync<AgapeModelCar.Car>();
                            if (carDetails != null && !(string.IsNullOrEmpty(carDetails.Id)))
                            {
                                var lstImages = await GetCarImageByOnwer(carDetails.Id);
                                if (lstImages != null && lstImages.Any())
                                {
                                    carDetails.Thumbnail = (lstImages.Where(i => i.Order == 1) != null && lstImages.Where(i => i.Order == 1).Any()) ? lstImages.Where(i => i.Order == 1).Select(i => i.Url).FirstOrDefault().ToString() : lstImages.FirstOrDefault().Url;
                                }
                                else
                                {
                                    carDetails.Thumbnail = defaultCarImageUrl;
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
            return carDetails;
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
        public async Task<(AgapeModelCar.Car, List<AgapeModelImage.Image>)> GetCarDetailsUser(string userId)
        {
            var car = new AgapeModelCar.Car();
            if (car.Video == null)
                car.Video = new AgapeModelCar.Video();
            var lstCarImages = new List<AgapeModelImage.Image>();
            try
            {
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); //logged in user id
                    string endpoint = apiBaseUrlCar + "Dealer/" + currentUserId;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var lstCar = await Response.Content.ReadAsAsync<List<AgapeModelCar.Car>>();
                            if (lstCar != null && lstCar.Any())
                            {
                                lstCar = lstCar.Where(i => !invalidStatustoShow.Contains(i.Status)).ToList(); //Car in open status
                                if (lstCar != null && lstCar.Any())
                                {
                                    car = lstCar.FirstOrDefault();
                                    lstCarImages = await GetCarImageByOnwer(car.Id);
                                }
                            }
                            if (car.Video == null)
                                car.Video = new AgapeModelCar.Video();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
                            return (car, lstCarImages);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return (car, lstCarImages);
        }

        public async Task<List<AgapeModelImage.Image>> GetCarImageByOnwer(string id)
        {
            var carImages = new List<AgapeModelImage.Image>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
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

        public async Task<IActionResult> MyProfile()
        {
            ViewBag.Title = "My Profile";
            ViewBag.Description = "My Profile";
            ViewBag.Keywords = "My Profile ";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await GetUserByIdentity(userId);
            return View(user);
        }

        public async Task<IActionResult> CarBidDetails()
        {
            ViewBag.Title = "Bid Details";
            ViewBag.Description = "Bid Details";
            ViewBag.Keywords = "Bid Details";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var carDetails = await GetCarDetailsDealer(userId);
            return View(carDetails);
        }

        public async Task<IActionResult> OfferDetails(string carId)
        {
            ViewBag.Title = "Offer Details";
            ViewBag.Description = "Offer Details";
            ViewBag.Keywords = "Offer Details";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var carDetails = await GetCarDetails(carId);
            ViewBag.CarDetail = carDetails;
            var bidDetails = await GetCarOfferDetails(carId);
            return View(bidDetails);
        }

        public async Task<IActionResult> Auction(string carId)
        {
            ViewBag.Title = "Auction";
            ViewBag.Description = "Auction";
            ViewBag.Keywords = "Auction";
            var staramt = _configure.GetValue<string>("StartAmount");
            var inc = _configure.GetValue<string>("Increment");
            ViewBag.start = staramt;
            ViewBag.Inc = inc;
            var carDetails = await GetCarDetails(carId);
            return View(carDetails);
        }

        public async Task<List<AgapeModelOffer.Offer>> GetCarOfferDetails(string carId)
        {
            var offerDetails = new List<AgapeModelOffer.Offer>();
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
                            if (lstOffers != null && lstOffers.Any())
                            {
                                var result = lstOffers.Where(i => i.CarId == carId && i.Type == "Offer");
                                if (result != null && result.Any())
                                {
                                    offerDetails = result.ToList();
                                }
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
            return offerDetails;
        }

        public async Task<List<AgapeModelBid.Bid>> GetCarBidDetails(string carId)
        {
            var bidDetails = new List<AgapeModelBid.Bid>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
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

        public async Task<IActionResult> MyDealership()
        {
            var dealer = new AgapeModelUser.User();
            ViewBag.Title = "My Dealership";
            ViewBag.Description = "My Dealership";
            ViewBag.Keywords = "My Dealership";
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                //dealer = await GetDealerDetails(userId);
                dealer = await GetUserByIdentity(userId);
                ViewBag.Countries = await GetCountryLookup();
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(dealer);
        }

        [HttpPost("MyDealerShipConfirmation")]
        public async Task<IActionResult> MyDealerShipConfirmation()
        {
            var dealer = new AgapeModelUser.User();
            try
            {
                dealer.Id = Request.Form["hdnDealerId"];
                dealer.CompanyName = Request.Form["txtCompanyName"];
                dealer.Phone = Request.Form["txtCompanyPhone"];
                dealer.FirstName = Request.Form["hdnFirstName"];
                dealer.LastName = Request.Form["hdnLastName"];
                dealer.Email = Request.Form["hdnEmailAddress"];
                dealer.UserType = "user";
                dealer.Idp = Request.Form["hdnIdp"];
                dealer.Password = Request.Form["hdnPassword"];
                dealer.ProfileUrl = Request.Form["hdnProfileUrl"];

                var paymentMethod = new List<string>();
                paymentMethod.Add(Request.Form["hdnCountryId"]);
                paymentMethod.Add(Request.Form["hdnStateId"]);

                dealer.PaymentMethods = paymentMethod;

                var address = new AgapeModelUser.Address();
                address.Country = Request.Form["hdnCountryValue"];
                address.Street = Request.Form["txtStreet"];
                address.City = Request.Form["txtCity"];
                address.State = Request.Form["cmbState"];
                address.Zip = Request.Form["txtZip"];
                address.Lat = "0";
                address.Lon = "0";
                dealer.Address = address;

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View("MyDealerShipConfirmation", dealer);
        }

        [HttpPost("CreateDealerShip")]
        public async Task<IActionResult> CreateDealerShip()
        {
            try
            {
                var dealer = new AgapeModelUser.User();
                dealer.Id = Request.Form["hdnDealerId"];
                dealer.CompanyName = Request.Form["txtCompanyName"];
                dealer.Phone = Request.Form["txtCompanyPhone"];
                dealer.FirstName = Request.Form["hdnFirstName"];
                dealer.LastName = Request.Form["hdnLastName"];
                dealer.Email = Request.Form["hdnEmailAddress"];
                dealer.UserType = "user";
                dealer.Idp = Request.Form["Idp"];
                dealer.Password = Request.Form["hdnPassword"];
                dealer.ProfileUrl = Request.Form["hdnProfileUrl"];

                var address = new AgapeModelUser.Address();
                address.Country = Request.Form["hdnCountryValue"];
                address.Street = Request.Form["txtStreet"];
                address.City = Request.Form["txtCity"];
                address.State = Request.Form["cmbState"];
                address.Zip = Request.Form["txtZip"];
                address.Lat = "0";
                address.Lon = "0";
                dealer.Address = address;

                if (!string.IsNullOrEmpty(dealer.Id))
                    await UpdateDealerDetails(dealer);
                else
                    await AddDealerDetails(dealer);

                //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                //HttpContext.Session.SetObjectAsJson(userId, dealer);
                //_httpContextAccessor.HttpContext.Session.SetString("test", "hai");
                //_httpContextAccessor.HttpContext.Session.SetString("test1", JsonConvert.SerializeObject(dealer));
                //HttpContext.Session.SetString("dealerDetails", JsonConvert.SerializeObject(dealer));
                //var priceDetails = HttpContext.Session.GetString("dealerDetails");
                //var finalDealer = JsonConvert.DeserializeObject<User>(priceDetails);

                //CookieOptions option = new CookieOptions();
                //option.Expires = DateTime.Now.AddMinutes(100);
                //Response.Cookies.Append("testKey", "testValue", option);

                //Create a payment
                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>
                                {
                                  new SessionLineItemOptions
                                  {
                                    PriceData = new SessionLineItemPriceDataOptions
                                    {
                                      UnitAmountDecimal = 99 * 100,
                                      Currency = "usd",
                                      ProductData = new SessionLineItemPriceDataProductDataOptions
                                      {
                                        Name = "ShopCarHere",
                                        Description = "DealerShip Charges"
                                      },

                                    },
                                    Quantity = 1,
                                  },
                                },
                    Mode = "payment",
                    SuccessUrl = paymentSuccessRedirectUrl,
                    CancelUrl = paymentErrorRedirectUrl,
                };

                var service = new SessionService();
                Session session = service.Create(options);

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }

            return View("Error");
        }

        public async Task<IActionResult> DealerShipSuccess()
        {
            ViewBag.Title = "DealerShip Success";
            ViewBag.Description = "DealerShip Success";
            ViewBag.Keywords = "DealerShip Success";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //var testValue = _httpContextAccessor.HttpContext.Session.GetString("test");
            //var dealerDetails = HttpContext.Session.GetString("dealerDetails");
            //var dealer = JsonConvert.DeserializeObject<User>(dealerDetails);

            var dealer = await GetUserByIdentity(userId);
            dealer.UserType = "dealer";
            var response = await UpdateDealerDetails(dealer);
            ViewBag.PaymentStatus = "Success";
            //ViewBag.DealerAddStatus = response == true ? "Success" : "Error";
            return View("DealerSuccess");
        }

        public async Task<IActionResult> DealerShipError()
        {
            ViewBag.Title = "DealerShip Error";
            ViewBag.Description = "DealerShip Error";
            ViewBag.Keywords = "DealerShip Error";
            ViewBag.PaymentStatus = "Error";
            return View("DealerError");
        }

        public async Task<bool> AddDealerDetails(AgapeModelUser.User dealer)
        {
            var status = false;
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(dealer), Encoding.UTF8, "application/json");
                var endpoint = apiBaseUrlDealer;

                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        status = true;
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Dealer Service");
                        status = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                status = false;
            }
            return status;
        }


        public async Task<bool> UpdateDealerDetails(AgapeModelUser.User dealer)
        {
            var status = false;
            dealer.ConfirmPassword = dealer.Password;
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(dealer), Encoding.UTF8, "application/json");
                var endpoint = apiBaseUrlDealer + dealer.Id;

                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        status = true;
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Dealer Service");
                        status = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                status = false;
            }
            return status;
        }

        public async Task<IActionResult> WatchListDetails(string carId)
        {
            ViewBag.Title = "WatchListDetails Details";
            ViewBag.Description = "WatchListDetails Details";
            ViewBag.Keywords = "WatchListDetails Details";
            var carDetails = await GetCarDetails(carId);
            ViewBag.CarDetail = carDetails;
            var watchListDetails = await GetWatchListDetails(carId);
            return View(watchListDetails);
        }

        public async Task<List<ModelAuctions.Auction>> GetWatchListDetails(string carId)
        {
            var watchDetails = new List<ModelAuctions.Auction>();
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
                                foreach (var item in watchDetails)
                                {
                                    var userDetail = await GetUserByIdentity(item.CreatedBy);
                                    item.DealerId = userDetail.Email;
                                    item.CarId = userDetail.FirstName;
                                    item.ApprovedBy = userDetail.LastName;
                                }
                            }
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
            return watchDetails;
        }

        public async Task<AgapeModelUser.User> GetDealerDetails(string id)
        {
            var dealer = new AgapeModelUser.User();
            dealer.Address = new DALModels.Address();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlDealer + id;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            dealer = await Response.Content.ReadAsAsync<AgapeModelUser.User>();
                            if (dealer.Address == null)
                                dealer.Address = new DALModels.Address();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Dealer Service");
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

        public async Task<AgapeModelUser.User> GetUserByIdentity(string id)
        {
            var user = new AgapeModelUser.User();
            user.Address = new DALModels.Address();
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
                                user.Address = new DALModels.Address();
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

        //PUT - Update the User
        public async Task<JsonResult> EditUser()
        {
            try
            {
                var dealerDetails = Request.Form["User"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var user = jsonSerializer.Deserialize<AgapeModel.User>(new JsonTextReader(reader));

                user.ConfirmPassword = user.Password;

                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
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

        //POST - Create New Dealer
        public async Task<JsonResult> AddDealer()
        {
            try
            {
                var dealerDetails = Request.Form["dealer"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var dealer = (AgapeModelUser.User)jsonSerializer.Deserialize(reader, typeof(AgapeModelUser.User));

                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(dealer), Encoding.UTF8, "application/json");
                var endpoint = apiBaseUrlDealer;

                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true, dealerId = dealer.Id });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Dealer Service");
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

        //PUT - Update the Dealer
        public async Task<JsonResult> EditDealer()
        {
            try
            {
                var dealerDetails = Request.Form["dealer"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var dealer = (AgapeModelUser.User)jsonSerializer.Deserialize(reader, typeof(AgapeModelUser.User));
                dealer.ConfirmPassword = dealer.Password;

                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(dealer), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlDealer + dealer.Id;

                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true, dealerId = dealer.Id });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Dealer Service");
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

        public async Task<SelectList> GetCountryLookup()
        {
            var lookUp = new List<LookUpValue>();
            try
            {
                var countries = await _serviceManager.GetCountries();
                foreach (var item in countries.OrderBy(i => i.Name))
                {
                    lookUp.Add(new LookUpValue() { Id = item.CountryCode, Value = item.Name });
                }
                return new SelectList(lookUp, "Id", "Value");
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return new SelectList(lookUp, "Id", "Value");
            }
        }

        public async Task<JsonResult> GetStateByCountry(string countryCode)
        {
            List<ServiceReference.State> states = new List<ServiceReference.State>();
            try
            {
                states = await _serviceManager.GetStates(countryCode);

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return Json(new { restult = true, states = states.OrderBy(i => i.Name) });
        }

        public async Task<List<AgapeModelPayment.PaymentMethod>> GetPaymentInformation()
        {
            var paymentDetails = new List<AgapeModelPayment.PaymentMethod>();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlPayment;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var paymentInfo = await Response.Content.ReadAsAsync<List<AgapeModelPayment.PaymentMethod>>();
                            if (paymentInfo != null && paymentInfo.Any())
                            {
                                var currentInfo = paymentInfo.Where(i => i.Owner == userId);
                                if (currentInfo != null && currentInfo.Any())
                                    paymentDetails = currentInfo.ToList();
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from payment Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return paymentDetails;
        }


        public async Task<List<AgapeModelPurchase.Purchase>> GetPurchaseInformation()
        {
            var purchaseDetails = new List<AgapeModelPurchase.Purchase>();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlPurchase;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var purchaseInfo = await Response.Content.ReadAsAsync<List<AgapeModelPurchase.Purchase>>();
                            if (purchaseInfo != null && purchaseInfo.Any())
                            {
                                var currentInfo = purchaseInfo.Where(i => i.Owner == userId);
                                if (currentInfo != null && currentInfo.Any())
                                    purchaseDetails = currentInfo.ToList();
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Purchase Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return purchaseDetails;
        }

        public async Task<JsonResult> SubmitAuction()
        {
            try
            {
                var dealerDetails = Request.Form["auction"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var auctionModel = (ModelAuctions.Auction)jsonSerializer.Deserialize(reader, typeof(ModelAuctions.Auction));
                auctionModel.CreatedDate = DateTime.Now;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                auctionModel.CreatedBy = userId;
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(auctionModel), Encoding.UTF8, "application/json");

                var endpoint = apiBaseUrlAuction;
                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from Auction Service");
                        return Json(new { result = false });
                    }
                }
                //return Json(new { result = true });

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false });
            }
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
        public async Task<JsonResult> EditStatusCar(string carId, string status)
        {
            try
            {
                var carDetails = await GetCarDetails(carId);



                //Get VehicleDetails from API
                var definition = new { result = false, errorMessage = "", carProperties = new AgapeModelCar.CarProperties() };
                var apiResponse = await GetVehicleInformation(carDetails.Vin);
                var deResponse = JsonConvert.DeserializeAnonymousType(JsonConvert.SerializeObject(apiResponse.Value), definition);

                var tempProperty = carDetails.Properties;
                deResponse.carProperties.Trim = tempProperty.Trim;
                if (string.IsNullOrEmpty(deResponse.errorMessage))
                {
                    carDetails.Properties = deResponse.carProperties;
                    carDetails.Status = status;

                    using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                    StringContent content = new StringContent(JsonConvert.SerializeObject(carDetails), Encoding.UTF8, "application/json");

                    string endpoint = apiBaseUrlCar + carDetails.Id;

                    using (var Response = await client.PutAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return Json(new { result = true, carId = carDetails.Id });
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
                            return Json(new { result = false });
                        }
                    }
                }
                else
                {
                    logHelper.LogError(deResponse.errorMessage);
                    return Json(new { result = false, errorMessage = deResponse.errorMessage });
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false });
            }
        }
        public async Task<JsonResult> GetVehicleInformation(string vinDetail)
        {
            var errorMessage = string.Empty;
            var properties = new AgapeModelCar.CarProperties();
            try
            {
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlVin + "DecodeVINValues/" + vinDetail + "?format=json";
                    //string endpoint = apiBaseUrlVin + vinDetail + "?format=json";
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var result = Response.Content.ReadAsStringAsync();
                            var vechicleDetail = JsonConvert.DeserializeObject<VehicleDetails>(result.Result);
                            if (vechicleDetail != null && !(string.IsNullOrEmpty(vechicleDetail.Message)) && vechicleDetail.Results != null && vechicleDetail.Results.Length > 0)
                            {
                                if (vechicleDetail.Results[0].ErrorCode != "0")
                                {
                                    errorMessage = vechicleDetail.Results[0].ErrorText;
                                }
                                else
                                {
                                    properties = vechicleDetail.Results[0];
                                }
                            }
                            else
                            {
                                logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the vehicle Details");
                                errorMessage = "Error occurred while getting the vehicle Details Please try again later";
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the vehicle Details");
                            errorMessage = "Error occurred while getting the vehicle Details Please try again later";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, errorMessage = "Error occurred while getting the vehicle Details Please try again later" });
            }
            return Json(new { result = (!(string.IsNullOrEmpty(errorMessage) ? false : true)), errorMessage = errorMessage, carProperties = properties });

        }
        public async Task<IActionResult> DetailOffer(string offerId, string carId)
        {
            var details = new AgapeModelOffer.Offer();
            try
            {
                var carDetails = await GetCarDetails(carId);
                ViewBag.CarDetail = carDetails;
                details = await ViewOffer(offerId);

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(details);
        }
        public async Task<AgapeModelOffer.Offer> ViewOffer(string OfferId)
        {
            var offerDetails = new AgapeModelOffer.Offer();

            try
            {

                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlOffers + OfferId;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            offerDetails = await Response.Content.ReadAsAsync<AgapeModelOffer.Offer>();

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
            return offerDetails;

        }
    }
}
