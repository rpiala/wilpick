using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Diagnostics;
using WilPick.Data;
using WilPick.Models;

namespace WilPick.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataHelper _helper;

        public HomeController(ILogger<HomeController> logger, DataHelper helper)
        {
            _logger = logger;
            _helper = helper;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var permutationsFour = await _helper.GetTableDataAsync(
                        "COLUMNS{:}dbo.GetPermutationsCSV2008('ABCD')");
            var combis = string.Join(",",permutationsFour.Rows.Cast<DataRow>().Select(r => r[0]?.ToString() ?? string.Empty));
            return View();
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
