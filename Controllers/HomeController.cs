using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerySimple.Models;

namespace VerySimple
{
    public class HomeController : Controller
    {
        public ViewResult Index()
        {
            var viewModel = new IndexViewModel(ControllerContext.HttpContext.Request, ControllerContext.HttpContext.Session);

            return View(viewModel);
        }

        public RedirectToActionResult SetSessionValue(string sessionValue)
        {
            ControllerContext.HttpContext.Session.SetString("sessionValue", sessionValue);

            return RedirectToAction("Index");
        }
    }
}