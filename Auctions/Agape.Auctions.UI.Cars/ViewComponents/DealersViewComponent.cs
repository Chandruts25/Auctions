using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace Agape.Auctions.UI.Cars.ViewComponents
{
    public class DealersViewComponent : ViewComponent
    {
        private readonly IConfiguration _Configure;
        private readonly string apiBaseUrl;
        public DealersViewComponent(IConfiguration configuration)
        {
            _Configure = configuration;
            apiBaseUrl = _Configure.GetValue<string>("WebAPIBaseUrlDealer");
        }
        public async Task<IViewComponentResult> InvokeAsync(DealersViewEnum view, string id)
        {
            var model = new object();
            switch (view)
            {
                case DealersViewEnum.AddEditDealer:
                    if(!string.IsNullOrEmpty(id))
                    {
                        model = await GetDealerDetails(id);
                    }
                    else
                    {
                        model = new Dealer();
                    }
                    break;
                default:
                    break;
            }
            return View(view.ToString(), model);
        }
        public async Task<Dealer> GetDealerDetails(string id)
        {
            var dealer = new Dealer();
            try
            {
                using (var client = new HttpClient())
                {
                    string endpoint = apiBaseUrl + id;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            dealer = await Response.Content.ReadAsAsync<Dealer>();
                        }
                        else
                        {
                            return dealer;
                        }
                    }
                }
                return dealer;
            }
            catch (Exception ex)
            {
                return dealer;
            }
        }
    }
}
