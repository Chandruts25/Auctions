using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly string apiBaseUrlUser;

        public AccountController(IConfiguration configuration)
        {
            apiBaseUrlUser = configuration.GetValue<string>("WebAPIBaseUrlUser");
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LogIn(string ReturnUrl = "")
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string userId, string password, string ReturnUrl = "")
        {
            User user = await ValidateUser(userId, password);
            if (!string.IsNullOrEmpty(user.Id) && !string.IsNullOrEmpty(user.FirstName))
            {
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.FirstName)
                };

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                AuthenticationProperties properties = new AuthenticationProperties()
                {
                    AllowRefresh = true,
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), properties);

                if(!string.IsNullOrEmpty(ReturnUrl))
                    return Redirect(ReturnUrl);

                return RedirectToAction("Profile", "User");
            }

            ViewBag.LoginError = "We can't seem to find your account";
            return View();
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Register(string ReturnUrl = "")
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user, string ReturnUrl)
        {
            if (ModelState.IsValid)
            {
                if (await AddNewUser(user))
                {
                    return await Login(user.Email, user.Password, ReturnUrl);
                };
                ViewBag.RegisterError = "Registration failed. Please try again";
            }
            ViewBag.ReturnUrl = ReturnUrl;
            return View(user);
        }

        public async Task<bool> AddNewUser(User user)
        {
            user.Idp = user.Id;
            user.UserType = "user";
            using (HttpClient client = new HttpClient()) {
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
                        return false;
                    }
                }
            }
        }

        public async Task<User> ValidateUser(string userId, string password)
        {
            User result = new();
            using (HttpClient client = new HttpClient())
            {
                string endpoint = apiBaseUrlUser + "Login?userid=" + userId + "&password=" + password;

                using (HttpResponseMessage response = await client.GetAsync(endpoint))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = await response.Content.ReadAsAsync<User>();
                    }
                }
            }
            return result;
        }

        public async Task<bool> IsUserExist(string emailId)
        {
            bool result = false;
            using (HttpClient client = new HttpClient())
            {
                string endpoint = apiBaseUrlUser + "IsUserExist/" + emailId;

                using (HttpResponseMessage response = await client.GetAsync(endpoint))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }
    }
}
