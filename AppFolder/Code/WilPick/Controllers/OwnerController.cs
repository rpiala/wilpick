using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Security.Claims;
using WilPick.Data;
using WilPick.Models;
using WilPick.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Constants = WilPick.Common.Constant;
using Roles = WilPick.Common.Roles;

namespace WilPick.Controllers
{
    public class OwnerController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly DataHelper _helper;

        public OwnerController(SignInManager<Users> signInManager, UserManager<Users> userManager, DataHelper helper)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            _helper = helper;
        }

        [Authorize]
        public IActionResult ClientList()
        {
            ClientListViewModel clients = new ClientListViewModel();

            var query = $"COLUMNS{{:}}*,UserIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),userId)){{|}}TABLES{{:}}wpAppUsers";
            clients.Clients = _helper.GetTableDataModel<WpAppUserViewModel>(query)?.ToList()!;
            return View(clients);
        }


        [Authorize]
        public IActionResult EditClient(string? userIdEnc)
        {
            var userId = string.IsNullOrEmpty(userIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(userIdEnc));
            var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpAppUsers{{|}}WHERE{{:}}userId = '{userId}'";
            var client = _helper.GetTableDataModel<WpAppUserViewModel>(query)?.FirstOrDefault();

            return View(client);
        }

        [HttpPost]
        [Authorize]
        public IActionResult EditClient(WpAppUserViewModel client)
        {
            _helper.UpdateWpClient(client);
            return RedirectToAction("ClientList","Owner");
        }

        [Authorize]
        public async Task<IActionResult> ClientsTodayBet()
        {
            if (ModelState.IsValid)
            {
                var drawDate = _helper.GetDrawDate();
                var isCuttOff = _helper.IsAlreadyCuttOff();

                ViewData["DrawDate"] = drawDate;
                ViewData["IsCuttOff"] = isCuttOff;

                var user = await userManager.GetUserAsync(User);
                WpBetHeaderViewModel betHeader = new WpBetHeaderViewModel();
                betHeader.BetDetails = new List<WpBetDetailViewModel>();

                if (user == null)
                {
                    ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml");
                }

                var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpAppUsers{{|}}WHERE{{:}}username = '{_helper.EscapeSqlString(user?.UserName)}'";
                var wpAppUser = _helper.GetTableDataModel<WpAppUserViewModel>(query)?.FirstOrDefault();

                if (wpAppUser == null)
                {
                    ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    return View("~/Views/Angent/MembersTodayBet.cshtml", betHeader);
                }

                var queryBetHdr = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpBetHeader{{|}}WHERE{{:}}userId = '{wpAppUser?.UserId}' AND drawDate ='{drawDate}'";
                betHeader = _helper.GetTableDataModel<WpBetHeaderViewModel>(queryBetHdr)?.FirstOrDefault();

                if (betHeader == null)
                {
                    //ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    betHeader = new WpBetHeaderViewModel();
                    betHeader.IsCuttOff = isCuttOff;
                }

                var queryBetDtl = $"COLUMNS{{:}}dtl.*,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),dtl.betDetailId)),LTRIM(CASE WHEN dtl.firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN dtl.secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN dtl.thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay" +
                    $"{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId{{|}}WHERE{{:}}dtl.drawDate ='{drawDate}'";
                betHeader.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList();

                //betHeader.BetDetails = new List<WpBetDetailViewModel>();
                //betHeader.BetDetails.Add(new WpBetDetailViewModel { BetDetailId = 1, Combination = "DEQL", BetAmount = 5 });

                betHeader.IsCuttOff = isCuttOff;
                return View("~/Views/Owner/ClientsTodayBet.cshtml", betHeader);
            }
            return View("~/Views/Owner/ClientsTodayBet.cshtml");
        }
    }
}
