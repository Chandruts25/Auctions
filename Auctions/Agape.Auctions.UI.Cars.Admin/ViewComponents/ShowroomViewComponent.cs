using Agape.Auctions.UI.Cars.Admin.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.Admin.ViewComponents
{
    public class ShowroomViewComponent : ViewComponent
    {
        private readonly ILogger<ShowroomViewComponent> _logger;
        private readonly IServiceManager _serviceManager;

        public ShowroomViewComponent(ILogger<ShowroomViewComponent> logger, IServiceManager serviceManager)
        {
            _logger = logger;
            _serviceManager = serviceManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View();
        }
    }
}
