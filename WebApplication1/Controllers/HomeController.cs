using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        [Authorize]
        public IActionResult Index()
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (role == "SuperAdmin")
            {
                return RedirectToAction("SuperAdminHome");
            }
            else if (role == "Admin")
            {
                return RedirectToAction("AdminHome");
            }
            else if (role == "SalesManager")
            {
                return RedirectToAction("SalesManagerHome");
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        [Authorize(Policy = "AdminPolicy")]
        public IActionResult AdminHome()
        {
            ViewData["Title"] = "Admin (or SuperAdmin) Dashboard";
            return View();
        }

        [Authorize(Policy = "SalesManagerPolicy")]
        public IActionResult SalesManagerHome()
        {
            ViewData["Title"] = "Sales Manager Dashboard";
            return View();
        }

        [Authorize(Policy = "SuperAdminPolicy")]
        public IActionResult SuperAdminHome()
        {
            ViewData["Title"] = "Super Admin Dashboard";
            return View();
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
