using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.IO;
using System.Drawing.Imaging;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using Agape.Auctions.Functions.Cars.WatchEmail.Models;
using User = Agape.Auctions.Functions.Cars.WatchEmail.Models.User;
using Microsoft.Extensions.DependencyInjection;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;
using SendGrid.Helpers.Mail;


namespace Agape.Auctions.Functions.Cars.WatchEmail
{
    public static class FunctionWatchEmail
    {
        public static readonly string apiBaseUrlAuction = Environment.GetEnvironmentVariable("WebAPIBaseUrlAuction");
        public static readonly string apiBaseUrlUser = Environment.GetEnvironmentVariable("WebAPIBaseUrlUser");
        public static readonly string apiBaseUrlCar = Environment.GetEnvironmentVariable("WebAPIBaseUrlCar");
        public static readonly string sendGridAPIKey = Environment.GetEnvironmentVariable("SendGridAPIKey");
        public static readonly string FromEmailId = Environment.GetEnvironmentVariable("FromEmailId");
        public static readonly string CarBaseUrl = Environment.GetEnvironmentVariable("CarBaseUrl");


        [FunctionName("FunctionWatchEmail")]
        public static void Run([CosmosDBTrigger(databaseName: "Tasks", collectionName: "Bidding", ConnectionStringSetting = "CosmosDbConnectionstring")] IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                for (int i = 0; i < input.Count; i++)
                {
                    log.LogInformation("Documents modified " + input.Count);
                    log.LogInformation("Current document Id " + input[i].Id);
                    var doc = input[i].ToString();
                    Bid bidding = JsonConvert.DeserializeObject<Bid>(doc);
                    if (bidding.Type.Equals("Bid"))
                    {
                        FindUsersToSendMail(bidding,log);
                    }
                }
            }
        }

        public static async Task<bool> FindUsersToSendMail(Bid bid, ILogger log)
        {
            if(bid != null && !(string.IsNullOrEmpty(bid.CreatedBy)))
            {
                var bidUserEmail = await GetUserEmail(bid.CreatedBy, log);

                if (!(string.IsNullOrEmpty(bidUserEmail)))
                {
                    SendEmailToBidUser(bidUserEmail, bid.BiddingAmount, bid.CarId, log);
                }
            }

            var lstUsers = await GetAuctionWatchlistUsers(bid.CarId,log);
            if(lstUsers != null)
            {
                foreach(var user in lstUsers)
                {
                    var emailAddress = await GetUserEmail(user, log);
                    if(!(string.IsNullOrEmpty(emailAddress)))
                    {
                        SendEmailToWatchUsers(emailAddress, bid.BiddingAmount, bid.CarId, log);
                    }
                }
            }
            return true;
        }

        private static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddSendGrid(options => { options.ApiKey = sendGridAPIKey; });

            return services;
        }

        public static async void SendEmailToBidUser(string toEmailAddress, decimal bidAmount, string carId, ILogger log)
        {
            try
            {
                var carDetails = await GetCarDetails(carId, log);

                string htmlContent = string.Empty;
                string carModel = string.Empty;

                if (carDetails != null)
                {
                    carModel = carDetails.Make + " " + carDetails.Model;
                }
                
                decimal batServiceFee = 250;
                decimal advancePercentage = 5;
                decimal minPer = 250;
                decimal maxPer = 5000;

                htmlContent += "<html><body>";
                htmlContent += "<div><h2>Confirm your bid</h2></div>";
                htmlContent += "<div><h3> " + carModel + "</h3></div>";
                htmlContent += "<div style='margin - bottom:10px; '> <div style='float: left; width: 20%'><b>Your Bid:</b></div>";
                htmlContent += "<div style='margin-left:100px;'>USD $ " + bidAmount + "</div></div>";
                htmlContent += "<div style='margin-bottom:10px;'><div style='float: left; width: 20%'><b>BaT Service Fee:</b></div>";
                htmlContent += "<div style='margin-left:100px;'>USD $" + batServiceFee + "</div></div>";
                htmlContent += "<div style='margin-bottom:15px;'>";
                htmlContent += "Bidding will advance immediately to $ " + bidAmount + ". The BaT Service Fee is " + advancePercentage + "% of the bid, with a minimum of $" + minPer + " up to a maximum of $" + maxPer + ".</div>";
                htmlContent += "<div style='margin-bottom:15px;'>";
                htmlContent += "When you bid we pre-authorize your credit card for the service fee (this helps prevent fraud). If you win the auction,";
                htmlContent += "your card will be charged for the service fee and you pay the seller directly for the vehicle. If you don't win,";
                htmlContent += "the pre-authorization will be released.</div>";
                htmlContent += "<div>For more info, read about our auctions or email us with any questions.</div></div>";
                htmlContent += "</body></html>";


                var services = ConfigureServices(new ServiceCollection()).BuildServiceProvider();
                var client = services.GetRequiredService<ISendGridClient>();
                var from = new EmailAddress(FromEmailId);
                var to = new EmailAddress(toEmailAddress);

                var msg = new SendGridMessage
                {
                    From = from,
                    Subject = "bid information for a car"
                };
                msg.AddContent(MimeType.Html, htmlContent);
                msg.AddTo(to);

                msg.MailSettings = new MailSettings
                {
                    SandboxMode = new SandboxMode
                    {
                        Enable = false
                    }
                };
                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
                log.LogInformation("Watch Email sent successfully to the user: " + toEmailAddress + "for Carid: " + carId + "for bid: " + bidAmount);
                // Console.WriteLine($"Sending email with payload: \n{msg.Serialize()}");
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }

        public static async void SendEmailToWatchUsers(string toEmailAddress,decimal bidAmount,string carId, ILogger log)
        {
            try
            {
                var carDetails = await GetCarDetails(carId,log);

                string htmlContent = string.Empty;
                string carModel = string.Empty;

                if(carDetails != null)
                {
                    carModel = carDetails.Make + " " + carDetails.Model;
                }

                htmlContent += "<html><body>";
                htmlContent += "<div><h2>Please find the latest bid for the car which is there in your watchlist</h2></div>";
                htmlContent += "<div><h2>Car: "+ carModel + "</h2></div>";
                htmlContent += "<div><h2>Bid Amount: $"+ bidAmount + "</h2></div>";
                htmlContent += "<div>Please find the details of car <a target='_blank' href=" + CarBaseUrl + carId +">here</a></div>";
                htmlContent += "</body></html>";

                var services = ConfigureServices(new ServiceCollection()).BuildServiceProvider();
                var client = services.GetRequiredService<ISendGridClient>();
                var from = new EmailAddress(FromEmailId);
                var to = new EmailAddress(toEmailAddress);

                var msg = new SendGridMessage
                {
                    From = from,
                    Subject = "bid information for a car"
                };
                msg.AddContent(MimeType.Html, htmlContent);
                msg.AddTo(to);

                msg.MailSettings = new MailSettings
                {
                    SandboxMode = new SandboxMode
                    {
                        Enable = false
                    }
                };
                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
                log.LogInformation("Watch Email sent successfully to the user: " + toEmailAddress + "for Carid: " + carId +"for bid: " + bidAmount);
                // Console.WriteLine($"Sending email with payload: \n{msg.Serialize()}");
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }

        public static async Task<Car> GetCarDetails(string id, ILogger log)
        {
            var carDetails = new Car();
            try
            {
                using (var client = new HttpClient())
                {
                    string endpoint = apiBaseUrlCar + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            carDetails = await Response.Content.ReadAsAsync<Car>();
                        }
                        else
                        {
                            log.LogError(Response.ReasonPhrase + " " + "Error from Car Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
            return carDetails;
        }

        public static async Task<string> GetUserEmail(string id, ILogger log)
        {
            var emailAddress = "";
            try
            {
                using (var client = new HttpClient())
                {
                    string endpoint = apiBaseUrlUser + "idp/" + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var lstUSer = await Response.Content.ReadAsAsync<List<User>>();
                            if (lstUSer != null && lstUSer.Any())
                                emailAddress = lstUSer.FirstOrDefault().Email;
                        }
                        else
                        {
                            log.LogError(Response.ReasonPhrase + " " + "Error from User Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
            return emailAddress;
        }

        public static async Task<List<string>> GetAuctionWatchlistUsers(string carId, ILogger log)
        {
            var lstUsers = new List<string>();
            try
            {
                using (var client = new HttpClient())
                {
                    string endpoint = apiBaseUrlAuction;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var response = await Response.Content.ReadAsAsync<List<Auction>>();
                            if (response != null && response.Any())
                            {
                                var carToWatch = response.Where(i => i.Type == "Watch" && i.CarId == carId);
                                if (carToWatch != null && carToWatch.Any())
                                    lstUsers = carToWatch.Select(i => i.CreatedBy).ToList();
                            }
                        }
                        else
                        {
                            log.LogError(Response.ReasonPhrase + " " + "Error from Auction Service");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
            return lstUsers;
        }
    }
}
