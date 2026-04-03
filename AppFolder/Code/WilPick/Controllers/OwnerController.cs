using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public IActionResult DrawHolidayHeader()
        {

            var fromDate = DateTime.Today.AddDays(-14);
            var toDate = DateTime.Today.AddDays(14);

            DrawHolidayHeaderViewModel hdr = new DrawHolidayHeaderViewModel();
            hdr.FromDate = fromDate;
            hdr.ToDate = toDate;

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY holidayDate) AS RowNum,HolidayIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),HolidayId)),*" +
                $"{{|}}TABLES{{:}}wpDrawHoliday{{|}}WHERE{{:}}holidayDate >= '{hdr.FromDate:yyyy-MM-dd HH:mm:ss}' AND holidayDate <= '{hdr.ToDate:yyyy-MM-dd HH:mm:ss}' AND isDeleted=0{{|}}SORT{{:}}holidayDate desc";
            hdr.Results = _helper.GetTableDataModel<DrawHolidayDetailViewModel>(query)?.ToList()!;

            return View(hdr);
        }

        [Authorize]
        [HttpPost]
        public IActionResult DrawHolidayHeader(DrawHolidayHeaderViewModel report)
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

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY holidayDate) AS RowNum,HolidayIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),HolidayId)),*" +
               $"{{|}}TABLES{{:}}wpDrawHoliday{{|}}WHERE{{:}}holidayDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND holidayDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND isDeleted=0{{|}}SORT{{:}}holidayDate desc";
            report.Results = _helper.GetTableDataModel<DrawHolidayDetailViewModel>(query)?.ToList()!;

            return View(report);
        }

        [Authorize]
        public IActionResult CreateUpdateDrawHoliday(string? HolidayIdEnc)
        {
            var HolidayId = string.IsNullOrEmpty(HolidayIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(HolidayIdEnc));

            DrawHolidayDetailViewModel holiday = new DrawHolidayDetailViewModel();

            if (HolidayId == 0) 
            {
                holiday.HolidayDate = DateTime.Now;
            }
            else
            {
                var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpDrawHoliday{{|}}WHERE{{:}}HolidayId = {HolidayId}";
                holiday = _helper.GetTableDataModel<DrawHolidayDetailViewModel>(query)?.FirstOrDefault() ?? new DrawHolidayDetailViewModel();
            }


            return View(holiday);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateUpdateDrawHoliday(DrawHolidayDetailViewModel holiday)
        {
            if (!ModelState.IsValid)
                return View();

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            _helper.CreateUpdateDrawHoliday(holiday, wpAppUser);

            return RedirectToAction("DrawHolidayHeader", "Owner");
        }

        [Authorize]
        public IActionResult DeleteDrawHoliday(string? HolidayIdEnc)
        {
            var HolidayId = string.IsNullOrEmpty(HolidayIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(HolidayIdEnc));

            var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpDrawHoliday{{|}}WHERE{{:}}HolidayId = {HolidayId}";
            var result = _helper.GetTableDataModel<DrawHolidayDetailViewModel>(query)?.FirstOrDefault() ?? new DrawHolidayDetailViewModel();

            return View(result);
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteDrawHoliday(DrawHolidayDetailViewModel holiday)
        {
            _helper.DeleteDrawHoliday(holiday);
            return RedirectToAction("DrawHolidayHeader", "Owner");
        }

        [Authorize]
        public async Task<IActionResult> OwnerCashOutTransactions()
        {
            var fromDate = DateTime.Today.AddDays(-30);
            var toDate = DateTime.Today.AddDays(1);

            CashOutHeaderViewModel report = new CashOutHeaderViewModel();
            report.FromDate = fromDate;
            report.ToDate = toDate;
            report.SelectedApproveStatus = 0;

            var user = await userManager.GetUserAsync(User);
            var wpAppUser = _helper.GetWpUserByUserName(user?.Email!);

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY cashOutId) AS RowNum,cashOutIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),cashOutId)),cash.*" +
                $",PlayerName = usr.firstName{{|}}TABLES{{:}}wpCashOutTransactions cash INNER JOIN wpAppUsers usr ON usr.userId = cash.userId" +
                $"{{|}}WHERE{{:}}requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND isDeleted=0 AND isCompleted=0{{|}}SORT{{:}}requestedDate";
            var cashOutDetails = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.ToList()!;
            report.details = cashOutDetails;

            return View(report);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> OwnerCashOutTransactions(CashOutHeaderViewModel report)
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

            var query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY cashOutId) AS RowNum,cashOutIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),cashOutId)),cash.*" +
                $",PlayerName = usr.firstName{{|}}TABLES{{:}}wpCashOutTransactions cash INNER JOIN wpAppUsers usr ON usr.userId = cash.userId" +
                $"{{|}}WHERE{{:}}requestedDate >= '{report.FromDate:yyyy-MM-dd HH:mm:ss}' AND " +
                $"requestedDate <= '{report.ToDate:yyyy-MM-dd HH:mm:ss}' AND isDeleted=0 AND ({report.SelectedApproveStatus} = -1 OR isCompleted = {report.SelectedApproveStatus}){{|}}SORT{{:}}requestedDate";
            var cashOutDetails = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.ToList()!;
            report.details = cashOutDetails;

            return View(report);
        }

        [Authorize]
        //[ValidateAntiForgeryToken]
        public IActionResult OwnerCashOutDetail(string? cashOutIdEnc)
        {
            var cashOutId = string.IsNullOrEmpty(cashOutIdEnc) ? 0 : Convert.ToDecimal(_helper.DecryptString(cashOutIdEnc));
            CashOutDetailViewModel detail = new CashOutDetailViewModel();

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

            if (cashOutId > 0)
            {
                var query = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpCashOutTransactions{{|}}WHERE{{:}}cashOutId = {cashOutId} AND isDeleted=0";
                detail = _helper.GetTableDataModel<CashOutDetailViewModel>(query)?.FirstOrDefault()!;
            }

            return View(detail);
        }

        [Authorize]
        [HttpPost]        
        public async Task<IActionResult> OwnerCashOutDetail(CashOutDetailViewModel details)
        {
            if (!ModelState.IsValid)
                return View(details);

            if (details?.Attachment != null && details.Attachment.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"CO_{Guid.NewGuid()}" + Path.GetExtension(details?.Attachment!.FileName);
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

            _helper.CreateUpdateCashOutTransaction(details);

            return RedirectToAction("OwnerCashOutTransactions", "Owner");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CompleteOwnerCashOutDetail(CashOutDetailViewModel cashOut)
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
                ModelState.AddModelError("", "Cash out has already been completed.");
                return View(cashOut);
            }

            if (cashOut?.Attachment != null && cashOut.Attachment.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOADPATH);

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"CO_{Guid.NewGuid()}" + Path.GetExtension(cashOut?.Attachment!.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await cashOut.Attachment.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(cashOut.AttachmentFilename))
                {
                    var oldFilePath = Path.Combine(uploadsPath, cashOut.AttachmentFilename);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                }
                cashOut.AttachmentFilename = fileName;
            }

            detail.ProcessedBy = wpAppUser.FirstName;
            detail.AttachmentFilename = cashOut?.AttachmentFilename;

            _helper.CompleteCashOut(detail);

            return RedirectToAction("CashOutTransactions", "Transactions");
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
        [HttpPost]
        public IActionResult DrawResultsHeader(DrawResultHeaderViewModel report)
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
                $",RequestedByUsername = usr.firstName{{|}}TABLES{{:}}wpUserLoadTrans load INNER JOIN wpAppUsers usr ON usr.userId = load.userId" +
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
                $",RequestedByUsername = usr.firstName{{|}}TABLES{{:}}wpUserLoadTrans load INNER JOIN wpAppUsers usr ON usr.userId = load.userId" +
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
                var query = $"COLUMNS{{:}}load.*,PlayerName = usr.firstName{{|}}TABLES{{:}}wpUserLoadTrans load INNER JOIN wpAppUsers usr ON usr.userId = load.userId{{|}}WHERE{{:}}load.loadId = {loadId}";
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

                if (string.IsNullOrEmpty(report.Combination))
                {
                    report.Combination = "%";
                }
                else
                {
                    report.Combination = $"%{_helper.EscapeSqlString(report.Combination)}%";
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
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY Combination) AS RowNum,Combination,SUM(CASE WHEN FirstDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS ThirdTotalBet " +
                        $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END) AS TotalBet" +
                        $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND Combination LIKE '{report.Combination}' " +
                        $"AND firstDrawSelected = 1 {{|}}GROUP{{:}}Combination{{|}}SORT{{:}}Combination";
                }
                else if (report.SecondDrawSelected == 1)
                {
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY Combination) AS RowNum,Combination,SUM(CASE WHEN FirstDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS ThirdTotalBet " +
                        $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END) AS TotalBet" +
                        $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND Combination LIKE '{report.Combination}' " +
                        $"AND secondDrawSelected = 1 {{|}}GROUP{{:}}Combination{{|}}SORT{{:}}Combination";

                }
                else if (report.ThirdDrawSelected == 1)
                {
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY Combination) AS RowNum,Combination,SUM(CASE WHEN FirstDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS ThirdTotalBet " +
                        $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END) AS TotalBet" +
                        $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND Combination LIKE '{report.Combination}' " +
                        $"AND thirdDrawSelected = 1 {{|}}GROUP{{:}}Combination{{|}}SORT{{:}}Combination";
                }
                else
                {
                    query = $"COLUMNS{{:}}ROW_NUMBER() OVER (ORDER BY Combination) AS RowNum,Combination,SUM(CASE WHEN FirstDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS FirstTotalBet, SUM(CASE WHEN SecondDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END) AS SecondTotalBet,SUM(CASE WHEN ThirdDrawSelected = 1 THEN CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END ELSE 0 END)  AS ThirdTotalBet " +
                            $",SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * CASE WHEN includeRamble = 1 THEN betAmount * 24 ELSE betAmount END) AS TotalBet" +
                            $"{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}drawDate >= '{report.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND drawDate < '{report.ToDate?.ToString("yyyy-MM-dd HH:mm")}' AND Combination LIKE '{report.Combination}'{{|}}GROUP{{:}}Combination{{|}}SORT{{:}}Combination";
                }
                var reportDetails = _helper.GetTableDataModel<SummaryBetDetailReportViewModel>(query)?.ToList();
                report.TotalRows = reportDetails?.Count;
                report.TotalBetAmount = reportDetails?.Sum(x => x.TotalBet);
                report.BetDetails = reportDetails;

                var isCuttOff = _helper.IsAlreadyCuttOff();
                report.IsCuttOff = isCuttOff;

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
            var queryBetDtl = $"COLUMNS{{:}}dtl.*,totalBet = CASE WHEN dtl.includeRamble = 1 THEN (dtl.betAmount * (dtl.firstDrawSelected + dtl.secondDrawSelected + dtl.thirdDrawSelected) * 24) " +
                $"ELSE (dtl.betAmount * (dtl.firstDrawSelected + dtl.secondDrawSelected + dtl.thirdDrawSelected)) END, PlayerName = usr.userName" +
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
        public IActionResult OwnerPlayerBetHistory()
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

            var playersQuery = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpAppUsers{{|}}WHERE{{:}}userName NOT IN (SELECT userName FROM wpOwner){{|}}SORT{{:}}firstName";
            var players = _helper.GetTableDataModel<Player>(playersQuery)?.ToList()!;

            if (players != null)
            {
                report.PlayersLists = players.Select(x => new SelectListItem
                {
                    Value = x.UserId.ToString(),
                    Text = x.firstName
                }).ToList();
            }            

            return View(report);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> OwnerPlayerBetHistory(PlayerHistoryBetHeaderViewModel history)
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

                var selectedUserIds = history.SelectedUserIds != null && history.SelectedUserIds.Any() ? string.Join(",", history.SelectedUserIds) : string.Empty;


                var queryBetDtl = !string.IsNullOrEmpty(selectedUserIds) 
                    ? $"COLUMNS{{:}}*,ROW_NUMBER() OVER (ORDER BY dtl.drawDate, usr.firstName) AS RowNum,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),dtl.betDetailId))" +
                        $",LTRIM(CASE WHEN dtl.firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN dtl.secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN dtl.thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay" +
                        $",totalBet = CASE WHEN dtl.includeRamble = 1 THEN (dtl.betAmount * (dtl.firstDrawSelected + dtl.secondDrawSelected + dtl.thirdDrawSelected) * 24) ELSE (betAmount * (dtl.firstDrawSelected + dtl.secondDrawSelected + dtl.thirdDrawSelected)) END" +
                        $",PlayerName = usr.firstName{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId" +
                        $"{{|}}WHERE{{:}}hdr.userId IN ({selectedUserIds}) AND dtl.drawDate >= '{history.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND dtl.drawDate < '{history.ToDate?.ToString("yyyy-MM-dd HH:mm")}'{{|}}SORT{{:}}RowNum"
                    : $"COLUMNS{{:}}*,ROW_NUMBER() OVER (ORDER BY dtl.drawDate, usr.firstName) AS RowNum,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),dtl.betDetailId))" +
                        $",LTRIM(CASE WHEN dtl.firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN dtl.secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN dtl.thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay" +
                        $",totalBet = CASE WHEN dtl.includeRamble = 1 THEN (dtl.betAmount * (dtl.firstDrawSelected + dtl.secondDrawSelected + dtl.thirdDrawSelected) * 24) ELSE (betAmount * (dtl.firstDrawSelected + dtl.secondDrawSelected + dtl.thirdDrawSelected)) END" +
                        $",PlayerName = usr.firstName{{|}}TABLES{{:}}wpBetDetail dtl INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId" +
                        $"{{|}}WHERE{{:}}dtl.drawDate >= '{history.FromDate?.ToString("yyyy-MM-dd HH:mm")}' AND dtl.drawDate < '{history.ToDate?.ToString("yyyy-MM-dd HH:mm")}'{{|}}SORT{{:}}RowNum";

                history.BetDetails = _helper.GetTableDataModel<WpBetDetailViewModel>(queryBetDtl)?.ToList();
                history.TotalBetAmount = history.BetDetails?.Sum(x => x.TotalBet);

                var playersQuery = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpAppUsers{{|}}WHERE{{:}}userName NOT IN (SELECT userName FROM wpOwner){{|}}SORT{{:}}firstName";
                var players = _helper.GetTableDataModel<Player>(playersQuery)?.ToList()!;

                if (players != null)
                {
                    history.PlayersLists = players.Select(x => new SelectListItem
                    {
                        Value = x.UserId.ToString(),
                        Text = x.firstName
                    }).ToList();
                }
            }
            return View(history);
        }
    }
}
