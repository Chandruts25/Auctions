using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace Agape.Auctions.UI.Cars.ViewComponents
{
    public class PagePartViewComponent : ViewComponent
    {
        private readonly ILogger<PagePartViewComponent> _logger;
        private readonly IServiceManager _serviceManager;

        public PagePartViewComponent(ILogger<PagePartViewComponent> logger, IServiceManager serviceManager)
        {
            _logger = logger;
            _serviceManager = serviceManager;
        }
        public async Task<IViewComponentResult> InvokeAsync(PagePartViewEnum view)
        {
            var pagePart = new object();
            switch (view)
            {
                case PagePartViewEnum.WhyChooseUs:
                    pagePart = await _serviceManager.GetPagePart((int)AgapePageEnum.WhyChooseUs);
                    break;
                case PagePartViewEnum.Header:
                    pagePart = await _serviceManager.GetPagePart((int)AgapePageEnum.Header);
                    ViewBag.IsLoggedWithGmail = IsLoggedWithGmail();
                    break;
                default:
                    break;
            }
            return View(view.ToString(), pagePart);
        }

        public bool IsLoggedWithGmail()
        {
            bool isLoggedinWithGmail = false;
            if(User.Identity.IsAuthenticated)
            {
                var claimsDetails = AllClaimsFromAzure();
                if (claimsDetails.Count > 8 && claimsDetails[8] != null && !string.IsNullOrEmpty(claimsDetails[8]))
                {
                    if (claimsDetails[8].ToString().ToUpper().Contains("GMAIL"))
                        isLoggedinWithGmail = true;
                }
            }
            return isLoggedinWithGmail;
        }

        public List<string> AllClaimsFromAzure()
        {
            ClaimsIdentity claimsIdentity = ((ClaimsIdentity)User.Identity);
            return claimsIdentity.Claims.Select(x => x.Value).ToList();
        }

    }
}
