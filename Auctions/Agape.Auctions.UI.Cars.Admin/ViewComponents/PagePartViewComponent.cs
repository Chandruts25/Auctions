using Agape.Auctions.UI.Cars.Admin.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.Admin.ViewComponents
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
                    break;
                default:
                    break;
            }
            return View(view.ToString(), pagePart);
        }

       
    }
}
