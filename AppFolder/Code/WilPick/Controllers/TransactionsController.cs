using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Text;
using WilPick.Data;
using WilPick.Models;
using WilPick.ViewModels;
using static System.Reflection.Metadata.BlobBuilder;
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
        public async Task<IActionResult> TodaysBetAsync()
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
                    return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml",betHeader);
                }

                var queryBetHdr = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpBetHeader{{|}}WHERE{{:}}userId = '{wpAppUser?.UserId}' AND drawDate ='{drawDate}'";
                betHeader = _helper.GetTableDataModel<WpBetHeaderViewModel>(queryBetHdr)?.FirstOrDefault();
                
                if (betHeader == null)
                {
                    //ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    betHeader = new WpBetHeaderViewModel();
                    betHeader.IsCuttOff = isCuttOff;
                    return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml",betHeader);
                }

                var queryBetDtl = $"COLUMNS{{:}}*,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),betDetailId)),LTRIM(CASE WHEN firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}betId = '{betHeader.BetId}' AND drawDate ='{drawDate}'";
                betHeader.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList();

                //betHeader.BetDetails = new List<WpBetDetailViewModel>();
                //betHeader.BetDetails.Add(new WpBetDetailViewModel { BetDetailId = 1, Combination = "DEQL", BetAmount = 5 });

                betHeader.IsCuttOff = isCuttOff;
                return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml", betHeader);
            }
            return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> TodaysBet(WpBetHeaderViewModel betHdr)
        {
            var isCuttOff = _helper.IsAlreadyCuttOff();
            betHdr.IsCuttOff = isCuttOff;
            ViewData["IsCuttOff"] = isCuttOff;

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

        public IActionResult DeleteBet(string? betDetailIdEnc)
        {
            var betDetailId = Convert.ToDecimal(_helper.DecryptString(betDetailIdEnc));

            var isCuttOff = _helper.IsAlreadyCuttOff();
            ViewData["Title"] = "Create Bet";
            ViewData["IsCuttOff"] = isCuttOff;
            if (isCuttOff)
            {
                return Forbid("Sorry, betting for the current draw is already closed.");
            }

            var drawDate = _helper.GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");
            WpBetDetailViewModel betDtl = new WpBetDetailViewModel();
            var queryBetDtl = $"COLUMNS{{:}}dtl.*,hdr.userId{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId{{|}}WHERE{{:}}dtl.betDetailId = '{betDetailId}' AND dtl.drawDate ='{drawDate}'";
            betDtl = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl).FirstOrDefault();
            if (betDtl == null)
            {
                betDtl = new WpBetDetailViewModel();
            }

            betDtl.PrevCombination = betDtl.Combination;
            betDtl.PrevBetAmount = betDtl.BetAmount;
            betDtl.PrevFirstDrawSelected = betDtl.FirstDrawSelected;
            betDtl.PrevSecondDrawSelected = betDtl.SecondDrawSelected;
            betDtl.PrevThirdDrawSelected = betDtl.ThirdDrawSelected;
            return View("~/Views/Transactions/TodaysBet/DeleteBet.cshtml", betDtl);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteBet(WpBetDetailViewModel betDtl)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong. try again.");
            }

            var drawDate = _helper.GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");
            var isCuttOff = _helper.IsAlreadyCuttOff();            
            if (isCuttOff)
            {
                return Forbid("Sorry, betting for the current draw is already closed.");
            }

            _helper.DeleteWpBetDetail(betDtl);

            return RedirectToAction("TodaysBet", "Transactions");
        }

        public IActionResult CreateBet(string? betDetailIdEnc)
        {
            var betDetailId = string.IsNullOrEmpty(betDetailIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(betDetailIdEnc));
            var isCuttOff = _helper.IsAlreadyCuttOff();
            ViewData["Title"] = "Create Bet";
            ViewData["IsCuttOff"] = isCuttOff;
            if (isCuttOff)
            {
                return Forbid("Sorry, betting for the current draw is already closed.");
            }

            var drawDate = _helper.GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");
            WpBetDetailViewModel betDtl = new WpBetDetailViewModel();
            var queryBetDtl = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}betDetailId = '{betDetailId}' AND drawDate ='{drawDate}'";
            betDtl = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl).FirstOrDefault(); 
            if (betDtl == null)
            {
                betDtl = new WpBetDetailViewModel
                {
                    BetAmount = _helper.GetBetAmount(),
                    FirstDrawSelected = 1,
                    SecondDrawSelected = 1,
                    ThirdDrawSelected = 1                    
                };                
            }
            
            betDtl.BetAmount = betDtl.BetAmount;
            betDtl.PrevCombination = betDtl.Combination;
            betDtl.PrevBetAmount = betDtl.BetAmount;
            betDtl.PrevFirstDrawSelected = betDtl.FirstDrawSelected;
            betDtl.PrevSecondDrawSelected = betDtl.SecondDrawSelected;
            betDtl.PrevThirdDrawSelected = betDtl.ThirdDrawSelected;
            return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml",betDtl);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBet(WpBetDetailViewModel betDtl)
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

            var isCuttOff = _helper.IsAlreadyCuttOff();            
            if (isCuttOff)
            {
                ModelState.AddModelError("","Sorry, betting for the current draw is already closed.");
                return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml", betDtl);
            }

            WpBetHeaderViewModel betHdr = new WpBetHeaderViewModel();           

            var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpAppUsers{{|}}WHERE{{:}}username = '{_helper.EscapeSqlString(user.UserName)}'";
            var wpAppUser = _helper.GetTableDataModel<WpAppUserViewModel>(query)?.FirstOrDefault();                       

            betHdr.UserId = wpAppUser.UserId;
            betHdr.AspNetUserID = wpAppUser.AspNetUserId;
            betHdr.AgentCode = wpAppUser.AgentCode;
            betHdr.BetReferenceNo = "tset";
            betHdr.BetTicketPrice = wpAppUser.BetTicketPrice;
            betHdr.WinningPrize = wpAppUser.WinningPrize;

            betDtl.FirstDrawSelected = betDtl.FirstDrawSelected != null ? betDtl.FirstDrawSelected : 0;
            betDtl.SecondDrawSelected = betDtl.SecondDrawSelected != null ? betDtl.SecondDrawSelected : 0;
            betDtl.ThirdDrawSelected = betDtl.ThirdDrawSelected != null ? betDtl.ThirdDrawSelected : 0;

            betHdr.BetDetails = new List<WpBetDetailViewModel>();
            betHdr.BetDetails.Add(betDtl);

            var drawDate = _helper.GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            var queryCombiTotal = $"COLUMNS{{:}}SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotal," +
                $"SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotal," +
                $"SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotal{{|}}" +
                $"TABLES{{:}}wpBetDetail{{|}}" +
                $"WHERE{{:}}baseCombination = '{_helper.GetBaseCombination(_helper.EscapeSqlString(betDtl.Combination))}' AND drawDate ='{drawDate}'";
            var combiTotal = _helper.GetTableDataModel<BaseCombiDrawTotalViewModel>(queryCombiTotal)?.FirstOrDefault();

            var betLimit = _helper.GetBetLimit();
            var betErrorLimitFlag = false;

            if (betDtl.FirstDrawSelected == 1 && combiTotal?.FirstTotal + betDtl.BetAmount > betLimit)
            {
                //ModelState.AddModelError("", $"Bet limit in the first draw. Current total: {combiTotal?.FirstTotal}");
                ModelState.AddModelError("", $"Combination is already sold out for FIRST DRAW");
                betErrorLimitFlag = true;                               
            }
            if (betDtl.SecondDrawSelected == 1 && combiTotal?.SecondTotal + betDtl.BetAmount > betLimit)
            {
                //ModelState.AddModelError("", $"Bet limit in the second draw. Current total: {combiTotal?.SecondTotal}");
                ModelState.AddModelError("", $"Combination is already sold out for SECOND DRAW");
                betErrorLimitFlag = true;
            }
            if (betDtl.ThirdDrawSelected == 1 && combiTotal?.ThirdTotal + betDtl.BetAmount > betLimit)
            {
                //ModelState.AddModelError("", $"Bet limit in the third draw. Current total: {combiTotal?.ThirdTotal}");
                ModelState.AddModelError("", $"Combination is already sold out for THIRD DRAW");
                betErrorLimitFlag = true;
            }

            if (betErrorLimitFlag == true)
            {
                return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml", betDtl);
            }

            _helper.CreateWpBetHeader(betHdr);

            
            var queryBetHdr = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpBetHeader{{|}}WHERE{{:}}userId = '{wpAppUser?.UserId}' AND drawDate ='{drawDate}'";
            var betHeader = _helper.GetTableDataModel<WpBetHeaderViewModel>(queryBetHdr)?.FirstOrDefault();

            if (betHeader == null)
            {
                //ModelState.AddModelError("TODO", "Something went wrong. try again.");
                return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml", betHeader);
            }

            var queryBetDtl = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}betId = '{betHeader.BetId}' AND drawDate ='{drawDate}'";
            betHeader.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList();

            isCuttOff = _helper.IsAlreadyCuttOff();
            betHeader.IsCuttOff = isCuttOff;
            //return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml", betHeader);
            return RedirectToAction("TodaysBet", "Transactions");
        }

        [HttpGet]        
        public async Task<IActionResult> VerifyBetAmount(decimal? betAmount, string? combination)
        {
            if (betAmount <= 0)
            {
                return Json("Bet amount should be greater than 0.");
            }

            //var exists = await _helper.AgentCodeExistsAsync(agentCode);
            //if (exists)
            //{
            //    // Remote expects 'true' for valid
            //    return Json(true);
            //}

            return Json(true);
        }
    }
}
