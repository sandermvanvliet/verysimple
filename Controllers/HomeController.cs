using Microsoft.AspNetCore.Mvc;

namespace VerySimple
{
    public class HomeController : Controller
    {
        public ViewResult Index()
        {
            return View();
        }
    }
}