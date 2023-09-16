using Agape.Auctions.UI.Cars.Models;
using Agape.Auctions.UI.Cars.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AgapeModel = Agape.Auctions.Models.Cars;
using AgapeModelImage = Agape.Auctions.Models.Images;
using AgapeModelUser = Agape.Auctions.Models.Users;
using AgageModelCarReview = Agape.Auctions.Models.Cars.CarReview;
using Microsoft.AspNetCore.Authorization;

namespace Agape.Auctions.UI.Cars.Controllers
{
    //[Authorize]
    public class CarController : Controller
    {
        private readonly ILogger<CarController> _logger;
        private readonly IConfiguration configure;
        private readonly AzureStorageConfig storageConfig = null;
        private readonly string[] invalidStatustoShow = { "Closed", "Sold", "UnSold" };
        private readonly string apiBaseUrl;
        private readonly string apiBaseUrlCarImage;
        private readonly string apiBaseUrlVin;
        private readonly string defaultCarImageUrl;
        private readonly string apiBaseUrlUser;
        private readonly string apiBaseUrlCarReview;

        // private readonly string apiBaseUrlCarSearch;
        private LogHelper logHelper;

        public CarController(IConfiguration configuration, IOptions<AzureStorageConfig> config, ILogger<CarController> logger)
        {
            configure = configuration;
            storageConfig = config.Value;
            apiBaseUrl = configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            apiBaseUrlVin = configure.GetValue<string>("WebAPIBaseUrlVin");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlUser = configure.GetValue<string>("WebAPIBaseUrlUser");
            apiBaseUrlCarReview = configure.GetValue<string>("WebAPIBaseUrlCarReview");

            // apiBaseUrlCarSearch = configure.GetValue<string>("WebAPIBaseUrlCarSearch");
            _logger = logger;
            logHelper = new LogHelper(configure, _logger);
        }

        public async Task<IActionResult> View(string carId)
        {
            var model = new AgapeModel.Car();
            try
            {
                if (!string.IsNullOrEmpty(carId))
                    model = await GetCarDetails(carId);

                if (model.Video == null)
                    model.Video = new AgapeModel.Video();
                if (model.Thumbnail != defaultCarImageUrl)
                    model.HasImages = true;
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(model);
        }

        public async Task<IActionResult> Search()
        {
            try
            {
                var makeResult = await GetSearchFilters("1");
                ViewBag.Makes = makeResult;
                var modelResult = await GetSearchFilters("2");
                ViewBag.Models = modelResult;
                var modelYearResult = await GetSearchFilters("3");
                ViewBag.ModelYear = modelYearResult;
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View();
        }
       
        public IActionResult SearchResults(string make, string model, string startPrice, string endPrice, int yearFrom, int yearTo)
        {
            return ViewComponent("Car", new { viewName = "SearchResults", make = make, model = model, startPrice = startPrice, endPrice = endPrice, yearFrom = yearFrom, yearTo = yearTo });
        }

        public IActionResult CarImages(string view, string id)
        {
            return ViewComponent("Car", new { view = view, id = id });
        }
        public IActionResult FilterFav(string make, string model, string startPrice, string endPrice, int yearFrom, int yearTo)
        {
            return ViewComponent("Car", new { viewName = "FilterFav", make = make, model = model, startPrice = startPrice, endPrice = endPrice, yearFrom = yearFrom, yearTo = yearTo });
        }
        public async Task<IActionResult> ViewReview(string carId)
        {
            var carReviews = GetReviewsAndComments(carId);
            ViewBag.CarReviews = carReviews;
            return PartialView("_ViewReview");
        }
        public async Task<IActionResult> details(string carId)
        {
            var carDetails = await GetCarDetails(carId);
            ViewBag.CarDetails = carDetails;
            if (carDetails != null)
            {
                var carOwner = carDetails.Owner;
                var user = await GetUserByIdentity(carOwner);
                ViewBag.UserDetails = user;
            }

           
            //if(carDetails.Video != null)
            //    ViewBag.VideoUrl = carDetails.Video.Url;
            //ViewBag.Description = carDetails.Description;
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

        #region Reviews and Comments

        public async Task<JsonResult> SaveCarReview(string message,string carId,int rate,string type,string title)
        {
            var reviewDetails = new AgageModelCarReview();
            
            //var carDetails = Request.Form["carReview"];
            //var jsonSerializer = new JsonSerializer();
            //var reader = new StringReader(carDetails);
            //var carReview = (AgageModelCarReview)jsonSerializer.Deserialize(reader, typeof(AgageModelCarReview));

            //carReview.CreatedDate = System.DateTime.Now;
            //carReview.Deleted = false;
            using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
            
            string endpoint = apiBaseUrlCarReview;
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var users = await GetUserByIdentity(userId);
                reviewDetails.Type = type;
                reviewDetails.Title = title;
                reviewDetails.FirstName = users.FirstName;
                reviewDetails.LastName = users.LastName;
                reviewDetails.Comments = message;
                reviewDetails.CarId = carId;
                reviewDetails.CreatedDate = System.DateTime.Now;
                reviewDetails.Deleted = false;
                reviewDetails.Rating = rate;
                StringContent content = new StringContent(JsonConvert.SerializeObject(reviewDetails), Encoding.UTF8, "application/json");
                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Json(new { saveResult = true });
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from car review service");
                        return Json(new { saveResult = false });
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { saveResult = false });
            }
        }
        public async Task<IActionResult> Ratings(string carId)
        {
            var carReviews =await  GetReviewsAndComments(carId);
            var rating = new List<AgageModelCarReview>();
            decimal rate = 0.0M;
            foreach(var review in carReviews)
            {
                if(review.Rating != 0)
                {
                    rating.Add(review);
                }
            }
            if (rating.Count == 0)
            {
                rate = 0.0M;
            }
            else
            {
                int sum = 0;
                int div = rating.Count;
                foreach(var item in rating)
                {
                    sum += item.Rating;
                }
                rate = sum / div;
            }
            return PartialView("_Rating", rate);
        }
        public async Task<List<CarReviewDetails>> GetReviewsAndComments(string carId)
        {
            List<CarReviewDetails> lstCarReview = new List<CarReviewDetails>();
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                string endpoint = apiBaseUrlCarReview + "GetReviewByCar/" + carId;
                using (var Response = await client.GetAsync(endpoint))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        lstCarReview = await Response.Content.ReadAsAsync<List<CarReviewDetails>>();
                        if (lstCarReview != null && lstCarReview.Any())
                        {
                            foreach(var item in lstCarReview)
                            {
                                var replyReviews = lstCarReview.Where(i => i.Title == item.Id);
                                item.ReplyReviewDetails = replyReviews != null && replyReviews.Any() ? replyReviews.ToList() : new List<CarReviewDetails>();
                            }
                        }
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

        #endregion


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

        public async Task<List<AgapeModel.Car>> GetCarsByOwner(string dealerId)
        {
            var lstCar = new List<AgapeModel.Car>();
            try
            {
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrl + "Dealer/" + dealerId;

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

        [Authorize]
        public async Task<IActionResult> AddEditCar(string carId)
        {
            var eligibleToAddCar = true;
            if(string.IsNullOrEmpty(carId))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await GetUserByIdentity(userId);
                if (user.UserType.ToUpper() != "DEALER")
                {
                    var lstCarDetails = await GetCarsByOwner(userId);
                    var totalCarsCount = lstCarDetails.Count();
                    if (totalCarsCount >= 1)
                        eligibleToAddCar = false;
                }
            }

            ViewBag.EligibleToAddCar = eligibleToAddCar;

            var model = new AgapeModel.Car();
            model.Properties = new AgapeModel.CarProperties();
            model.Video = new AgapeModel.Video();
            try
            {
                ViewBag.Status = GetStatus();
                ViewBag.Years = GetYears();
                var carMakeDetails = await GetCarMakeDetails();
                ViewBag.CarMake = carMakeDetails;

                //var carMakeId = carMakeDetails.FirstOrDefault().Value;
                var carMakeId = string.Empty;
                if (!string.IsNullOrEmpty(carId))
                {
                    model = await GetCarDetails(carId);
                    carMakeId = carMakeDetails.Where(i => i.Text == model.Make).FirstOrDefault().Value;

                    if(model != null && model.Properties != null)
                    {
                        model.Make = model.Properties.MakeID;
                        model.Model = model.Properties.ModelID;
                    }
                }

                var carModelDetails = await GetCarModelDetails(carMakeId);
                var definition = new { result = false, errorMessage = "", modelDetails = new List<CarModel>() };
                var deResponse = JsonConvert.DeserializeAnonymousType(JsonConvert.SerializeObject(carModelDetails.Value), definition);

                var lookUp = new List<LookUpValue>();
                foreach (var item in deResponse.modelDetails)
                {
                    lookUp.Add(new LookUpValue() { Id = item.Model_ID.ToString(), Value = item.Model_Name.ToString() });
                }
               
                var carModelFinalDetails = new SelectList(lookUp, "Id", "Value");
                ViewBag.CarModel = carModelFinalDetails;

                if (model.Properties == null)
                    model.Properties = new AgapeModel.CarProperties();

                if (model.Video == null)
                    model.Video = new AgapeModel.Video();
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(model);
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

        // GET: CarController
        public async Task<IActionResult> Index()
        {
            var lstCar = new List<AgapeModel.Car>();
            try
            {
                lstCar = lstCar.Where(i => !invalidStatustoShow.Contains(i.Status)).ToList();
                var lstCarImages = await GetAllCarImages();
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); //logged in user id
                    string endpoint = apiBaseUrl + "Dealer/" + currentUserId;

                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            lstCar = await Response.Content.ReadAsAsync<List<AgapeModel.Car>>();
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
                                        car.Video = new AgapeModel.Video();
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
            ViewBag.status_all = "All";
            return View(lstCar);
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
                logHelper.LogError(ex.ToString());
            }
            return carImages;
        }
        //POST - Create New Car
        public async Task<JsonResult> AddCar()
        {
            try
            {
                var carDetails = Request.Form["car"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(carDetails);
                var car = (AgapeModel.Car)jsonSerializer.Deserialize(reader, typeof(AgapeModel.Car));
                // car.Id = Guid.NewGuid().ToString();

                //Get VehicleDetails from API
                var definition = new { result = false, errorMessage = "", carProperties = new AgapeModel.CarProperties() };
                var apiResponse = await GetVehicleInformation(car.Vin);
                var deResponse = JsonConvert.DeserializeAnonymousType(JsonConvert.SerializeObject(apiResponse.Value), definition);

                var tempProperty = car.Properties;
                deResponse.carProperties.Trim = tempProperty.Trim;

                if (string.IsNullOrEmpty(deResponse.errorMessage))
                {
                    car.Properties = deResponse.carProperties;
                    car.Owner = User.FindFirstValue(ClaimTypes.NameIdentifier); //logged in user id
                    using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                    StringContent content = new StringContent(JsonConvert.SerializeObject(car), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrl;

                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return Json(new { result = true, carId = car.Id });
                        }
                        else
                        {
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

        //PUT - Update the car
        public async Task<JsonResult> EditCar()
        {
            try
            {
                var carDetails = Request.Form["car"];
                var jsonSerializer = new JsonSerializer();
                var reader = new StringReader(carDetails);
                var car = (AgapeModel.Car)jsonSerializer.Deserialize(reader, typeof(AgapeModel.Car));

                car.Owner = User.FindFirstValue(ClaimTypes.NameIdentifier); //logged in user id

                //Get VehicleDetails from API
                var definition = new { result = false, errorMessage = "", carProperties = new AgapeModel.CarProperties() };
                var apiResponse = await GetVehicleInformation(car.Vin);
                var deResponse = JsonConvert.DeserializeAnonymousType(JsonConvert.SerializeObject(apiResponse.Value), definition);

                var tempProperty = car.Properties;
                deResponse.carProperties.Trim = tempProperty.Trim;
                if (string.IsNullOrEmpty(deResponse.errorMessage))
                {
                    car.Properties = deResponse.carProperties;
                    car.Owner = User.FindFirstValue(ClaimTypes.NameIdentifier); //logged in user id

                    using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                    StringContent content = new StringContent(JsonConvert.SerializeObject(car), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrl + car.Id;

                    using (var Response = await client.PutAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return Json(new { result = true, carId = car.Id });
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

        //DELETE - Remove the car
        public async Task<JsonResult> RemoveCar()
        {
            try
            {
                var carId = Request.Form["carId"];

                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrl + carId;

                    using (var Response = await client.DeleteAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return Json(new { SaveResult = true });
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
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

        public async Task<JsonResult> RemoveCarImageByCarImageId(string carId, string carImageId, string imageUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlCarImage + carImageId;

                    using (var Response = await client.DeleteAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            await RemoveCarImageFromStorage(carId, imageUrl);
                            return Json(new { result = true });

                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from image Service");
                            return Json(new { result = false, message = "Error while delete the image from table" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, message = ex.ToString() });
            }

        }

        public async Task<JsonResult> RemoveCarImage()
        {
            try
            {
                var carImageId = Request.Form["carImageId"];
                var carId = Request.Form["carId"];
                var imageUrl = Request.Form["imageUrl"];

                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlCarImage + carImageId;

                    using (var Response = await client.DeleteAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            await RemoveCarImageFromStorage(carId, imageUrl);
                            return Json(new { result = true });

                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from image Service");
                            return Json(new { result = false, message = "Error while delete the image from table" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, message = ex.ToString() });
            }

        }

        public async Task<JsonResult> ReorderImages()
        {
            try
            {
                var lstImages = Request.Form["lstImages[]"];

                var orderCount = 1;
                foreach (var image in lstImages)
                {
                    using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                    {
                        string endpoint = apiBaseUrlCarImage + image;

                        using (var Response = await client.GetAsync(endpoint))
                        {
                            if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var carImage = await Response.Content.ReadAsAsync<AgapeModelImage.Image>();
                                carImage.Order = orderCount;
                                var response = await UpdateCarImage(carImage);

                                if (!response.Item1)
                                    return Json(new { result = false, message = response.Item2 });
                            }
                            else
                            {
                                logHelper.LogError(Response.ReasonPhrase + " Error from image Service");
                                return Json(new { result = false, message = "Error while reorder the image" });
                            }
                        }
                    }
                    orderCount += 1;
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, message = ex.ToString() });
            }
            return Json(new { result = true });

        }

        public async Task<(bool, string)> UpdateCarImage(AgapeModelImage.Image carImage)
        {
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(carImage), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlCarImage + carImage.Id;

                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return (true, "success");
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from image Service");
                        return (false, "Error while saving the image details");
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return (false, $"Error while saving the image details, Details: {ex.Message }");
            }
        }

        public async Task<bool> RemoveCarImageFromStorage(string carId, string imageUrl)
        {
            try
            {
                var currentImageFormat = string.Empty;
                var currentImageName = string.Empty;
                var imageName = imageUrl.Split("/");
                if (imageName.Length > 0)
                {
                    var imageDetail = imageName[imageName.Length - 1];
                    if (imageDetail != null)
                    {
                        var imageData = imageDetail.Split('.');
                        if (imageData != null && imageData.Length > 0)
                        {
                            currentImageName = imageData[0];
                            currentImageFormat = imageData[1];
                        }
                    }
                }

                var actualImage = currentImageName + "." + currentImageFormat;
                var thumnailImage = currentImageName + "_tn." + currentImageFormat;
                var smallImage = currentImageName + "_small." + currentImageFormat;
                var listingImage = currentImageName + "_listing." + currentImageFormat;
                var gridImage = currentImageName + "_grid." + currentImageFormat;
                var mediumImage = currentImageName + "_medium." + currentImageFormat;

                var lstImages = new List<string>();
                lstImages.Add(actualImage);
                lstImages.Add(thumnailImage);
                lstImages.Add(smallImage);
                lstImages.Add(listingImage);
                lstImages.Add(gridImage);
                lstImages.Add(mediumImage);

                var result = await StorageHelper.RemoveFileFromStorage(lstImages, carId, storageConfig);
                return result;
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return false;
            }

        }

        #region UtilityMethods
        public SelectList GetStatus()
        {
            var lookUp = new List<LookUpValue>();
            lookUp.Add(new LookUpValue() { Id = "open", Value = "Opem" });
            lookUp.Add(new LookUpValue() { Id = "closed", Value = "Opem" });
            lookUp.Insert(0, new LookUpValue() { Id = "0", Value = "--Select a item--" });
            return new SelectList(lookUp, "Id", "Value");
        }


        #region CarImages

        [HttpPost]
        public async Task<IActionResult> UploadCarImages(IList<IFormFile> files)
        {
            bool uploadResult = false;
           
            try
            {
                if (files.Count == 0)
                    return Json(new { result = false, message = "No images available to upload, Please check the input and try again", count = files.Count });

                var carId = Request.Form["carId"];
                var currentImageCount = int.Parse(Request.Form["currentImageCount"]);

                if (!string.IsNullOrEmpty(carId))
                {
                    List<AgapeModelImage.Image> lstCarImages = await GetCarImageByOnwer(carId);

                    foreach (var formFile in files)
                    {
                        if (StorageHelper.IsImage(formFile))
                        {
                            if (formFile.Length > 0)
                            {
                                using (Stream stream = formFile.OpenReadStream())
                                {
                                    var imageUrl = "https://" + storageConfig.AccountName + ".blob.core.windows.net/" + storageConfig.ImageContainer + "/" + carId + "/" + formFile.FileName;

                                    if (lstCarImages != null && lstCarImages.Any())
                                    {
                                        var findImage = lstCarImages.Where(i => i.Url == imageUrl);
                                        if (findImage != null && findImage.Any())
                                        {
                                            var findImageId = findImage.FirstOrDefault().Id;
                                            var removeImageResult = await RemoveCarImageByCarImageId(carId, findImageId, imageUrl);
                                            // var removeImageResult = await RemoveCarImageFromStorage(carId, imageUrl);
                                        }
                                    }
                                    uploadResult = await StorageHelper.UploadFileToStorage(stream, formFile.FileName, carId, storageConfig);

                                    if (uploadResult)
                                    {
                                        //Call the api to save the data in to cosmosdb
                                        var carImage = new AgapeModelImage.Image()
                                        {
                                            // Id = Guid.NewGuid().ToString(),
                                            Url = "https://" + storageConfig.AccountName + ".blob.core.windows.net/" + storageConfig.ImageContainer + "/" + carId + "/" + formFile.FileName,
                                            // Type = "Image",
                                            Owner = carId,
                                            IsProcessed = true,
                                            Order = currentImageCount + 1
                                        };
                                        currentImageCount = currentImageCount + 1;
                                        var response = await SaveCarImages(carImage);
                                        if (!response.Item1)
                                            return Json(new { result = false, message = response.Item2, count = files.Count });
                                    }
                                }
                            }
                        }
                        else
                        {
                            logHelper.LogError("Upload image - Trying to upload the invalid image format");
                            return Json(new { result = false, message = "Invalid image format type, Please try again with valid image", count = files.Count });
                        }
                    }
                }
                else
                {
                    return Json(new { result = true, message = "Car details is  not aavailable to save the image", count = files.Count });
                }

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, message = ex.Message, count = files.Count });
            }
            return Json(new { result = true, message = "success",count = files.Count });
        }

        public async Task<(bool, string)> SaveCarImages(AgapeModelImage.Image carImage)
        {
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(carImage), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlCarImage;

                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return (true, "success");
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " Error from CarImage Service");
                        return (false, "Error while saving the image details");
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return (false, $"Error while saving the image details, Details: {ex.Message }");
            }
        }

        public async Task<JsonResult> GetVehicleInformation(string vinDetail)
        {
            var errorMessage = string.Empty;
            var properties = new AgapeModel.CarProperties();
            try
            {
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlVin + "DecodeVINValues/" + vinDetail + "?format=json";
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


        public async Task<SelectList> GetCarMakeDetails()
        {
            var errorMessage = string.Empty;
            var lookUp = new List<LookUpValue>();
            try
            {
                using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlVin + "GetMakesForVehicleType/car?format=json";
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var result = Response.Content.ReadAsStringAsync();
                            var makeDetails = JsonConvert.DeserializeObject<CarMakeDetails>(result.Result);
                            if (makeDetails != null && makeDetails.Results != null && makeDetails.Results.Any())
                            {
                                if (makeDetails.Message.ToUpper() == "Response returned successfully".ToUpper())
                                {
                                    //success
                                    foreach(var item in makeDetails.Results.OrderBy(i => i.MakeName))
                                    {
                                        lookUp.Add(new LookUpValue() { Id = item.MakeId.ToString(), Value = item.MakeName.ToString() });
                                    }
                                    lookUp.Insert(0, new LookUpValue() { Id = "0", Value = "--Select a Make--" });
                                }
                                else
                                {
                                    logHelper.LogError(makeDetails.Message + " Error occurred while getting the carmake Details");
                                }
                            }
                            else
                            {
                                logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the carmake Details");
                            }
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the carmake Details");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return new SelectList(lookUp, "Id", "Value");
        }


        public async Task<JsonResult> GetCarModelDetails(string carMakeId)
        {
            var errorMessage = string.Empty;
            var modelDetails = new List<CarModel>();
            try
            {
                if(!string.IsNullOrEmpty(carMakeId))
                {

                    using (HttpClient client = new HttpClient(new CustomHttpClientHandler(configure)))
                    {
                        string endpoint = apiBaseUrlVin + "GetModelsForMakeId/" + carMakeId + "?format=json";

                        using (var Response = await client.GetAsync(endpoint))
                        {
                            if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var result = Response.Content.ReadAsStringAsync();
                                var makeDetails = JsonConvert.DeserializeObject<CarMakeDetails>(result.Result);
                                if (makeDetails != null && makeDetails.Results != null && makeDetails.Results.Any())
                                {
                                    if (makeDetails.Message.ToUpper() == "Response returned successfully".ToUpper())
                                    {
                                        //success
                                        foreach (var item in makeDetails.Results.OrderBy(i => i.Model_Name))
                                        {
                                            modelDetails.Add(new CarModel() { Model_ID = item.Model_ID, Model_Name = item.Model_Name });
                                        }
                                    }
                                    else
                                    {
                                        errorMessage = makeDetails.Message;
                                    }
                                }
                                else
                                {
                                    logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the Model Details for make" + carMakeId);
                                    errorMessage = "Error occurred while getting the Model Details Please try again later";
                                }
                            }
                            else
                            {
                                logHelper.LogError(Response.ReasonPhrase + " Error occurred while getting the Model Details for make" + carMakeId);
                                errorMessage = "Error occurred while getting the Model Details Please try again later";
                            }
                        }
                    }

                }
                modelDetails.Insert(0, new CarModel() { Model_ID = 0, Model_Name = "--Select a Model--" });

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
                return Json(new { result = false, errorMessage = "Error occurred while getting the Model Details Please try again later" });
            }
            return Json(new { result = (!(string.IsNullOrEmpty(errorMessage) ? false : true)), errorMessage = errorMessage, modelDetails = modelDetails });
        }



        #endregion

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
                if (id == "3")
                {
                    lookUp = lookUp.OrderByDescending(i =>int.Parse(i.Value)).ToList();
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

        public SelectList GetYears()
        {
            var lookUp = new List<LookUpValue>();
            var currentYear = DateTime.Now.Year;
            for(int i=1980;i<= currentYear; i++)
            {
                lookUp.Add(new LookUpValue() { Id = i.ToString(), Value = i.ToString() });
            }
            lookUp.Insert(0, new LookUpValue() { Id = "0", Value = "--Select a Year--" });
            return new SelectList(lookUp, "Id", "Value");
        }

        #endregion
    }

}
