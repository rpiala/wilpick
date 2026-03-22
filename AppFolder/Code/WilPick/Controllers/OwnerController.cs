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
        public IActionResult SummaryBetReport()
        {
            var drawDate = _helper.GetDrawDate();
            var fromDate = drawDate;
            var toDate = fromDate.AddHours(13).AddSeconds(-1);

            var report = new SummaryBetReportViewModel
            {                
                FromDate = fromDate,
                ToDate = toDate,
                FirstDrawSelected = 0,
                SecondDrawSelected = 0,
                ThirdDrawSelected = 0
            };

            return View(report);
        }

        [HttpPost]
        [Authorize]
        public IActionResult SummaryBetReport(SummaryBetReportViewModel report)
        {
            if (ModelState.IsValid)
            {          
                if (report.FromDate == report.ToDate)
                {
                    report.ToDate = report.FromDate?.AddHours(24).AddSeconds(-1);
                }
                if (string.IsNullOrEmpty(report.Combination))
                {
                    report.Combination = "%";
                }
                else
                {
                    report.Combination = $"%{_helper.GetBaseCombination(_helper.EscapeSqlString(report.Combination))}%";
                }                

                report.FirstDrawSelected = report.FirstDrawSelected != null ? report.FirstDrawSelected : 0;
                report.SecondDrawSelected = report.SecondDrawSelected != null ? report.SecondDrawSelected : 0;
                report.ThirdDrawSelected = report.ThirdDrawSelected != null ? report.ThirdDrawSelected : 0;

                if (report.FirstDrawSelected + report.SecondDrawSelected + report.ThirdDrawSelected > 1)
                {
                    ModelState.AddModelError("", "Please select only one draw.");
                    return View(report);
                }

                string query = string.Empty;

                if (report.FirstDrawSelected == 1)
                {
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY baseCombination) AS RowNum,baseCombination,SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotalBet " +
                        $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet" +
                        $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND baseCombination LIKE '{report.Combination}' " +
                        $"AND firstDrawSelected = 1 {{|}}GROUP{{:}}BaseCombination";
                }
                else if (report.SecondDrawSelected == 1)
                {
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY baseCombination) AS RowNum,baseCombination,SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotalBet " +
                        $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet" +
                        $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND baseCombination LIKE '{report.Combination}' " +
                        $"AND secondDrawSelected = 1 {{|}}GROUP{{:}}BaseCombination";

                }
                else if (report.ThirdDrawSelected == 1)
                {
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY baseCombination) AS RowNum,baseCombination,SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotalBet " +
                        $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet" +
                        $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND baseCombination LIKE '{report.Combination}' " +
                        $"AND thirdDrawSelected = 1 {{|}}GROUP{{:}}BaseCombination";
                }
                else
                {
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY baseCombination) AS RowNum,baseCombination,SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotalBet " +
                            $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet" +
                            $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND baseCombination LIKE '{report.Combination}'{{|}}GROUP{{:}}BaseCombination";
                }
                var reportDetails = _helper.GetTableDataModel<SummaryBetDetailReportViewModel>(query)?.ToList();
                report.TotalRows = reportDetails?.Count;
                report.TotalBetAmount = reportDetails?.Sum(x => x.TotalBet);
                report.BetDetails = reportDetails;

            }
            return View(report);
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
                betHeader = _helper.GetTableDataModel<WpBetHeaderViewModel>(queryBetHdr)?.FirstOrDefault()!;

                if (betHeader == null)
                {
                    //ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    betHeader = new WpBetHeaderViewModel();
                    betHeader.IsCuttOff = isCuttOff;
                }

                var queryBetDtl = $"COLUMNS{{:}}dtl.*,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),dtl.betDetailId)),LTRIM(CASE WHEN dtl.firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN dtl.secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN dtl.thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay" +
                    $",totalBet = betAmount * (firstDrawSelected + secondDrawSelected + thirdDrawSelected)" +
                    $"{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId{{|}}WHERE{{:}}dtl.drawDate ='{drawDate}'";
                betHeader.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList()!;

                //betHeader.BetDetails = new List<WpBetDetailViewModel>();
                //betHeader.BetDetails.Add(new WpBetDetailViewModel { BetDetailId = 1, Combination = "DEQL", BetAmount = 5 });

                betHeader.IsCuttOff = isCuttOff;
                return View("~/Views/Owner/ClientsTodayBet.cshtml", betHeader);
            }
            return View("~/Views/Owner/ClientsTodayBet.cshtml");
        }
    }
}
