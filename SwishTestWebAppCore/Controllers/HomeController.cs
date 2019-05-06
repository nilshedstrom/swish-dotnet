using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DeviceDetectorNET;
using Microsoft.AspNetCore.Mvc;
using SwishTestWebAppCore.Models;

namespace SwishTestWebAppCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var dd = new DeviceDetector(Request.Headers["User-Agent"].ToString());
            dd.Parse();
            return View(dd);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
