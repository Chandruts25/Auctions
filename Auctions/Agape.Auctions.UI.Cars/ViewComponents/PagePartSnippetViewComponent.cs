using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.ViewComponents
{
    public class PagePartSnippetViewComponent : ViewComponent
    {
        private readonly ILogger<PagePartSnippetViewComponent> _logger;
        private readonly IServiceManager _serviceManager;

        public PagePartSnippetViewComponent(ILogger<PagePartSnippetViewComponent> logger, IServiceManager serviceManager)
        {
            _logger = logger;
            _serviceManager = serviceManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(PagePartViewEnum view)
        {
            var list = new object();
            switch (view)
            {
                case  PagePartViewEnum.Footer:
                    list = await _serviceManager.GetCollection((int)AgapeCollectionEnum.BottomMenu);
                    var pagePartFooterContact = await _serviceManager.GetPagePart((int)AgapePageEnum.ContactUsFooter);
                    List<ServiceReference.NewsListItem> newsList = await _serviceManager.GetNews();
                    ViewBag.FooterContact = pagePartFooterContact.Body;
                    ViewBag.NewsList = newsList;
                    break;

                case PagePartViewEnum.Navigation:
                    list = await _serviceManager.GetCollection((int)AgapeCollectionEnum.Navigation); 
                    break;
                default:
                    break;
            }
            return View(view.ToString(), list);
        }
    }
}
