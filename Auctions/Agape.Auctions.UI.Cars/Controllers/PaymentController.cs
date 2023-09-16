using Agape.Auctions.UI.Cars.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using AgapeModelCar = Agape.Auctions.Models.Cars;
using AgapeModelImage = Agape.Auctions.Models.Images;
using AgapeModelPurchase = Agape.Auctions.Models.Puchases;
using AgapeModelPayment = Agape.Auctions.Models.PaymentMethods;
using AgapeModels = Agape.Auctions.Models;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Stripe;
using Stripe.Checkout;

namespace Agape.Auctions.UI.Cars.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration configure;
        private readonly string apiBaseUrlCar;
        private readonly string apiBaseUrlCarImage;
        private readonly string defaultCarImageUrl;
        private readonly string apiBaseUrlPurchase;
        private readonly string apiBaseUrlPayment;
        private readonly int CarAdvanceAmountPercentage;
        private readonly int CarTaxPercentage;
        private readonly int GstPercentage;
        private readonly IServiceManager _serviceManager;
        private readonly int advanceThresholdAmount;
        private readonly string stripeAPIKey;
        private readonly string paymentSuccessRedirectUrl;
        private readonly string paymentErrorRedirectUrl;

        private LogHelper logHelper;

        public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger, IServiceManager serviceManager)
        {
            configure = configuration;
            apiBaseUrlCar = configure.GetValue<string>("WebAPIBaseUrlCar");
            apiBaseUrlCarImage = configure.GetValue<string>("WebAPIBaseUrlCarImage");
            defaultCarImageUrl = configure.GetValue<string>("DefaultCarImageUrl");
            apiBaseUrlPurchase = configure.GetValue<string>("WebAPIBaseUrlPurchase");
            apiBaseUrlPayment = configure.GetValue<string>("WebAPIBaseUrlPayment");
            CarAdvanceAmountPercentage = configure.GetValue<int>("CarAdvanceAmountPercentage");
            CarTaxPercentage = configure.GetValue<int>("CarTaxPercentage");
            GstPercentage = configure.GetValue<int>("GstPercentage");
            advanceThresholdAmount = configure.GetValue<int>("AdvanceThresholdAmount");
            stripeAPIKey = configure.GetValue<string>("StripeAPIKey");
            paymentSuccessRedirectUrl = configure.GetValue<string>("PaymentSuccessRedirectUrl");
            paymentErrorRedirectUrl = configure.GetValue<string>("PaymentErrorRedirectUrl");
            _serviceManager = serviceManager;

            _logger = logger;
            logHelper = new LogHelper(configure, _logger);

            StripeConfiguration.ApiKey = stripeAPIKey;
        }

        public async Task<IActionResult> OrderConfirmation(string id)
        {
            ViewBag.Title = "Payment Confirmation";
            ViewBag.Description = "Payment Confirmation";
            ViewBag.Keywords = "Payment Confirmation";
            ViewBag.Amount = id;
            return View();
        }

        public async Task<IActionResult> Success(string orderId, string amt, string carId)
        {
            ViewBag.Title = "Payment Success";
            ViewBag.Description = "Payment Success";
            ViewBag.Keywords = "Payment Success";
            ViewBag.Amount = amt;
            AgapeModelCar.Car car = new AgapeModelCar.Car();
            AgapeModelPayment.PaymentMethod paymentInfo = await GetPaymentOrderInfo(orderId);
            if (paymentInfo != null)
            {
                paymentInfo.Status = "Payment processed successfully";
                var updatePaymentInfo = await UpdatePaymentOrderInfo(paymentInfo);

                //Update the car status to sold in the car table
                UpdateCarStatus(carId, "Sold");
            }
            if (!string.IsNullOrEmpty(carId))
            {
                car = await GetCarDetails(carId);
            }
            return View(car);
        }

        public async Task<IActionResult> Error(string orderId, string amt, string carId)
        {
            ViewBag.Title = "Payment Error";
            ViewBag.Description = "Payment Error";
            ViewBag.Keywords = "Payment Error";
            ViewBag.Amount = amt;
            AgapeModelPayment.PaymentMethod paymentInfo = await GetPaymentOrderInfo(orderId);
            if (paymentInfo != null)
            {
                paymentInfo.Status = "Payment Error";
                var updatePaymentInfo = await UpdatePaymentOrderInfo(paymentInfo);

                //Update the car status to Open in the car table
                UpdateCarStatus(carId, "open");
            }
            return View();
        }

        public async Task<AgapeModelPayment.PaymentMethod> GetPaymentOrderInfo(string orderId)
        {
            var paymentDetails = new AgapeModelPayment.PaymentMethod();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlPayment + orderId;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            paymentDetails = await Response.Content.ReadAsAsync<AgapeModelPayment.PaymentMethod>();
                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from payment Service");
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

        public async Task<bool> UpdatePaymentOrderInfo(AgapeModelPayment.PaymentMethod payment)
        {
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(payment), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlPayment;

                using (var Response = await client.PutAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " " + "Error from payment Service");
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


        public async Task<IActionResult> Summary(string carId)
        {
            ViewBag.Title = "Payment Summary";
            ViewBag.Description = "Payment Summary";
            ViewBag.Keywords = "Payment Summary";
            ViewBag.CarAdvanceAmountPercentage = CarAdvanceAmountPercentage;
            ViewBag.CarTaxPercentage = CarTaxPercentage;
            ViewBag.GstPercentage = GstPercentage;
            ViewBag.AdvanceThresholdAmount = advanceThresholdAmount;
            var car = new AgapeModelCar.Car();
            try
            {
                ViewBag.Countries = await GetCountryLookup();
                if (!string.IsNullOrEmpty(carId))
                    car = await GetCarDetails(carId);
                string defaultImageUrl = string.Empty;
                if (car != null)
                {
                    var lstImages = await GetCarImageByOnwer(carId);
                    if (lstImages != null && lstImages.Any())
                    {
                        var defaultImage = lstImages.Where(i => i.Order == 1);
                        if (defaultImage != null && defaultImage.Any())
                        {
                            defaultImageUrl = defaultImage.FirstOrDefault().Url;
                           
                        }
                       
                    }
                    if (string.IsNullOrEmpty(defaultImageUrl))
                    {
                        defaultImageUrl = defaultCarImageUrl;
                    }
                    car.Thumbnail = defaultImageUrl;
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(car);
        }

        public async Task<IActionResult> Receipt(string carId)
        {
            ViewBag.Title = "Payment Receipt";
            ViewBag.Description = "Payment Receipt";
            ViewBag.Keywords = "Payment Receipt";
            ViewBag.CarAdvanceAmountPercentage = CarAdvanceAmountPercentage;
            ViewBag.CarTaxPercentage = CarTaxPercentage;
            ViewBag.GstPercentage = GstPercentage;
            var car = new AgapeModelCar.Car();
            try
            {
                ViewBag.Countries = await GetCountryLookup();
                if (!string.IsNullOrEmpty(carId))
                    car = await GetCarDetails(carId);
                string defaultImageUrl = string.Empty;
                if (car != null)
                {
                    var lstImages = await GetCarImageByOnwer(carId);
                    if (lstImages != null && lstImages.Any())
                    {
                        var defaultImage = lstImages.Where(i => i.Order == 1);
                        if (defaultImage != null && defaultImage.Any())
                        {
                            defaultImageUrl = defaultImage.FirstOrDefault().MediumUrl;
                            if (string.IsNullOrEmpty(defaultImageUrl))
                                defaultImageUrl = defaultCarImageUrl;
                        }
                    }
                    car.Thumbnail = defaultImageUrl;
                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(car);
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

        public async Task<AgapeModelCar.Car> GetCarDetails(string id)
        {
            var carDetails = new AgapeModelCar.Car();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
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


        //POST - Create New Purchase
        public async Task<bool> CreatePurchaseOrder(string carId)
        {
            try
            {
                var carDetails = await GetCarDetails(carId);
                var purchase = new AgapeModelPurchase.Purchase();

                purchase.Owner = carDetails.Owner;  //Need to check, should be a different id
                purchase.Price = (float)((carDetails.SalePrice * 5) / 100); //5% of the selling price
                purchase.Created = DateTimeOffset.Now;

                AgapeModelCar.Car car = new AgapeModelCar.Car();
                car.Id = carId;
                car.HasImages = carDetails.HasImages;
                car.Make = carDetails.Make;
                car.Mileage = carDetails.Mileage;
                car.Model = carDetails.Model;
                car.SalePrice = carDetails.SalePrice;
              //  car.sellerId = carDetails.Owner;
                car.Status = carDetails.Status;
                car.Vin = carDetails.Vin;
                car.Year = carDetails.Year;
                purchase.Car = car;

                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(purchase), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlPurchase;

                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " " + "Error from Purchase Service");
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

        //public async Task<JsonResult> CreatePaymentWithStripe()
        //{
        //    var paymentDetails = Request.Form["paymentInfo"];
        //    var jsonSerializer = new JsonSerializer();
        //    var reader = new StringReader(paymentDetails);
        //    var payment = (PaymentInfo)jsonSerializer.Deserialize(reader, typeof(PaymentInfo));

        //    Stripe.StripeConfiguration.ApiKey = stripeAPIKey;

        //    Stripe.cr

        //    Stripe.CreditCardOptions card = new Stripe.CreditCardOptions();
        //    card.Name = tParams.CardOwnerFirstName + " " + tParams.CardOwnerLastName;
        //    card.Number = tParams.CardNumber;
        //    card.ExpYear = tParams.ExpirationYear;
        //    card.ExpMonth = tParams.ExpirationMonth;
        //    card.Cvc = tParams.CVV2;
        //    //Assign Card to Token Object and create Token  
        //    Stripe.TokenCreateOptions token = new Stripe.TokenCreateOptions();
        //    token.Card = card;
        //    Stripe.TokenService serviceToken = new Stripe.TokenService();
        //    Stripe.Token newToken = serviceToken.Create(token);
        //}

        //POST - Create New Purchase

        [HttpPost("CreatePurchaseOrderPayments")]
        public async Task<IActionResult> CreatePurchaseOrderPayments()
        {
            try
            {
                decimal totalPrice = 0;
                var carId = Request.Form["carId"];
                var price = Request.Form["totalPrice"];

                if (!string.IsNullOrEmpty(price))
                    totalPrice = Convert.ToDecimal(price) * 100;

                if (!string.IsNullOrEmpty(carId))
                {
                    //Create a purchase Order 
                    bool createPurchase = await CreatePurchaseOrder(carId);
                    var carDetails = await GetCarDetails(carId);

                    if (createPurchase)
                    {
                        //Create a paymentOrder
                        var payment = new AgapeModelPayment.PaymentMethod();
                        payment.Created = DateTimeOffset.Now;
                        payment.Status = "Payment Processing";
                        payment.Owner = Request.Form["owner"];

                        var address = new AgapeModels.Address();
                        address.City = Request.Form["city"];
                        address.State = Request.Form["state"];
                        address.Country = Request.Form["country"];
                        address.Zip = Request.Form["zip"];
                        address.Street = Request.Form["street"];
                        payment.BillingDetails = address;

                        bool paymentOrderResponse = await CreatePurchaseOrderPayment(payment);

                        //Update the car status to PaymentPending in the car table
                        UpdateCarStatus(carId, "PaymentPending");

                        if (paymentOrderResponse)
                        {
                            //Create a payment
                            var options = new SessionCreateOptions
                            {
                                LineItems = new List<SessionLineItemOptions>
                                {
                                  new SessionLineItemOptions
                                  {
                                    PriceData = new SessionLineItemPriceDataOptions
                                    {
                                      UnitAmountDecimal = totalPrice,
                                      Currency = "usd",
                                      ProductData = new SessionLineItemPriceDataProductDataOptions
                                      {
                                        Name = "BADASS Carz",
                                        Description = carDetails.Description
                                      },

                                    },
                                    Quantity = 1,
                                  },
                                },
                                Mode = "payment",
                                SuccessUrl = paymentSuccessRedirectUrl + "?orderId=" + payment.Id + "&amt=" + price + "&carId=" + carId,
                                CancelUrl = paymentErrorRedirectUrl + "?orderId=" + payment.Id + "&amt=" + price + "&carId=" + carId,
                            };

                            var service = new SessionService();
                            Session session = service.Create(options);

                            Response.Headers.Add("Location", session.Url);
                            return new StatusCodeResult(303);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }

            return View("Error");
        }


        public async Task<bool> CreatePurchaseOrderPayment(AgapeModelPayment.PaymentMethod payment)
        {
            try
            {
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(payment), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlPayment;

                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        logHelper.LogError(Response.ReasonPhrase + " " + "Error from payment Service");
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

        public async void UpdateCarStatus(string carId,string status)
        {
            try
            {
                AgapeModelCar.Car car = await GetCarDetails(carId);
                if (car != null)
                {
                    car.Status = status;
                    using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                    StringContent content = new StringContent(JsonConvert.SerializeObject(car), Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrlCar + car.Id;

                    using (var Response = await client.PutAsync(endpoint, content))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                        }
                        else
                        {
                            logHelper.LogError(Response.ReasonPhrase + " Error from Car Service");
                        }
                    }
                }
                else
                {
                    logHelper.LogError("Error while retreive the car, CarId : " + carId);
                }

            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
        }

    }
}
