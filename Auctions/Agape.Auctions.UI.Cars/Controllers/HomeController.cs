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
using AgapeModel = DataAccessLayer.Models;
using AgapeModelAddress = DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;

namespace Agape.Auctions.UI.Cars.Controllers
{

    public class HomeController : Controller
    {
        private readonly IConfiguration configure;
        private readonly ILogger<HomeController> _logger;
        private readonly IServiceManager _serviceManager;
        private readonly string apiBaseUrlUser;
        private LogHelper logHelper;
        public HomeController(ILogger<HomeController> logger, IServiceManager serviceManager, IConfiguration configuration)
        {
            _logger = logger;
            configure = configuration;
            _serviceManager = serviceManager;
            apiBaseUrlUser = configure.GetValue<string>("WebAPIBaseUrlUser");
            logHelper = new LogHelper(configure, _logger);
        }

        public async Task<IActionResult> Privacy()
        {
            return View("Page", await GetContentAsync((int)AgapePageEnum.Privacy));
        }

        public async Task<IActionResult> Terms_Of_Use()
        {
            return View("Page", await GetContentAsync((int)AgapePageEnum.TermsOfUse));
        }

        public async Task<IActionResult> Resources()
        {
            List<Blog> list = new List<Blog>();
            try
            {
                ViewBag.Title = "Resources";
                ViewBag.Description = "Resources";
                ViewBag.Keywords = "Resources";
                List<BlogListItem> collection = await _serviceManager.GetBlog();
                foreach (var item in collection)
                {
                    list.Add(await _serviceManager.GetBlogDetails(item.BlogID));
                }
                PagePart pp = await _serviceManager.GetPagePart((int)AgapePageEnum.ResourceLeftPanel);
                ViewBag.ResourcePagePart = pp.Body;
            }
            catch(Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            
            return View(list);
        }

        public async Task<IActionResult> Dealer()
        {
            ViewBag.Title = "Dealer";
            ViewBag.Description = "Dealer";
            ViewBag.Keywords = "Dealer";
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            return View();
        }
        public async Task<IActionResult> Auctions1()
        {
            return View();
        }
        public async Task<IActionResult> CarView()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(MailRequest mailRequest)
        {
            return View();
        }

        public async Task<IActionResult> Page(int id)
        {
            var page = await _serviceManager.GetPage(id);
            return View(page);
        }
       

        private async Task<Content> GetContentAsync(int PageId)
        {
            Content page = await _serviceManager.GetPage(PageId);
            ViewBag.Title = page.Title;
            ViewBag.Description = page.Description;
            ViewBag.Keywords = page.Keywords;
            return page;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Index()
        {
            ServiceReference.Content model= new ServiceReference.Content();
            try
            {
                //string error = await ValidateAndAddUser();
                //if(!string.IsNullOrEmpty(error))
                //{
                //    ViewBag.Error = error;
                //    logHelper.LogError(error);
                //}
                model = await _serviceManager.GetPage((int)AgapePageEnum.About);
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex.ToString());
            }
            return View(model);
        }

        public async Task<string> ValidateAndAddUser()
        {
            string error = string.Empty;
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    var email = GetClaimValueByType("emails");
                    //Validate the user availability based on the logged in user identity
                    var users = await GetUserByIdentity(userId);

                    if (string.IsNullOrEmpty(users.Item2))
                    {
                        var displayName = string.Empty;
                        if (users.Item1 == null || !users.Item1.Any() || users.Item1.Count <= 0)
                        {
                            //Add the new user into table
                            var emailAddress = GetClaimValueByType("emails");
                            var name = GetClaimValueByType("name");
                            var user = new AgapeModel.User();
                            user.Idp = userId;
                            user.Email = emailAddress;
                            user.FirstName = GetClaimValueByType("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
                            user.LastName = GetClaimValueByType("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");

                            if (string.IsNullOrEmpty(user.FirstName))
                            {
                                if (!string.IsNullOrEmpty(emailAddress))
                                {
                                    user.FirstName = GetNameFromEmail(emailAddress);
                                }
                            }
                            if (string.IsNullOrEmpty(user.LastName))
                            {
                                user.LastName = user.FirstName;
                            }
                            var address = new AgapeModelAddress.Address();
                            address.Country = GetClaimValueByType("country");
                            user.Address = address;
                            bool response = await AddNewUser(user);
                            if(!response)
                            {
                                error = "Error while add the new user, Please contact administrator for more details";
                            }
                        }
                    }
                    else
                    {
                        error = users.Item2;
                    }
                }

            }
            catch (Exception ex)
            {
                error = "Exception while validate and add the user, Please contact administrator for more details";
                logHelper.LogError(ex.ToString());
            }
            return error;
        }

        private string GetNameFromEmail(string emailAddress)
        {
            var name = string.Empty;
            try
            {
                    if (emailAddress != null && emailAddress.Contains("@"))
                    {
                        var emailArray = emailAddress.Split("@");
                        name = emailArray[0];
                    }
            }
            catch(Exception ex)
            {

            }
            return name;
        }
        public async Task<string> ValidateAndAddUser1(string Id,string Email,string Name,string Fn,string Ln)
        {
            string error = string.Empty;
            try
            {
                var userId = Id;

                if (!string.IsNullOrEmpty(userId))
                {
                    //Validate the user availability based on the logged in user identity
                    var users = await GetUserByIdentity(userId);

                    if (string.IsNullOrEmpty(users.Item2))
                    {
                        if (users.Item1 == null || !users.Item1.Any() || users.Item1.Count <= 0)
                        {
                            //Add the new user into table
                            var emailAddress = Email;
                            var name = Name;
                            var user = new AgapeModel.User();
                            user.Idp = userId;
                            user.Email = emailAddress;
                            user.FirstName = Fn;
                            user.LastName = Ln;
                            var address = new AgapeModelAddress.Address();
                            address.Country = "India";
                            user.Address = address;
                            bool response = await AddNewUser(user);
                            if (!response)
                            {
                                error = "Error while add the new user, Please contact administrator for more details";
                            }
                        }
                    }
                    else
                    {
                        error = users.Item2;
                    }
                }

            }
            catch (Exception ex)
            {
                error = "Exception while validate and add the user, Please contact administrator for more details";
                logHelper.LogError(ex.ToString());
            }
            return error;
        }
        //POST - Create New User
        public async Task<bool> AddNewUser(AgapeModel.User user)
        {
            try
            {
                user.Id = Guid.NewGuid().ToString();
                using HttpClient client = new HttpClient(new CustomHttpClientHandler(configure));
                StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                string endpoint = apiBaseUrlUser;

                using (var Response = await client.PostAsync(endpoint, content))
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

        public IActionResult DefaultResult(string make)
        {
            return ViewComponent("Favorites", new { viewName = "DefaultResult", make = make });
        }

        public async Task<(List<AgapeModel.User>, string)> GetUserByIdentity(string id)
        {
            string error = string.Empty;
            var lstUser = new List<AgapeModel.User>();
            try
            {
                using (var client = new HttpClient(new CustomHttpClientHandler(configure)))
                {
                    string endpoint = apiBaseUrlUser + "idp/" + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            lstUser = await Response.Content.ReadAsAsync<List<AgapeModel.User>>();
                        }
                        else
                        {
                            error = "Error from User Service, Please contact administrator for more details";
                            logHelper.LogError(Response.ReasonPhrase + " " + "Error from User Service");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = "Error while calling User Service, Please contact administrator for more details";
                logHelper.LogError(ex.ToString());
            }
            return (lstUser, error);
        }

        public string GetClaimValueByType(string claimType)
        {
            var currentValue = string.Empty;
            ClaimsIdentity claimsIdentity = ((ClaimsIdentity)User.Identity);
            foreach(Claim claim in claimsIdentity.Claims)
            {
                if(claim.Type == claimType)
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

        public string GetCurrentEmailFromAzureClaims()
        {
            return AllClaimsFromAzure()[5];
        }

        public string GetCurrentNameFromAzureClaims()
        {
            return AllClaimsFromAzure()[4];
        }

        public string GetCurrentCountryFromAzureClaims()
        {
            return AllClaimsFromAzure()[2];
        }

    }
}
