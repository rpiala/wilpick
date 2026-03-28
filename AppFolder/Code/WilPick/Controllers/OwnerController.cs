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
        public IActionResult DrawResultsHeader()
        {
            
            var fromDate = DateTime.Today.AddDays(-7);
            var toDate = DateTime.Today.AddDays(1);

            DrawResultHeaderViewModel hdr = new DrawResultHeaderViewModel();
            hdr.FromDate = fromDate;
            hdr.ToDate = toDate;

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY drawDate) AS RowNum,ResultIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),resultId)),*" +
                $"{{|}}TABLES{{:}}wpDrawResults{{|}}WHERE{{:}}drawDate >= '{hdr.FromDate:yyyy-MM-dd HH:mm:ss}' AND drawDate <= '{hdr.ToDate:yyyy-MM-dd HH:mm:ss}'{{|}}SORT{{:}}drawDate desc";
            hdr.Results = _helper.GetTableDataModel<DrawResultDetailViewModel>(query)?.ToList()!;            

            return View(hdr);
        }

        [Authorize]
        public IActionResult CreateUpdateDrawResult(string? ResultIdEnc)
        {
            var ResultId = string.IsNullOrEmpty(ResultIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(ResultIdEnc));

            var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpDrawResults{{|}}WHERE{{:}}ResultId = {ResultId}";
            var result = _helper.GetTableDataModel<DrawResultDetailViewModel>(query)?.FirstOrDefault() ?? new DrawResultDetailViewModel();
            if (result.DrawDate == null)
            {
                result.DrawDate = _helper.GetDrawDate().AddDays(-1);
            }

            return View(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateUpdateDrawResult(DrawResultDetailViewModel result)
        {
            if (!ModelState.IsValid)
                return View();

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            _helper.CreateUpdateDrawResult(result, wpAppUser);

            return RedirectToAction("DrawResultsHeader", "Owner");
        }

        [Authorize]
        public async Task<IActionResult> OwnerPlayerLoadTransactions()
        {
            var fromDate = DateTime.Today.AddDays(-7);
            var toDate = DateTime.Today.AddDays(1);

            PlayerLoadTransactionsViewModel report = new PlayerLoadTransactionsViewModel();
            report.FromDate = fromDate;
            report.ToDate = toDate;
            report.SelectedApproveStatus = 0;

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY load.loadId) AS RowNum,loadIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),loadId)),load.*" +
                $",RequestedByUsername = usr.userName{{|}}TABLES{{:}}wpUserLoadTrans load INNER JOIN wpAppUsers usr ON usr.userId = load.userId" +
                $"{{|}}WHERE{{:}}load.requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"load.requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND load.isApproved = {report.SelectedApproveStatus}{{|}}SORT{{:}}requestedDate";
            var loadDetails = _helper.GetTableDataModel<PlayerLoadDetailViewModel>(query)?.ToList()!;
            report.LoadDetails = loadDetails;

            return View(report);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> OwnerPlayerLoadTransactions(PlayerLoadTransactionsViewModel report)
        {
            if (!ModelState.IsValid)
                return View();

            if (report.FromDate == report.ToDate)
            {
                report.ToDate = report.FromDate?.AddHours(24).AddSeconds(-1);
            }

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                return View(report);
            }

            if (report.SelectedApproveStatus == null)
            {
                report.SelectedApproveStatus = -1;
            }

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY load.loadId) AS RowNum,loadIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),loadId)),load.*" +
                $",RequestedByUsername = usr.userName{{|}}TABLES{{:}}wpUserLoadTrans load INNER JOIN wpAppUsers usr ON usr.userId = load.userId" +
                $"{{|}}WHERE{{:}}load.requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"load.requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND ({report.SelectedApproveStatus} = -1 OR load.isApproved = {report.SelectedApproveStatus}){{|}}SORT{{:}}requestedDate";
            var loadDetails = _helper.GetTableDataModel<PlayerLoadDetailViewModel>(query)?.ToList()!;
            report.LoadDetails = loadDetails;

            return View(report);
        }

        [Authorize]        
        public IActionResult ViewApproveLoadTransaction(string? loadIdEnc)
        {
            var loadId = string.IsNullOrEmpty(loadIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(loadIdEnc));
            PlayerLoadDetailViewModel loadDetail = new PlayerLoadDetailViewModel();

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

            if (loadId > 0)
            {
                var query = $"COLUMNS{{:}}load.*,PlayerName = usr.userName{{|}}TABLES{{:}}wpUserLoadTrans load INNER JOIN wpAppUsers usr ON usr.userId = load.userId{{|}}WHERE{{:}}load.loadId = {loadId}";
                loadDetail = _helper.GetTableDataModel<PlayerLoadDetailViewModel>(query)?.FirstOrDefault()!;
            }            
            loadDetail.ApprovedAmount = loadDetail.RequestedAmount;

            return View(loadDetail);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ApproveLoadTransaction(PlayerLoadDetailViewModel loadDetail)
        {            
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Error Approval");
                return View("~/Views/Owner/ViewApproveLoadTransaction.cshtml", loadDetail);
            }

            if (loadDetail.ApprovedAmount <= 0)
            {
                ModelState.AddModelError("", "Approve amount should be greather than zero");
                return View("~/Views/Owner/ViewApproveLoadTransaction.cshtml", loadDetail);
            }

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                return View(loadDetail);
            }

            loadDetail.ApprovedBy = wpAppUser.FirstName;
            loadDetail.IsApproved = 1;


            _helper.ApproveLoadTransaction(loadDetail);


            return RedirectToAction("OwnerPlayerLoadTransactions", "Owner");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DisapproveLoadTransaction(PlayerLoadDetailViewModel loadDetail)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "");
                return View();
            }

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                return View(loadDetail);
            }

            loadDetail.ApprovedBy = wpAppUser.FirstName;
            loadDetail.IsApproved = 2;


            _helper.ApproveLoadTransaction(loadDetail);

            return RedirectToAction("OwnerPlayerLoadTransactions", "Owner");
        }

        [Authorize]
        public IActionResult ClientList()
        {
            ClientListViewModel clients = new ClientListViewModel();

            var query = $"COLUMNS{{:}}usr.*,UserIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),usr.userId)), RemainingLoad = dbo.GetPlayerRemainingLoad(usr.userId)" +
                $",userType = CASE WHEN EXISTS (SELECT 1 FROM wpOwner WHERE userName = usr.userName) THEN 'Admin' WHEN EXISTS (SELECT 1 FROM wpAgents WHERE userName = usr.userName) THEN 'Agent' ELSE 'Player' END" +
                $",AgentName = CASE WHEN EXISTS (SELECT 1 FROM wpAgents WHERE agentCode = usr.agentCode)  THEN (SELECT usrA.firstName FROM wpAgents wa INNER JOIN wpAppUsers usrA ON usrA.userName = wa.userName  WHERE wa.agentCode = usr.agentCode) ELSE '' END" +
                $"{{|}}TABLES{{:}}wpAppUsers usr{{|}}SORT{{:}}userType, usr.firstName";
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
            var query = $"COLUMNS{{:}}usr.*,UserIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),usr.userId)), RemainingLoad = dbo.GetPlayerRemainingLoad(usr.userId)" +
                $",userType = CASE WHEN EXISTS (SELECT 1 FROM wpOwner WHERE userName = usr.userName) THEN 'Admin' WHEN EXISTS (SELECT 1 FROM wpAgents WHERE userName = usr.userName) THEN 'Agent' ELSE 'Player' END" +
                $"{{|}}TABLES{{:}}wpAppUsers usr{{|}}WHERE{{:}}userId = '{userId}'";
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
        public IActionResult ViewClientBet(string? betDetailIdEnc)
        {
            var betDetailId = string.IsNullOrEmpty(betDetailIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(betDetailIdEnc));            
            var drawDate = _helper.GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            WpBetDetailViewModel betDtl = new WpBetDetailViewModel();
            var queryBetDtl = $"COLUMNS{{:}}dtl.*,totalBet = dtl.betAmount * (dtl.firstDrawSelected + dtl.secondDrawSelected + dtl.thirdDrawSelected), PlayerName = usr.userName" +
                $"{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId " +
                $"INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId{{|}}WHERE{{:}}dtl.betDetailId = '{betDetailId}'";
            betDtl = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.FirstOrDefault()!;
            
            betDtl.BetAmount = betDtl.BetAmount;
            betDtl.PrevCombination = betDtl.Combination;
            betDtl.PrevBetAmount = betDtl.BetAmount;
            betDtl.PrevFirstDrawSelected = betDtl.FirstDrawSelected;
            betDtl.PrevSecondDrawSelected = betDtl.SecondDrawSelected;
            betDtl.PrevThirdDrawSelected = betDtl.ThirdDrawSelected;            
            return View("~/Views/Owner/ViewClientBet.cshtml", betDtl);
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

                var queryBetDtl = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY usr.firstName,dtl.dateCreated) AS RowNum,dtl.*,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),dtl.betDetailId)),LTRIM(CASE WHEN dtl.firstDrawSelected = 1 THEN '1,' ELSE '' END + " +
                    $"CASE WHEN dtl.secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN dtl.thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay" +
                    $",totalBet = betAmount * (firstDrawSelected + secondDrawSelected + thirdDrawSelected), PlayerName = usr.firstName" +
                    $"{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId " +
                    $"INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId{{|}}WHERE{{:}}dtl.drawDate ='{drawDate}'{{|}}SORT{{:}}usr.userName asc, dtl.dateCreated";
                betHeader.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList()!;
                betHeader.TotalBetAmount = betHeader.BetDetails.Sum(x => x.TotalBet) ?? 0;

                //betHeader.BetDetails = new List<WpBetDetailViewModel>();
                //betHeader.BetDetails.Add(new WpBetDetailViewModel { BetDetailId = 1, Combination = "DEQL", BetAmount = 5 });

                betHeader.IsCuttOff = isCuttOff;
                return View("~/Views/Owner/ClientsTodayBet.cshtml", betHeader);
            }
            return View("~/Views/Owner/ClientsTodayBet.cshtml");
        }
    }
}
