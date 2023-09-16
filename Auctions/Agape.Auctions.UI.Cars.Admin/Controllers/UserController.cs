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
using AgapeModelPayment = DataAccessLayer.Models;
using AgapeModelPurchase = DataAccessLayer.Models;
using ModelAuctions = DataAccessLayer.Models;
using AgapeModelBid = DataAccessLayer.Models;

using Agape.Auctions.UI.Cars.Admin.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Agape.Auctions.UI.Cars.Admin.Models;
using AgapeAPI.Core;
using Microsoft.Extensions.Logging;

namespace Agape.Auctions.UI.Cars.Admin.Controllers
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
        private readonly string[] invalidStatustoShow = { "Closed", "Sold", "UnSold" };
        private readonly IServiceManager _serviceManager;
        private readonly ILogger<UserController> _logger;
        private LogHelper logHelper;
        private readonly string apiBaseUrlPurchase;
        private readonly string apiBaseUrlVin;



        public UserController(IConfiguration configuration, IServiceManager serviceManager, ILogger<UserController> logger)
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
            _serviceManager = serviceManager;
            logHelper = new LogHelper(_configure, _logger);


        }
        public ActionResult StatusList(string view, string status,string brand,string model,string fuel,string year)
        {
            return ViewComponent("Status", new { view = view, brand= brand, model = model, fuel= fuel, year = year, status = status });
        }

        public async Task<ActionResult> Amount(string id)
        {

            var detail = new ModelAuctions.Auction();   
            var Car = await GetCarDetails(id);
            var auction = await GetAuctionCars("Submitted");


            if (Car != null)
            {
                foreach (var item in auction)
                {
                    if (item.CarId == Car.Id)
                    {
                        detail = item;
                    }
                }
                detail.Reserve = (int)Decimal.Truncate(detail.Reserve);
                detail.StartAmount = (int)Decimal.Truncate(detail.StartAmount);
                detail.Increment = (int)Decimal.Truncate(detail.Increment);
            }

            return PartialView("_Amount", detail);
        }
        public async Task<ActionResult> Resubmit(string id,string auctionStatus)
        {

            var detail = new ModelAuctions.Auction();
            var Car = await GetCarDetails(id);
            var auction = await GetAuctionCars(auctionStatus);

            if (Car != null)
            {
                foreach (var item in auction)
                {
                    if (item.CarId == Car.Id)
                    {
                        detail = item;
                    }
                }
                detail.Reserve = (int)Decimal.Truncate(detail.Reserve);
                detail.StartAmount = (int)Decimal.Truncate(detail.StartAmount);
                detail.Increment = (int)Decimal.Truncate(detail.Increment);
            }

            return PartialView("_Resubmit", detail);
        }
        public IActionResult AuctionResults(string make, string model, int yearFrom, int yearTo)
        {
            return ViewComponent("Auctions", new { viewName = "AuctionResults", make = make, model = model, yearFrom = yearFrom, yearTo = yearTo });
        }
        public async Task<ActionResult> Filters(string status = "Submitted")
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
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return PartialView("_Filter", Car);
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
                    string endpoint = apiBaseUrlCar;
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
                                    logHelper.LogInformation("Method Name : GetAllCarDetails, Car Loop, Current Car :" + car.Id);

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

        public async Task<ActionResult> ApprovedCarz()
        {



            var Car = new List<AgapeModelCar.Car>();


            try
            {

                var carDetails = await GetAllCarDetailsDealer("Approved");

                Car = carDetails;




            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(Car);

        }
        public async Task<ActionResult> CanceledCarz()
        {






            var Car = new List<AgapeModelCar.Car>();


            try
            {

                var carDetails = await GetAllCarDetailsDealer("cancelled");
                Car = carDetails;




            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(Car);

        }
        public async Task<ActionResult> SoldCarz()
        {






            var Car = new List<AgapeModelCar.Car>();


            try
            {

                var carDetails = await GetAllCarDetailsDealer("Sold");
                Car = carDetails;




            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(Car);

        }
        public async Task<ActionResult> UnsoldCarz()
        {
            var Car = new List<AgapeModelCar.Car>();
            try
            {
                var carDetails = await GetAllCarDetailsDealer("UnSold");
                Car = carDetails;
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(Car);

        }

        public async Task<IActionResult> Profiles()
        {
            var dealer = new AgapeModelUser.User();
            dynamic mymodel = new ExpandoObject();
            var adminID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminUser = await GetUserByIdentity(adminID);
            var userId = User.FindAll(ClaimTypes.NameIdentifier);
            var user = new List<AgapeModelUser.User>();
            mymodel.Car = new List<AgapeModelCar.Car>();

            mymodel.Images = new List<AgapeModelImage.Image>();
            foreach (var id in userId)
            {

                user.Add(await GetUserByIdentity(id.Value));
            }
            mymodel.Profile = adminUser;
            try
            {
                mymodel.Dealer = adminUser;
                var car = await GetAllCarDetailsDealer("Submitted");
                mymodel.Car = car;

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(mymodel);
        }

        public async Task<IActionResult> Profile()
        {
            var dealer = new AgapeModelUser.User();
            dynamic mymodel = new ExpandoObject();
            var adminID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminUser = await GetUserByIdentity(adminID);
            var userId = User.FindAll(ClaimTypes.NameIdentifier);
            var user = new List<AgapeModelUser.User>();
            mymodel.Car = new List<AgapeModelCar.Car>();

            mymodel.Images = new List<AgapeModelImage.Image>();
            foreach (var id in userId)
            {

                user.Add(await GetUserByIdentity(id.Value));
            }
            mymodel.Profile = adminUser;
            try
            {
                //dealer = await GetDealerDetails(userId);
                //mymodel.Dealer = dealer;
                mymodel.Dealer = adminUser;
                var car = await GetAllCarDetailsDealer("Submitted");
                mymodel.Car = car;
                //foreach (var user1 in user)
                //{
                //    if (!(string.IsNullOrEmpty(user1.CompanyName)))
                //    {
                //        mymodel.IsDealer = true;
                //        var carDetails = await GetCarDetailsDealer(user1.Id);
                //        mymodel.Car.add(carDetails);

                //    }
                //    else
                //    {
                //        mymodel.IsDealer = false;
                //        var carDetails = await GetCarDetailsUser(user1.Id);
                //        mymodel.Car.add(carDetails.Item1);
                //        mymodel.Images.add(carDetails.Item2);

                //    }
                //}
                //var carDetails = Approved();

                //mymodel.Car=carDetails;


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
                                car = lstCar.FirstOrDefault();
                                lstCarImages = await GetCarImageByOnwer(car.Id);
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

            }
            return View(dealer);
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
                var user = (AgapeModelUser.User)jsonSerializer.Deserialize(reader, typeof(AgapeModelUser.User));
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
        public async Task<SelectList> GetCountryLookup()
        {
            var lookUp = new List<LookUpValue>();
            try
            {
                var countries = await _serviceManager.GetCountries();
                foreach (var item in countries)
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
            return Json(new { restult = true, states = states });
        }
        public async Task<JsonResult> EditUserAdmin()
        {



            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var users = await GetUserByIdentity(userId);
                users.ConfirmPassword = users.Password;



                if (users.UserType == "user" || users.UserType == "Dealer")
                {
                    users.UserType = "ContentAuthor";
                }
                else
                {
                    return Json(new { result = true, userId = users.Id });
                }
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(users), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlUser + users.Id;



                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { result = true, userId = users.Id });
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


        public async Task<bool> RemoveBiddingDetails(string bidId)
        {
            var status = false;
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlBidding + bidId;

                    using (var Response = await client.DeleteAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            status = true;
                        }
                        else
                        {
                            status = false;
                            logHelper.LogError(Response.ReasonPhrase + " Error from Bidding Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                status = false;
                logHelper.LogError(ex.ToString());
            }
            return status;
        }


        public async Task<JsonResult> SubmitAuction()
        {
            try
            {
                var dealerDetails = Request.Form["auction"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(dealerDetails);
                var auctionModel = (ModelAuctions.Auction)jsonSerializer.Deserialize(reader, typeof(ModelAuctions.Auction));
                var aucDetails = new ModelAuctions.Auction();
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var users = await GetUserByIdentity(userId);
                aucDetails = await GetAuctionDetails(auctionModel.Id);
                aucDetails.ApprovedBy = users.FirstName;
                aucDetails.Increment = auctionModel.Increment;
                aucDetails.AuctionDays = auctionModel.AuctionDays;
                aucDetails.StartAmount = auctionModel.StartAmount;
                aucDetails.Reserve = auctionModel.Reserve;
                if (auctionModel.Status != "Resubmit")
                {
                    aucDetails.Status = auctionModel.Status;
                    aucDetails.ApprovedDate = DateTime.Now;
                }
                else if  (auctionModel.Status == "Resubmit")
                {
                    if(aucDetails.Status == "UnSold")
                    {
                        aucDetails.Status = "Approved";
                        aucDetails.ApprovedDate = DateTime.Now;
                        await EditStatusCar(aucDetails.CarId, "Approved");
                    }
                   
                    var bidDetails = await GetCarBidDetails(aucDetails.CarId);
                    if(bidDetails != null && bidDetails.Any())
                    {
                        foreach(var bid in bidDetails)
                        {
                            var bidRemovalStatus = await RemoveBiddingDetails(bid.Id);
                        }
                    }
                    
                }

                var endpoint = apiBaseUrlAuction + aucDetails.Id;
                StringContent content = new StringContent(JsonConvert.SerializeObject(aucDetails), Encoding.UTF8, "application/json");
                using (var Response = await client.PutAsync(endpoint, content))
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
        public async Task<ModelAuctions.Auction> GetAuctionDetails(string id)
        {
            var aucDetails = new ModelAuctions.Auction();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(_configure)))
                {
                    string endpoint = apiBaseUrlAuction + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            aucDetails = await Response.Content.ReadAsAsync<ModelAuctions.Auction>();
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
            return aucDetails;
        }

        public async Task<JsonResult> CancelAuction(string Id, string stat)
        {
            try
            {
                var auction = new ModelAuctions.Auction();
                var Car = await GetCarDetails(Id);
                var auction1 = await GetAuctionCars("Submitted");


                if (Car != null)
                {
                    foreach (var item in auction1)
                    {
                        if (item.CarId == Car.Id)
                        {
                            auction = item;
                        }
                    }
                }


                using HttpClient client = new HttpClient(new CustomHttpClientHandler(_configure));


                auction.Status = stat;
                auction.ApprovedDate = DateTime.Now;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var users = await GetUserByIdentity(userId);

                auction.ApprovedBy = users.FirstName;
                var endpoint = apiBaseUrlAuction + auction.Id;
                StringContent content = new StringContent(JsonConvert.SerializeObject(auction), Encoding.UTF8, "application/json");
                using (var Response = await client.PutAsync(endpoint, content))
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
        public async Task<List<ModelAuctions.Auction>> GetAuctionCars(string stat)
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

    }

}
