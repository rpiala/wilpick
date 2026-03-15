using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Text;
using WilPick.Data;
using WilPick.Models;
using WilPick.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Constants = WilPick.Common.Constant;

namespace WilPick.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly DataHelper _helper;

        public TransactionsController(SignInManager<Users> signInManager, UserManager<Users> userManager, DataHelper helper)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            _helper = helper;
        }
        public IActionResult TodaysBet()
        {
            return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> TodaysBet(WpBetHeaderViewModel betHdr)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);

                if (user != null)
                {
                    return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml");
                }
            }
            return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml");
        }

        public IActionResult CreateBet(decimal? betDetailId)
        {                        
            WpBetDetailViewModel betDtl = new WpBetDetailViewModel();
            betDtl.Combination = "DEQL";
            return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml",betDtl);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBet(WpBetDetailViewModel beDtl)
        {            
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong. try again.");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                ModelState.AddModelError("", "Something went wrong. try again.");
            }

            var sb = new StringBuilder();

            sb.Append("COLUMNS{:}*{|}TABLES{:}wpAppUsers{|}WHERE{:}username = '");
            sb.Append($"{user.UserName}'");


            var wpAppUser = _helper.GetTableDataModel<WpAppUserViewModel>(sb.ToString())?.FirstOrDefault();

            WpBetHeaderViewModel betHdr = new WpBetHeaderViewModel();

            betHdr.UserId = wpAppUser.UserId;
            betHdr.AspNetUserID = wpAppUser.AspNetUserId;
            betHdr.AgentCode = wpAppUser.AgentCode;
            betHdr.BetReferenceNo = "tset";
            betHdr.BetTicketPrice = wpAppUser.BetTicketPrice;
            betHdr.WinningPrize = wpAppUser.WinningPrize;
           
            _helper.CreateWpBetHeader(betHdr);

            return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml");
        }
    }
}
