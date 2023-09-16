using AgapeAPI.Core;
using Microsoft.AspNetCore.Mvc;
using Agape.Auctions.UI.Cars.Models;
using System.Threading.Tasks;

namespace Agape.Auctions.UI.Cars.Controllers
{
    public class NewsController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public NewsController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            ViewBag.Title = "News Details";
            ViewBag.Description = "News Details";
            ViewBag.Keywords = "News Details";
            ServiceReference.News newsDetails = await _serviceManager.GetArticle(id);
            return View(newsDetails);
        }
    }
}
