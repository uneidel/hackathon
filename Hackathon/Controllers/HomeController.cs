using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Hackathon.Controllers
{
    public class HomeController : Controller
    {
        public HomeController() { }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "hackathon";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "hackathon";

            return View();
        }
    }
}