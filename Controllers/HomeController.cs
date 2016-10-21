using Microsoft.AspNetCore.Mvc;
using VerySimple.Models;

namespace VerySimple
{
    public class HomeController : Controller
    {
        public ViewResult Index()
        {
            var viewModel = new IndexViewModel(ControllerContext.HttpContext.Request);

            return View(viewModel);
        }
    }
}