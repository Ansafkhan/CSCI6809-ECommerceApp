using Microsoft.AspNetCore.Mvc;

namespace FrontendUI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}