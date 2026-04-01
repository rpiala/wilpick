using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Diagnostics;
using WilPick.Common;
using WilPick.Data;
using WilPick.Models;

namespace WilPick.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataHelper _helper;
        private readonly UserManager<Users> userManager;

        public HomeController(ILogger<HomeController> logger, DataHelper helper, UserManager<Users> userManager )
        {
            _logger = logger;
            _helper = helper;
            this.userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> IndexAsync()
        {
            var permutationsFour = await _helper.GetTableDataAsync(
                        "COLUMNS{:}dbo.GetBaseCombination('ABCD')");
            var combis = string.Join(",",permutationsFour.Rows.Cast<DataRow>().Select(r => r[0]?.ToString() ?? string.Empty));

            var user = await userManager.GetUserAsync(User);            
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid Session.");
                //return View();
            }

            var wpUser = _helper.GetWpUserByUserName(user?.Email!);
            if (wpUser == null)
            {
                ModelState.AddModelError("", "Invalid Session.");
                //return View();
            }

            if (User.FindFirst("Role") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            if (wpUser.AccessRole == Roles.Client)
            {
                return RedirectToAction("TodaysBet", "Transactions");
            }

            return View(wpAppUser);
        }

        [Authorize]
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
