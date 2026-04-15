using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Text;
using WilPick.Data;
using WilPick.Models;
using WilPick.ViewModels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
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

        [Authorize]
        public IActionResult PlayerDrawResultsHeader()
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
        [HttpPost]
        public IActionResult PlayerDrawResultsHeader(DrawResultHeaderViewModel report)
        {
            if (report.FromDate > report.ToDate)
            {
                ModelState.AddModelError("", "From date should be greater than to date.");
                return View(report);
            }
            if (report.FromDate == report.ToDate)
            {
                report.ToDate = report.FromDate?.AddHours(24).AddSeconds(-1);
            }
            else
            {
                report.ToDate = report.ToDate?.AddHours(24).AddSeconds(-1);
            }

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY drawDate) AS RowNum,ResultIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),resultId)),*" +
                $"{{|}}TABLES{{:}}wpDrawResults{{|}}WHERE{{:}}drawDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND drawDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}'{{|}}SORT{{:}}drawDate desc";
            report.Results = _helper.GetTableDataModel<DrawResultDetailViewModel>(query)?.ToList()!;

            return View(report);
        }

        [Authorize]
        public async Task<IActionResult> CashOutTransactions()
        {
            var fromDate = DateTime.Today.AddDays(-7);
            var toDate = DateTime.Today.AddDays(1);

            CashOutHeaderViewModel report = new CashOutHeaderViewModel();
            report.FromDate = fromDate;
            report.ToDate = toDate;
            report.SelectedApproveStatus = 0;

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY cashOutId) AS RowNum,cashOutIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),cashOutId)),*{{|}}TABLES{{:}}wpCashOutTransactions" +
                $"{{|}}WHERE{{:}}userId ={wpAppUser.UserId} AND requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND isDeleted=0 AND isCompleted = {report.SelectedApproveStatus}{{|}}SORT{{:}}requestedDate";
            var cashOutDetails = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.ToList()!;
            report.details = cashOutDetails;

            return View(report);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CashOutTransactions(CashOutHeaderViewModel report)
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

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY cashOutId) AS RowNum,cashOutIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),cashOutId)),*{{|}}TABLES{{:}}wpCashOutTransactions" +
                $"{{|}}WHERE{{:}}userId ={wpAppUser.UserId} AND requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND ({report.SelectedApproveStatus} = -1 OR isCompleted = {report.SelectedApproveStatus}) AND isDeleted=0{{|}}SORT{{:}}requestedDate";
            var cashOutDetails = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.ToList()!;
            report.details = cashOutDetails;

            return View(report);
        }

        [Authorize]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> CashOutDetail(string? cashOutIdEnc)
        {
            if (!ModelState.IsValid)
                return View();

            var cashOutId = string.IsNullOrEmpty(cashOutIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(cashOutIdEnc));
            CashOutDetailViewModel detail = new CashOutDetailViewModel();

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);
            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                return View(detail);
            }

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

            if (cashOutId > 0)
            {
                var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpCashOutTransactions{{|}}WHERE{{:}}cashOutId = {cashOutId}";
                detail = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.FirstOrDefault()!;
            }
            else
            {
                detail.LoadBalance = _helper.GetRemainingLoad(wpAppUser?.UserId!);
                detail.ReceiverMobileNumber = wpAppUser?.MobileNumber;
                detail.ReceiverName = wpAppUser?.FirstName;
            }

            return View(detail);
        }

        [Authorize]
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> CashOutDetail(CashOutDetailViewModel details)
        {
            if (!ModelState.IsValid)
                return View(details);

            if (string.IsNullOrEmpty(details.ReceiverMobileNumber))
            {
                ModelState.AddModelError("ReceiverMobileNumber", "Please enter receiver mobile number.");
                return View(details);
            }

            if (details.CashOutAmount < 1)
            {
                ModelState.AddModelError("", "Please enter cash out amount.");
                return View(details);
            }

            if (details?.Attachment != null && details.Attachment.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(details?.Attachment!.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await details.Attachment.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(details.AttachmentFilename))
                {
                    var oldFilePath = Path.Combine(uploadsPath, details.AttachmentFilename);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                }
                details.AttachmentFilename = fileName;
            }

            // Save fileName to database
            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                return View(details);
            }

            details.UserId = wpAppUser.UserId;
            var remainingLoad = _helper.GetRemainingLoad(details.UserId);
            details.LoadBalance = remainingLoad;

            if (details.CashOutAmount > remainingLoad)
            {
                ModelState.AddModelError("CashOutAmount", $"Insufficient load. Your remaining load is {remainingLoad}");
                return View(details);
            }

            _helper.CreateUpdateCashOutTransaction(details);

            return RedirectToAction("CashOutTransactions", "Transactions");
        }

        [Authorize]
        public async Task<IActionResult> DeleteCashOut(string? cashOutIdEnc)
        {
            var cashOutId = string.IsNullOrEmpty(cashOutIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(cashOutIdEnc));
            CashOutDetailViewModel detail = new CashOutDetailViewModel();

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);
            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                return View(detail);
            }

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

            if (cashOutId > 0)
            {
                var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpCashOutTransactions{{|}}WHERE{{:}}cashOutId = {cashOutId} AND isDeleted=0";
                detail = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.FirstOrDefault()!;
            }
            detail.LoadBalance = _helper.GetRemainingLoad(wpAppUser?.UserId!);

            return View(detail);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteCashOut(CashOutDetailViewModel cashOut)
        {
            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);
            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                return View(cashOut);
            }

            if (cashOut.CashOutId < 0)
            {
                ModelState.AddModelError("", "Cash out details expired");
                return View(cashOut);
            }

            var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpCashOutTransactions{{|}}WHERE{{:}}cashOutId = {cashOut.CashOutId} AND isDeleted=0";
            var detail = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.FirstOrDefault()!;

            if (detail == null)
            {
                ModelState.AddModelError("", "Cash out details expired");
                return View(cashOut);
            }

            if (detail.IsCompleted == 1)
            {
                ModelState.AddModelError("", "Completed cash out transaction cannot be deleted.");
                return View(cashOut);
            }

            _helper.DeleteCashOut(detail);

            return RedirectToAction("CashOutTransactions", "Transactions");
        }


        [Authorize]
        public async Task<IActionResult> PlayerLoadTransactions()
        {
            var fromDate = DateTime.Today.AddDays(-7);
            var toDate = DateTime.Today.AddDays(1);

            PlayerLoadTransactionsViewModel report = new PlayerLoadTransactionsViewModel();
            report.FromDate = fromDate;
            report.ToDate = toDate;
            report.SelectedApproveStatus = 0;

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY loadId) AS RowNum,loadIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),loadId)),*{{|}}TABLES{{:}}wpUserLoadTrans" +
                $"{{|}}WHERE{{:}}userId ={wpAppUser.UserId} AND requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}'{{|}}SORT{{:}}requestedDate";
            var loadDetails = _helper.GetTableDataModel<PlayerLoadDetailViewModel>(query)?.ToList()!;
            report.LoadDetails = loadDetails;

            return View(report);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlayerLoadTransactions(PlayerLoadTransactionsViewModel report)
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

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY loadId) AS RowNum,loadIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),loadId)),*{{|}}TABLES{{:}}wpUserLoadTrans" +
                $"{{|}}WHERE{{:}}userId ={wpAppUser.UserId} AND requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND ({report.SelectedApproveStatus} = -1 OR isApproved = {report.SelectedApproveStatus}){{|}}SORT{{:}}requestedDate";
            var loadDetails = _helper.GetTableDataModel<PlayerLoadDetailViewModel>(query)?.ToList()!;
            report.LoadDetails = loadDetails;

            return View(report);
        }

        [Authorize]
        //[ValidateAntiForgeryToken]
        public IActionResult CreateLoadTransaction(string? loadIdEnc)
        {
            var loadId = string.IsNullOrEmpty(loadIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(loadIdEnc));
            PlayerLoadDetailViewModel loadDetail = new PlayerLoadDetailViewModel();
            loadDetail.ReceiverMobileNumbers = new List<SelectListItem>();

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

            if (loadId > 0)
            {
                var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpUserLoadTrans{{|}}WHERE{{:}}loadId = {loadId}";
                loadDetail = _helper.GetTableDataModel<PlayerLoadDetailViewModel>(query)?.FirstOrDefault()!;
            }
            loadDetail.ReferenceNo = $"Please select receiver";

            var receiversQuery = $"COLUMNS{{:}}mobileNumber{{|}}TABLES{{:}}wpOwner{{|}}WHERE{{:}}mobileNumber IS NOT NULL";
            var receivers = _helper.GetTableDataModel<GcashReceivers>(receiversQuery)?.ToList()!;

            if (receivers != null)
            {
                loadDetail.ReceiverMobileNumbers = receivers.Select(x => new SelectListItem
                {
                    Value = x.MobileNumber,
                    Text = x.MobileNumber
                }).ToList();
            }

            return View(loadDetail);
        }


        [Authorize]
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLoadTransaction(PlayerLoadDetailViewModel loadDetail)
        {
            if (!ModelState.IsValid)
                return View(loadDetail);

            var errFlag = false;
            var receiversQuery = $"COLUMNS{{:}}mobileNumber{{|}}TABLES{{:}}wpOwner";
            var receivers = _helper.GetTableDataModel<GcashReceivers>(receiversQuery)?.ToList()!;

            if (receivers != null)
            {
                loadDetail.ReceiverMobileNumbers = receivers.Select(x => new SelectListItem
                {
                    Value = x.MobileNumber,
                    Text = x.MobileNumber
                }).ToList();
            }

            if (loadDetail.RequestedAmount < 1)
            {
                ModelState.AddModelError("", "Please enter cash in amount.");
                errFlag = true;
            }

            if (string.IsNullOrEmpty(loadDetail.ReceiverMobileNumber))
            {
                ModelState.AddModelError("", "Please select receiver number.");
                errFlag = true;
            }

            if ((loadDetail?.Attachment == null || loadDetail?.Attachment!.Length < 1) && loadDetail?.LoadId < 1)
            {
                ModelState.AddModelError("", "Please upload a file.");
                errFlag = true;
            }

            // Save fileName to database
            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            if (wpAppUser == null)
            {
                ModelState.AddModelError("", "Player info expired");
                errFlag = true;
            }

            if (errFlag)
            {
                return View(loadDetail);
            }

            if (loadDetail?.Attachment != null && loadDetail.Attachment.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"CI_{Guid.NewGuid()}" + Path.GetExtension(loadDetail?.Attachment!.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await loadDetail.Attachment.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(loadDetail.AttachmentFilename))
                {
                    var oldFilePath = Path.Combine(uploadsPath, loadDetail.AttachmentFilename);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                }
                loadDetail.AttachmentFilename = fileName;
            }

            loadDetail.UserId = wpAppUser.UserId;

            _helper.CreateUpdateLoadTransaction(loadDetail);

            return RedirectToAction("PlayerLoadTransactions", "Transactions");
        }

        [Authorize]
        public async Task<IActionResult> TodaysBetAsync()
        {
            if (ModelState.IsValid)
            {
                if (!signInManager.IsSignedIn(User))
                    return RedirectToAction("IndexAsync", "Home");

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
                    return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml", betHeader);
                }

                var remainingLoad = _helper.GetRemainingLoad(wpAppUser?.UserId);

                var queryBetHdr = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpBetHeader{{|}}WHERE{{:}}userId = '{wpAppUser?.UserId}' AND drawDate ='{drawDate}'";
                betHeader = _helper.GetTableDataModel<WpBetHeaderViewModel>(queryBetHdr)?.FirstOrDefault()!;

                var cashFlowQuery = $"COLUMNS{{:}}TotalCashIn, TotalBet AS OverallTotalBet, TotalCashOut{{|}}TABLES{{:}}dbo.GetPlayerCashFlowByUserId({wpAppUser?.UserId})";
                var cashFlow = _helper.GetTableDataModel<WpCashFlowViewModel>(cashFlowQuery)?.FirstOrDefault();

                if (betHeader == null)
                {
                    //ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    betHeader = new WpBetHeaderViewModel();
                    betHeader.IsCuttOff = isCuttOff;
                    betHeader.PlayerRemainingload = remainingLoad;
                    betHeader.BetType = wpAppUser?.betType;
                    betHeader.TotalCashIn = cashFlow?.TotalCashIn;
                    betHeader.OverallTotalBet = cashFlow?.OverallTotalBet;
                    betHeader.TotalCashOut = cashFlow?.TotalCashOut;
                    return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml", betHeader);
                }
                betHeader.PlayerRemainingload = remainingLoad;
                betHeader.BetType = wpAppUser?.betType;

                var queryBetDtl = $"COLUMNS{{:}}*,ROW_NUMBER() OVER (ORDER BY betDetailId) AS RowNum,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),betDetailId)),LTRIM(CASE WHEN firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay" +
                    $",totalBet = (betAmount + rambleBetAmount) * (firstDrawSelected + secondDrawSelected + thirdDrawSelected){{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}betId = '{betHeader.BetId}' AND drawDate ='{drawDate}'";
                betHeader.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList()!;
                betHeader.TotalBetAmount = betHeader.BetDetails?.Sum(x => x.TotalBet);

                betHeader.IsCuttOff = isCuttOff;
                betHeader.TotalCashIn = cashFlow?.TotalCashIn;
                betHeader.OverallTotalBet = cashFlow?.OverallTotalBet;
                betHeader.TotalCashOut = cashFlow?.TotalCashOut;
                return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml", betHeader);
            }
            return View("~/Views/Transactions/TodaysBet/TodaysBet.cshtml");
        }

        [Authorize]
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

        [Authorize]
        public IActionResult PlayerBetHistory()
        {
            var drawDate = _helper.GetDrawDate();
            var fromDate = drawDate;
            var toDate = fromDate.AddHours(13).AddSeconds(-1);

            var report = new PlayerHistoryBetHeaderViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalBetAmount = 0
            };

            return View(report);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlayerBetHistory(PlayerHistoryBetHeaderViewModel history)
        {

            if (ModelState.IsValid)
            {
                if (history.FromDate == history.ToDate)
                {
                    history.ToDate = history.FromDate?.AddHours(24).AddSeconds(-1);
                }
                else
                {
                    history.ToDate = history.ToDate?.AddHours(24).AddSeconds(-1);
                }

                var user = await userManager.GetUserAsync(User);
                if (user == null)
                {
                    return View(history);
                }

                var wpUser = _helper.GetWpUserByUserName(user?.Email!);
                if (wpUser == null)
                {
                    return View(history);
                }

                var queryBetDtl = $"COLUMNS{{:}}*,ROW_NUMBER() OVER (ORDER BY dtl.betDetailId) AS RowNum,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),dtl.betDetailId))" +
                    $",LTRIM(CASE WHEN dtl.firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN dtl.secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN dtl.thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay" +
                    $",totalBet = (betAmount + rambleBetAmount) * (firstDrawSelected + secondDrawSelected + thirdDrawSelected)" +
                    $"{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId{{|}}WHERE{{:}}hdr.userId = {wpUser.UserId} AND dtl.drawDate >= '{history.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND dtl.drawDate < '{history.ToDate?.ToString("yyyy-MM-dd HH:mm")}'{{|}}SORT{{:}}RowNum";

                history.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList();
                history.TotalBetAmount = history.BetDetails?.Sum(x => x.TotalBet);

            }
            return View(history);
        }

        [Authorize]
        public IActionResult DeleteBet(string? betDetailIdEnc)
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


        [Authorize]
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

        [Authorize]
        public async Task<IActionResult> CreateBet(string? betDetailIdEnc)
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
            betDtl = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.FirstOrDefault()!;

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid Session.");
                return View();
            }

            var wpUser = _helper.GetWpUserByUserName(user?.Email!);

            var remainingLoad = _helper.GetRemainingLoad(wpUser?.UserId);

            if (betDtl == null)
            {
                betDtl = new WpBetDetailViewModel
                {
                    //BetAmount = wpUser.BetTicketPrice,
                    //RambleBetAmount = wpUser.BetTicketPrice,
                    FirstDrawSelected = 1,
                    SecondDrawSelected = 1,
                    ThirdDrawSelected = 1,
                    IncludeRamble = 1,
                    LoadBalance = remainingLoad,
                    WinningPrize = wpUser.WinningPrize,
                    RambleWinningPrize = wpUser.RambleWinningPrize
                };
            }
            else
            {
                betDtl.LoadBalance = remainingLoad;
                betDtl.PrevBetAmount = betDtl.BetAmount;
                betDtl.PrevCombination = betDtl.Combination;
                betDtl.PrevBetAmount = betDtl.BetAmount;
                betDtl.PrevFirstDrawSelected = betDtl.FirstDrawSelected;
                betDtl.PrevSecondDrawSelected = betDtl.SecondDrawSelected;
                betDtl.PrevThirdDrawSelected = betDtl.ThirdDrawSelected;
                betDtl.WinningPrize = wpUser?.WinningPrize;
                betDtl.RambleWinningPrize = wpUser?.RambleWinningPrize;
            }

            if (wpUser?.betType == Constants.LOADTYPE && remainingLoad <= 0)
            {
                ModelState.AddModelError("", $"Insufficient load. Your remaining load is {remainingLoad}");
                return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml", betDtl);
            }
            return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml", betDtl);
        }

        [Authorize]
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
                ModelState.AddModelError("", "Sorry, betting for the current draw is already closed.");
                return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml", betDtl);
            }

            var drawDate = _helper.GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            WpBetHeaderViewModel betHdr = new WpBetHeaderViewModel();

            var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpAppUsers{{|}}WHERE{{:}}username = '{_helper.EscapeSqlString(user?.UserName)}'";
            var wpAppUser = _helper.GetTableDataModel<WpAppUserViewModel>(query)?.FirstOrDefault()!;

            betHdr.UserId = wpAppUser.UserId;
            betHdr.AspNetUserID = wpAppUser.AspNetUserId;
            betHdr.AgentCode = wpAppUser.AgentCode;
            betHdr.BetReferenceNo = _helper.GetDrawDate().ToString($"yyyymmdd-{wpAppUser.UserId}");
            betHdr.BetTicketPrice = wpAppUser.BetTicketPrice;
            betHdr.WinningPrize = wpAppUser.WinningPrize;
            betHdr.RambleWinningPrize = wpAppUser.RambleWinningPrize;

            betDtl.FirstDrawSelected = betDtl.FirstDrawSelected != null ? betDtl.FirstDrawSelected : 0;
            betDtl.SecondDrawSelected = betDtl.SecondDrawSelected != null ? betDtl.SecondDrawSelected : 0;
            betDtl.ThirdDrawSelected = betDtl.ThirdDrawSelected != null ? betDtl.ThirdDrawSelected : 0;
            betDtl.betType = wpAppUser.betType;
            betDtl.IncludeRamble = betDtl.IncludeRamble != null ? betDtl.IncludeRamble : 0;
            betDtl.WinningPrize = wpAppUser.WinningPrize;
            betDtl.RambleWinningPrize = wpAppUser.RambleWinningPrize;

            betDtl.BetAmount = betDtl.BetAmount == null ? 0 : betDtl.BetAmount;
            betDtl.RambleBetAmount = betDtl.RambleBetAmount == null ? 0 : betDtl.RambleBetAmount;

            betHdr.BetDetails = new List<WpBetDetailViewModel>();
            betHdr.BetDetails.Add(betDtl);

            //var queryCombiTotal = $"COLUMNS{{:}}SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotal," +
            //    $"SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotal," +
            //    $"SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotal{{|}}" +
            //    $"TABLES{{:}}wpBetDetail{{|}}" +
            //    $"WHERE{{:}}baseCombination = '{_helper.GetBaseCombination(_helper.EscapeSqlString(betDtl.Combination))}' AND drawDate ='{drawDate}'";

            var RambleQueryCombiTotal = $"COLUMNS{{:}}SUM(CASE WHEN FirstDrawSelected = 1 THEN rambleBetAmount ELSE 0 END)  AS FirstTotal," +
                $"SUM(CASE WHEN SecondDrawSelected = 1 THEN rambleBetAmount ELSE 0 END) AS SecondTotal," +
                $"SUM(CASE WHEN ThirdDrawSelected = 1 THEN rambleBetAmount ELSE 0 END)  AS ThirdTotal{{|}}" +
                $"TABLES{{:}}wpBetDetail{{|}}" +
                $"WHERE{{:}}baseCombination = '{_helper.GetBaseCombination(_helper.EscapeSqlString(betDtl.Combination))}' " +
                $"AND drawDate ='{drawDate}' AND betDetailId <> {betDtl.BetDetailId}";
            var RambleCombiTotal = _helper.GetTableDataModel<BaseCombiDrawTotalViewModel>(RambleQueryCombiTotal)?.FirstOrDefault();

            var queryCombiTotal = $"COLUMNS{{:}}SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotal," +
                $"SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotal," +
                $"SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotal{{|}}" +
                $"TABLES{{:}}wpBetDetail{{|}}" +
                $"WHERE{{:}}Combination = '{_helper.EscapeSqlString(betDtl.Combination)}' AND drawDate ='{drawDate}' AND betDetailId <> {betDtl.BetDetailId}";
            var combiTotal = _helper.GetTableDataModel<BaseCombiDrawTotalViewModel>(queryCombiTotal)?.FirstOrDefault();

            var betLimit = _helper.GetBetLimit();
            var betErrorLimitFlag = false;

            if (betDtl.FirstDrawSelected == 1 && (combiTotal?.FirstTotal >= betLimit || (combiTotal?.FirstTotal + betDtl.BetAmount) > betLimit ||
                (RambleCombiTotal?.FirstTotal / betLimit) + (betDtl.RambleBetAmount / betLimit) > betLimit))
            {
                if (combiTotal?.FirstTotal >= betLimit)
                    ModelState.AddModelError("", $"FIRST DRAW available STRAIGHT:0 AND RAMBLE:0");
                else
                    ModelState.AddModelError("", $"FIRST DRAW available STRAIGHT:{betLimit - combiTotal?.FirstTotal} OR  RAMBLE:{((betLimit * betLimit) - (RambleCombiTotal?.FirstTotal))}");

                betErrorLimitFlag = true;
            }
            if (betDtl.SecondDrawSelected == 1 && (combiTotal?.SecondTotal >= betLimit || (combiTotal?.SecondTotal + betDtl.BetAmount) > betLimit ||
                (RambleCombiTotal?.SecondTotal / betLimit) + (betDtl.RambleBetAmount / betLimit) > betLimit))
            {
                if (combiTotal?.SecondTotal >= betLimit)
                    ModelState.AddModelError("", $"SECOND DRAW available STRAIGHT:0 AND RAMBLE:0");
                else
                    ModelState.AddModelError("", $"SECOND DRAW available STRAIGHT:{betLimit - combiTotal?.FirstTotal} OR  RAMBLE:{((betLimit * betLimit) - (RambleCombiTotal?.FirstTotal))}");

                betErrorLimitFlag = true;
            }
            if (betDtl.ThirdDrawSelected == 1 && (combiTotal?.ThirdTotal >= betLimit || (combiTotal?.ThirdTotal + betDtl.BetAmount) > betLimit ||
                (RambleCombiTotal?.ThirdTotal / betLimit) + (betDtl.RambleBetAmount / betLimit) > betLimit))
            {
                if (combiTotal?.ThirdTotal >= betLimit)
                    ModelState.AddModelError("", $"THIRD DRAW available STRAIGHT:0 AND RAMBLE:0");
                else
                    ModelState.AddModelError("", $"THIRD DRAW available STRAIGHT:{betLimit - combiTotal?.FirstTotal} OR  RAMBLE:{((betLimit * betLimit) - (RambleCombiTotal?.FirstTotal))}");

                betErrorLimitFlag = true;
            }

            if (betDtl.FirstDrawSelected == 0 && betDtl.SecondDrawSelected == 0 && betDtl.ThirdDrawSelected == 0)
            {
                ModelState.AddModelError("", $"No draw has been selected. Please select and save.");
                betErrorLimitFlag = true;
            }

            if (betDtl.BetAmount < 1 && betDtl.RambleBetAmount < 1)
            {
                ModelState.AddModelError("", $"Please enter Bet Amount.");
                betErrorLimitFlag = true;
            }

            var remainingLoad = _helper.GetRemainingLoad(wpAppUser?.UserId);
            if (betErrorLimitFlag == true)
            {
                betDtl.LoadBalance = remainingLoad;
                return View("~/Views/Transactions/TodaysBet/CreateBet.cshtml", betDtl);
            }

            if (wpAppUser?.betType == Constants.LOADTYPE && remainingLoad < betDtl.ComputedAmount)
            {
                betDtl.LoadBalance = remainingLoad;
                ModelState.AddModelError("", $"Insufficient load. Your remaining load is {remainingLoad}");
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

        [Authorize]
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

        [Authorize]
        public async Task<IActionResult> SwTodaysBetAsync()
        {
            if (ModelState.IsValid)
            {
                if (!signInManager.IsSignedIn(User))
                    return RedirectToAction("IndexAsync", "Home");

                var drawDate = _helper.GetSwDrawDate();
                var isCuttOff = _helper.IsSwAlreadyCuttOff();

                ViewData["DrawDate"] = drawDate;
                ViewData["IsCuttOff"] = isCuttOff;

                var user = await userManager.GetUserAsync(User);
                SwCoBetHdrViewModel betHeader = new SwCoBetHdrViewModel();
                betHeader.BetDetails = new List<SwCoBetDtlViewModel>();

                if (user == null)
                {
                    ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    return View("~/Views/Transactions/SwTodaysBet.cshtml");
                }

                var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

                if (wpAppUser == null)
                {
                    ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    return View("~/Views/Transactions/SwTodaysBet.cshtml", betHeader);
                }

                var remainingLoad = _helper.GetRemainingLoad(wpAppUser?.UserId);

                var queryBetHdr = $"COLUMNS{{:}}*{{|}}TABLES{{:}}co_bet_hdr{{|}}WHERE{{:}}cw_id = {wpAppUser?.SwCw_id} AND co_id = {wpAppUser?.SwCo_id} AND wp_id = {wpAppUser?.SwWp_id} AND draw_sked ='{drawDate.ToString("yyyy-MM-dd HH:mm")}'";
                betHeader = _helper.GetTableDataModel<SwCoBetHdrViewModel>(queryBetHdr)?.FirstOrDefault()!;

                var cashFlowQuery = $"COLUMNS{{:}}TotalCashIn, TotalBet AS OverallTotalBet, TotalCashOut{{|}}TABLES{{:}}dbo.GetPlayerCashFlowByUserId({wpAppUser?.UserId})";
                var cashFlow = _helper.GetTableDataModel<WpCashFlowViewModel>(cashFlowQuery)?.FirstOrDefault();

                if (betHeader == null)
                {
                    //ModelState.AddModelError("TODO", "Something went wrong. try again.");
                    betHeader = new SwCoBetHdrViewModel();
                    betHeader.IsCuttOff = isCuttOff;
                    betHeader.PlayerRemainingload = remainingLoad;
                    betHeader.BetType = wpAppUser?.betType;
                    betHeader.TotalCashIn = cashFlow?.TotalCashIn;
                    betHeader.OverallTotalBet = cashFlow?.OverallTotalBet;
                    betHeader.TotalCashOut = cashFlow?.TotalCashOut;
                    return View("~/Views/Transactions/SwTodaysBet.cshtml", betHeader);
                }
                betHeader.PlayerRemainingload = remainingLoad;
                betHeader.BetType = wpAppUser?.betType;

                var queryBetDtl = $"COLUMNS{{:}}*,ROW_NUMBER() OVER (ORDER BY cbd_dtl_no) AS RowNum,cbd_dtl_no_enc = dbo.EncryptString(CONVERT(VARCHAR(20),cbd_dtl_no))" +
                    $"{{|}}TABLES{{:}}co_bet_dtl{{|}}WHERE{{:}}cbh_no = '{betHeader.cbh_no}' AND draw_sked ='{drawDate.ToString("yyyy-MM-dd HH:mm")}'";
                betHeader.BetDetails = _helper.GetTableDataModel<SwCoBetDtlViewModel>(queryBetDtl)?.ToList()!;
                //betHeader.TotalBetAmount = betHeader.BetDetails?.Sum(x => x.TotalBet);

                betHeader.IsCuttOff = isCuttOff;
                betHeader.TotalCashIn = cashFlow?.TotalCashIn;
                betHeader.OverallTotalBet = cashFlow?.OverallTotalBet;
                betHeader.TotalCashOut = cashFlow?.TotalCashOut;
                return View("~/Views/Transactions/SwTodaysBet.cshtml", betHeader);
            }
            return View("~/Views/Transactions/SwTodaysBet.cshtml");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SwTodaysBet(SwCoBetHdrViewModel betHdr)
        {
            var isCuttOff = _helper.IsAlreadyCuttOff();
            betHdr.IsCuttOff = isCuttOff;
            ViewData["IsCuttOff"] = isCuttOff;

            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);

                if (user != null)
                {
                    return View("~/Views/Transactions/SwTodaysBet.cshtml");
                }
            }
            return View("~/Views/Transactions/SwTodaysBet.cshtml");
        }

        [Authorize]
        public async Task<IActionResult> SwCreateBet(string? cbdDtlNoEnc)
        {
            var cbdDtlNo = string.IsNullOrEmpty(cbdDtlNoEnc) ? string.Empty : _helper.DecryptString(cbdDtlNoEnc);           
            var isCuttOff = _helper.IsSwAlreadyCuttOff();

            ViewData["Title"] = "Create S3 Bet";
            ViewData["IsCuttOff"] = isCuttOff;
            if (isCuttOff)
            {
                return Forbid("Sorry, betting for the current draw is already closed.");
            }

            var drawDate = _helper.GetSwDrawDate().ToString("yyyy-MM-dd HH:mm");

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid Session.");
                return View();
            }

            var wpUser = _helper.GetWpUserByUserName(user?.Email!);

            SwCoBetDtlViewModel betDtl = new SwCoBetDtlViewModel();
            var queryBetDtl = $"COLUMNS{{:}}*{{|}}TABLES{{:}}co_bet_dtl{{|}}WHERE{{:}}cbd_dtl_no = '{cbdDtlNo}' AND draw_sked ='{drawDate}'";
            betDtl = _helper.GetTableDataModel<SwCoBetDtlViewModel>(queryBetDtl)?.FirstOrDefault()!;

            var remainingLoad = _helper.GetRemainingLoad(wpUser?.UserId);

            var queryBetHdr = $"COLUMNS{{:}}*{{|}}TABLES{{:}}co_bet_hdr{{|}}WHERE{{:}}cw_id = {wpUser?.SwCw_id} AND co_id = {wpUser?.SwCo_id} AND wp_id = {wpUser?.SwWp_id} AND draw_sked ='{drawDate}'";
            var betHeader = _helper.GetTableDataModel<SwCoBetHdrViewModel>(queryBetHdr)?.FirstOrDefault()!;

            if (betDtl == null)
            {
                betDtl = new SwCoBetDtlViewModel
                {
                    cvm_no = betHeader?.cvm_no,
                    cbh_no = betHeader?.cbh_no!,
                    cbd_dtl_no = "",
                    SwEntries = new List<SwCoSwEntryViewModel>().ToList(),
                    LoadBalance = remainingLoad,
                    WinningPrize = wpUser?.WinningPrize,
                };
            }
            else
            {
                betDtl.LoadBalance = remainingLoad;
                betDtl.prev_cbd_msg = betDtl.cbd_msg;
                betDtl.prev_cbd_bet = betDtl.cbd_bet;
            }

            if (wpUser?.betType == Constants.LOADTYPE && remainingLoad <= 0)
            {
                ModelState.AddModelError("", $"Insufficient load. Your remaining load is {remainingLoad}");
                return View("~/Views/Transactions/SwCreateBet.cshtml", betDtl);
            }
            return View("~/Views/Transactions/SwCreateBet.cshtml", betDtl);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SwCreateBet(SwCoBetDtlViewModel betDtl)
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
            var isCuttOff = _helper.IsSwAlreadyCuttOff();
            if (isCuttOff)
            {
                ModelState.AddModelError("", "Sorry, betting for the current draw is already closed.");
                return View("~/Views/Transactions/SwCreateBet.cshtml", betDtl);
            }

            betDtl.target = betDtl.target != null ? betDtl.target : 0;
            betDtl.ramble = betDtl.ramble != null ? betDtl.ramble : 0;

            var wpUser = _helper.GetWpUserByUserName(user?.Email!);

            _helper.CreateSwBet(betDtl, wpUser);

            return View(betDtl);
        }
    }
}
