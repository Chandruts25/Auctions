using Agape.Auctions.UI.Cars.Admin.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;


namespace Agape.Auctions.UI.Cars.Admin.ViewComponents
{
    public class FormPartViewComponent : ViewComponent
    {
        private readonly ILogger<FormPartViewComponent> _logger;
        private readonly IServiceManager _serviceManager;

        public FormPartViewComponent(ILogger<FormPartViewComponent> logger, IServiceManager serviceManager)
        {
            _logger = logger;
            _serviceManager = serviceManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(FormPartViewEnum view)
        {
            var model = new object();
            switch (view)
            {
                case FormPartViewEnum.Contact:
                    model = await _serviceManager.GetPagePart((int)AgapePageEnum.ContactUs); 
                    break;
                default:
                    break;
            }
            return View(view.ToString(), model);
        }
    }
}
