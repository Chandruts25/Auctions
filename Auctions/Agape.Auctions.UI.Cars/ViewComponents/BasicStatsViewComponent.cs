using Agape.Auctions.UI.Cars.Models;
using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.ViewComponents
{
    public class BasicStatsViewComponent : ViewComponent
    {
        private readonly ILogger<BasicStatsViewComponent> _logger;
        private readonly IServiceManager _serviceManager;

        public BasicStatsViewComponent(ILogger<BasicStatsViewComponent> logger, IServiceManager serviceManager)
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
