
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using WilPick.ViewModels;
using Constants = WilPick.Common.Constant;
using Role = WilPick.Common.Roles;
using Logger = WilPick.Common;
using WilPick.Helpers;

namespace WilPick.Data
{


    public sealed class DbSettings
    {
        public int CommandTimeoutSeconds { get; set; } = 30;
    }

    public sealed class SqlConnectionFactory
    {
        private readonly string _connStr;
        public SqlConnectionFactory(string connStr) => _connStr = connStr;
        public SqlConnection Create() => new SqlConnection(_connStr);
    }


    public sealed class DataHelper
    {
        private readonly SqlConnectionFactory _factory;
        private readonly int _dbTimeout;
        private readonly IList<SmSettingsViewModel> _smSettings;
        private readonly DateTime _now = DateTime.Now;
        private readonly IList<DrawHolidayDetailViewModel> _drawHolidays = new List<DrawHolidayDetailViewModel>();
        //private readonly DateTime _now = new DateTime(2026, 4, 1, 15, 0, 0);

        // Cache property maps for types to speed up reflection
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propMapCache = new();


        public DataHelper(SqlConnectionFactory factory, IOptions<DbSettings> options)
        {
            _factory = factory;
            _dbTimeout = options.Value.CommandTimeoutSeconds;
            _smSettings = GetTableDataModel<SmSettingsViewModel>($"COLUMNS{{:}}*{{|}}TABLES{{:}}dbo.wpSmSettings")?.ToList()!;
            var fromDate = _now.AddMonths(-1);
            var toDate = _now.AddMonths(1);
            _drawHolidays = GetTableDataModel<DrawHolidayDetailViewModel>($"COLUMNS{{:}}*{{|}}TABLES{{:}}dbo.wpDrawHoliday{{|}}WHERE{{:}}isDeleted=0 AND holidayDate >= '{fromDate:yyyy-MM-dd}' AND holidayDate <= '{toDate:yyyy-MM-dd}'")?.ToList() ?? new List<DrawHolidayDetailViewModel>();
        }        

        public WpAppUserViewModel GetWpUserByUserName(string userName)
        {
            var query = $"COLUMNS{{:}}usr.*,CASE WHEN EXISTS (SELECT 1 FROM wpOwner WHERE userName = usr.userName) THEN '{Role.Owner}' " +
                $"WHEN EXISTS (SELECT 1 FROM wpAgents WHERE userName = usr.userName) THEN '{Role.Agent}' ELSE '{Role.Client}' END AS accessRole{{|}}" +
                $"TABLES{{:}}dbo.wpAppUsers usr{{|}}WHERE{{:}}usr.userName = '{EscapeSqlString(userName)}'";
            return GetTableDataModel<WpAppUserViewModel>(query)?.FirstOrDefault() ?? new WpAppUserViewModel();
        }

        public WpAppUserViewModel GetWpUserByUserId(decimal userId)
        {
            var query = $"COLUMNS{{:}}usr.*,CASE WHEN EXISTS (SELECT 1 FROM wpOwner WHERE userName = usr.userName) THEN '{Role.Owner}' " +
                $"WHEN EXISTS (SELECT 1 FROM wpAgents WHERE userName = usr.userName) THEN '{Role.Agent}' ELSE '{Role.Client}' END AS accessRole{{|}}" +
                $"TABLES{{:}}dbo.wpAppUsers usr{{|}}WHERE{{:}}usr.userId = '{userId}'";
            return GetTableDataModel<WpAppUserViewModel>(query)?.FirstOrDefault() ?? new WpAppUserViewModel();
        }

        public string GetAgentDefaultCommissionPct()
        {
            var setting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Agent_Commission_Pct", StringComparison.OrdinalIgnoreCase));
            return setting?.VarValue ?? string.Empty;
        }

        public string GetPowerCuttOffTime()
        {
            var setting = _smSettings.FirstOrDefault(s => s.VarName.Equals("CuttOff_Time", StringComparison.OrdinalIgnoreCase));
            return setting?.VarValue ?? string.Empty;
        }

        public string GetPowerOwnerCode()
        {
            var setting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Power_Owner_Code", StringComparison.OrdinalIgnoreCase));
            return setting?.VarValue ?? string.Empty;
        }

        public string GetPowerAgentCode()
        {
            var setting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Power_Agent_Code",StringComparison.OrdinalIgnoreCase));
            return setting?.VarValue ?? string.Empty;
        }
        public string GetGcashReceiverNumber()
        {
            var setting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Gcash_Load_Receiver", StringComparison.OrdinalIgnoreCase));
            return setting?.VarValue ?? string.Empty;
        }

        public decimal GetTicketPrize()
        {
            var betLimitSetting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Ticket_Price", StringComparison.OrdinalIgnoreCase));
            if (betLimitSetting == null || !decimal.TryParse(betLimitSetting.VarValue, out var betAmount))
                return 0; // Default to 0 if setting is missing or invalid
            return betAmount;
        }

        public decimal GetWinningPrize()
        {
            var betLimitSetting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Winning_Prize", StringComparison.OrdinalIgnoreCase));
            if (betLimitSetting == null || !decimal.TryParse(betLimitSetting.VarValue, out var betAmount))
                return 0; // Default to 0 if setting is missing or invalid
            return betAmount;
        }

        public decimal GetRambleWinningPrize()
        {
            var betLimitSetting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Ramble_Winning_Prize", StringComparison.OrdinalIgnoreCase));
            if (betLimitSetting == null || !decimal.TryParse(betLimitSetting.VarValue, out var betAmount))
                return 0; // Default to 0 if setting is missing or invalid
            return betAmount;
        }

        public decimal GetBetLimit()
        {
            var betLimitSetting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Bet_Limit", StringComparison.OrdinalIgnoreCase));
            if (betLimitSetting == null || !decimal.TryParse(betLimitSetting.VarValue, out var betLimit))
                return 0; // Default to 0 if setting is missing or invalid
            return betLimit;
        }

        public bool IsAlreadyCuttOff()
        {
            if ((_now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) || _drawHolidays.Any(h => h.HolidayDate?.Date == _now.Date))
            {
                return false;
            }

            var cutoffSetting = _smSettings.FirstOrDefault(s => s.VarName.Equals("CuttOff_Time", StringComparison.OrdinalIgnoreCase));
            if (cutoffSetting == null || !TimeSpan.TryParse(cutoffSetting.VarValue, out var cutoffTime))
                return false; // Default to not cutoff if setting is missing or invalid

            var startTimeSetting = _smSettings.FirstOrDefault(s => s.VarName.Equals("Start_Time", StringComparison.OrdinalIgnoreCase));
            if (startTimeSetting == null || !TimeSpan.TryParse(startTimeSetting.VarValue, out var startTime))
                return false; // Default to not cutoff if setting is missing or invalid

            //var now = DateTime.Now.TimeOfDay;
            var now = _now.TimeOfDay;
            return now >= cutoffTime && now < startTime;
        }           

        public DateTime GetDrawDate()
        {
            var now = _now;

            while (_drawHolidays.Any(h => h.HolidayDate?.Date == now.Date) || now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
            {
                now = now.AddDays(1);
            }

            // Try a few common setting names for start time
            var startSetting = _smSettings.FirstOrDefault(s => string.Equals(s.VarName, "Start_Time", StringComparison.OrdinalIgnoreCase))?.VarValue;
            var cuffOffTime = _smSettings.FirstOrDefault(s => string.Equals(s.VarName, "CuttOff_Time", StringComparison.OrdinalIgnoreCase))?.VarValue;

            // Default start time fallback (11:00:00) if setting missing/invalid
            TimeParser.TryExtractHms(cuffOffTime?.ToString(), out var hour, out var minute, out var seconds);
            TimeOnly startTime = new TimeOnly(hour, minute, seconds);
            if (startSetting != null && !string.IsNullOrWhiteSpace(startSetting))
            {
                if (TryParseTimeOfDay(startSetting, out var parsed))
                    startTime = parsed;
            }

            // Determine target date
            DateTime targetDate;
            var currentTimeOnly = TimeOnly.FromDateTime(now);

            if (currentTimeOnly >= startTime)
            {
                // Move to next day
                var tomorrow = now.Date.AddDays(1);

                // If today is Friday, next valid draw is Monday (add 3 days)
                if (now.DayOfWeek == DayOfWeek.Friday)
                    targetDate = now.Date.AddDays(3);
                else
                    targetDate = tomorrow;
            }
            else
            {
                // Draw remains today
                targetDate = now.Date;
            }

            // If targetDate lands on weekend, roll forward to Monday
            if (targetDate.DayOfWeek == DayOfWeek.Saturday)
                targetDate = targetDate.AddDays(2);
            else if (targetDate.DayOfWeek == DayOfWeek.Sunday)
                targetDate = targetDate.AddDays(1);

            // Set draw time to 12:00:00 (noon)
            var drawDateTime = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, hour, minute, seconds);

            return drawDateTime;
        }

        public static bool TryParseTimeOfDay(string? input, out TimeOnly time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();            

            return TimeOnly.TryParse(input, System.Globalization.CultureInfo.InvariantCulture, out time);
        }

        public static bool TryParseTimeSpan(string? input, out TimeSpan timeSpan)
        {
            timeSpan = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            return TimeSpan.TryParse(input.Trim(), System.Globalization.CultureInfo.InvariantCulture, out timeSpan);
        }

        // Helper to check database for agent code
        public async Task<bool> AgentCodeExistsAsync(string agentCode)
        {
            // Sanitize to avoid breaking the helper SQL string; prefer parameterized helper if available.
            var safe = EscapeSqlString(agentCode);

            // Adjust this query to match DataHelper's expected input format.
            var dt = await GetTableDataAsync($"COLUMNS{{:}}COUNT(*){{|}}TABLES{{:}}dbo.wpAgents WHERE AgentCode = '{safe}'");

            if (dt == null) return false;
            if (dt.Rows.Count == 0) return false;

            var val = dt.Rows[0][0];
            if (val == null || val == DBNull.Value) return false;

            return Convert.ToInt32(val) > 0;
        }

        public string DecryptString(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            // IMPORTANT: Do NOT wrap in quotes and do NOT escape
            var dt = GetTableData(
                $"COLUMNS{{:}}dbo.DecryptString('{cipherText}')"
            );

            if (dt == null || dt.Rows.Count == 0)
                return string.Empty;

            var val = dt.Rows[0][0];
            return val == DBNull.Value ? string.Empty : val.ToString()!;
        }

        public string EscapeSqlString(string? s) => (s ?? string.Empty).Replace("'", "''");

        public string GetBaseCombination(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Dictionary preserves first occurrence while enforcing case-insensitive uniqueness
            var map = new Dictionary<char, char>();

            for (int i = 0; i < input.Length; i++)
            {
                char original = input[i];
                char key = char.ToUpperInvariant(original);

                // Keep first occurrence only
                if (!map.ContainsKey(key))
                {
                    map[key] = original;
                }
            }

            // Sort by uppercase key (same as SQL ORDER BY keych)
            var result = map
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value);

            return new string(result.ToArray());
        }

        public decimal GetRemainingLoad(decimal? userId)
            => GetRemainingLoadAsync(userId).GetAwaiter().GetResult();

        private async Task<decimal> GetRemainingLoadAsync(decimal? userId)
        {                        
            var dt = await GetTableDataAsync($"COLUMNS{{:}}dbo.GetPlayerRemainingLoad({userId})");

            if (dt == null) return 0;
            if (dt.Rows.Count == 0) return 0;

            var val = dt.Rows[0][0];
            if (val == null || val == DBNull.Value) return 0;

            return Convert.ToInt32(val);
        }

        public void CreateUpdateDrawHoliday(DrawHolidayDetailViewModel holiday, WpAppUserViewModel wpUser)
            => CreateUpdateDrawHolidayAsync(holiday, wpUser).GetAwaiter().GetResult();

        public async Task<bool> CreateUpdateDrawHolidayAsync(DrawHolidayDetailViewModel holiday, WpAppUserViewModel wpUser, CancellationToken ct = default)
        {
            var now = _now;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;


                var drawResultQuery = string.Empty;

                if (holiday?.HolidayId == null || holiday?.HolidayId <= 0)
                {
                    drawResultQuery = $"COLUMNSINSERT{{:}}wpDrawHoliday(holidayDate,holidayName,addedBy,addedDate,isDeleted){{|}}" +
                            $"VALUES{{:}}('{holiday?.HolidayDate}','{EscapeSqlString(holiday?.HolidayName)}','{EscapeSqlString(wpUser?.FirstName)}','{_now}',0)";
                }
                else
                {
                    drawResultQuery = $"UPDATETABLE{{:}}wpDrawHoliday{{|}}COLUMNSVALUESET{{:}}holidayDate ='{holiday?.HolidayDate}', holidayName ='{EscapeSqlString(holiday?.HolidayName)}'" +
                        $", addedBy = '{wpUser.FirstName}'{{|}}WHERE{{:}}holidayId = {holiday?.HolidayId}";
                }

                var (okDrawResult, newResultId) = await InsertUpdateTableDataAsync(drawResultQuery, cm, ct);
                if (!okDrawResult)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }                              

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateUpdateDrawHolidayAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }

            return true;
        }

        public void DeleteDrawHoliday(DrawHolidayDetailViewModel holiday)
            => DeleteDrawHolidayAsync(holiday).GetAwaiter().GetResult();

        public async Task<bool> DeleteDrawHolidayAsync(DrawHolidayDetailViewModel holiday, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");
            var drawDate = GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            var queryDeleteBetDtl = $"UPDATETABLE{{:}}wpDrawHoliday{{|}}COLUMNSVALUESET{{:}}isDeleted=1{{|}}WHERE{{:}}holidayId = {holiday.HolidayId}";

            string formatted = queryDeleteBetDtl;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var (ok, _) = await InsertUpdateTableDataAsync(formatted, cm, ct);
                if (!ok)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "DeleteDrawHolidayAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
            return true;
        }

        public void CreateUpdateDrawResult(DrawResultDetailViewModel result, WpAppUserViewModel wpUser)
            => CreateUpdateDrawResultAsync(result, wpUser).GetAwaiter().GetResult();

        public async Task<bool> CreateUpdateDrawResultAsync(DrawResultDetailViewModel result, WpAppUserViewModel wpUser, CancellationToken ct = default)
        {

            var now = _now;
            var strTime = GetPowerCuttOffTime();


            var time = TimeSpan.Parse(strTime);
            var combinedDateTime = now.Date + time;

            result.DrawDate = result.DrawDate == null ? combinedDateTime : result.DrawDate;
            
            var winningQuery = $"COLUMNS{{:}}*{{|}}TABLES{{:}}dbo.GetDrawSkedWinningWithRamble('{result.DrawDate?.ToString("yyyy-MM-dd")}','{EscapeSqlString(result.FirstResult)}'" +
                $",'{EscapeSqlString(result.SecondResult)}','{EscapeSqlString(result.ThirdResult)}')";
            var winnings = GetTableDataModel<DrawSkedWinningViewModel>(winningQuery)?.ToList() ?? new List<DrawSkedWinningViewModel>();

            var agentSalesQuery = $"COLUMNS{{:}}*{{|}}TABLES{{:}}dbo.GetAgentDrawSkedSales('{result.DrawDate?.ToString("yyyy-MM-dd")}')";
            var agentSales = GetTableDataModel<AgentDrawSkedSalesViewModel>(agentSalesQuery)?.ToList() ?? new List<AgentDrawSkedSalesViewModel>();

            var loadTransQuery = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpUserLoadTrans{{|}}WHERE{{:}}resultId ={result.ResultId}";
            var loadTrans = GetTableDataModel<PlayerLoadDetailViewModel>(loadTransQuery)?.ToList() ?? new List<PlayerLoadDetailViewModel>();

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;


                var drawResultQuery = string.Empty;

                if (result?.ResultId == null || result?.ResultId <= 0)
                {
                    drawResultQuery = $"COLUMNSINSERT{{:}}wpDrawResults(drawDate,dateEntered,firstResult,secondResult,thirdResult,enteredBy){{|}}" +
                            $"VALUES{{:}}('{result?.DrawDate}','{_now}','{EscapeSqlString(result?.FirstResult)}','{EscapeSqlString(result?.SecondResult)}','{EscapeSqlString(result?.ThirdResult)}','{wpUser.FirstName}')";                    
                }
                else
                {
                    drawResultQuery = $"UPDATETABLE{{:}}wpDrawResults{{|}}COLUMNSVALUESET{{:}}firstResult ='{EscapeSqlString(result?.FirstResult)}', secondResult ='{EscapeSqlString(result?.SecondResult)}', " +
                        $"thirdResult ='{EscapeSqlString(result?.ThirdResult)}', enteredBy='{wpUser.FirstName}'{{|}}WHERE{{:}}resultId = {result?.ResultId}";
                }

                var (okDrawResult, newResultId) = await InsertUpdateTableDataAsync(drawResultQuery, cm, ct);
                if (!okDrawResult)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }
                if (result?.ResultId == null || result.ResultId <= 0)
                {
                    result.ResultId = newResultId is decimal d ? d : Convert.ToDecimal(newResultId);
                }

                if (loadTrans.Count > 0)
                {
                    var deleteLoadTransQuery = $"DELETE{{:}}{{|}}TABLES{{:}}wpUserLoadTrans{{|}}WHERE{{:}}resultId = {result?.ResultId}";

                    var (okDtl, _) = await InsertUpdateTableDataAsync(deleteLoadTransQuery, cm, ct);
                    if (!okDtl)
                    {
                        await trans.RollbackAsync(ct);
                        return false;
                    }
                }

                if (agentSales.Count > 0)
                {
                    var agentInsertQuery = string.Empty;
                                     
                    foreach (var agentSale in agentSales)
                    {
                        agentInsertQuery = $"COLUMNSINSERT{{:}}wpUserLoadTrans(userId,requestedDate,approvedDate,requestedAmount,approvedAmount,approvedBy,isApproved,resultId,remarks){{|}}" +
                            $"VALUES{{:}}({agentSale.UserId},'{_now}','{_now}',{agentSale.Commission},{agentSale.Commission},'{wpUser.FirstName}',1,{result?.ResultId},'Commission Sale from DrawDate: {result?.DrawDate?.ToString("MMM dd,yyyy")}')";
                        
                        var (okAgentSale, _) = await InsertUpdateTableDataAsync(agentInsertQuery, cm, ct);
                        if (!okAgentSale)
                        {
                            await trans.RollbackAsync(ct);
                            return false;
                        }
                    }
                }

                if(winnings.Count > 0)
                {
                    var playerInsertQuery = string.Empty;

                    foreach (var winning in winnings)
                    {
                        playerInsertQuery = $"COLUMNSINSERT{{:}}wpUserLoadTrans(userId,requestedDate,approvedDate,requestedAmount,approvedAmount,approvedBy,isApproved,resultId,remarks){{|}}" +
                            $"VALUES{{:}}({winning.UserId},'{_now}','{_now}',{winning.TotalWinningAmount},{winning.TotalWinningAmount},'{wpUser.FirstName}',1,{result?.ResultId},'Winning From DrawDate: {result?.DrawDate?.ToString("MMM dd,yyyy")}')";

                        var (okPlayerWinning, _) = await InsertUpdateTableDataAsync(playerInsertQuery, cm, ct);
                        if (!okPlayerWinning)
                        {
                            await trans.RollbackAsync(ct);
                            return false;
                        }
                    }
                }
                    
                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateUpdateDrawResultAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }


            return true;
        }

        public void DisapproveLoadTransaction(PlayerLoadDetailViewModel loadDetail)
            => DisapproveLoadTransactionAsync(loadDetail).GetAwaiter().GetResult();

        public async Task<bool> DisapproveLoadTransactionAsync(PlayerLoadDetailViewModel loadDetail, CancellationToken ct = default)
        {
            var now = _now;

            loadDetail.ApprovedDate = now;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var formattedDtl = string.Empty;

                if (loadDetail?.LoadId != null && loadDetail.LoadId > 0)
                {
                    formattedDtl = $"UPDATETABLE{{:}}wpUserLoadTrans{{|}}COLUMNSVALUESET{{:}}isApproved ={loadDetail.IsApproved}, approvedDate='{loadDetail.ApprovedDate}'" +
                        $"{{|}}WHERE{{:}}loadId ={loadDetail.LoadId}";
                }

                var (okDtl, _) = await InsertUpdateTableDataAsync(formattedDtl, cm, ct);
                if (!okDtl)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateUpdateLoadTransactionAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }


            return true;
        }

        public void ApproveLoadTransaction(PlayerLoadDetailViewModel loadDetail)
            => ApproveLoadTransactionAsync(loadDetail).GetAwaiter().GetResult();

        public async Task<bool> ApproveLoadTransactionAsync(PlayerLoadDetailViewModel loadDetail, CancellationToken ct = default)
        {
            var now = _now;

            loadDetail.ApprovedDate = now;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var formattedDtl = string.Empty;

                if (loadDetail?.LoadId != null && loadDetail.LoadId > 0)
                {
                    formattedDtl = $"UPDATETABLE{{:}}wpUserLoadTrans{{|}}COLUMNSVALUESET{{:}}approvedAmount ={loadDetail.ApprovedAmount}" +
                        $",approvedBy = '{loadDetail.ApprovedBy}' ,isApproved ={loadDetail.IsApproved}, approvedDate='{loadDetail.ApprovedDate}'" +
                        $"{{|}}WHERE{{:}}loadId ={loadDetail.LoadId}";
                }               

                var (okDtl, _) = await InsertUpdateTableDataAsync(formattedDtl, cm, ct);
                if (!okDtl)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateUpdateLoadTransactionAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }


            return true;
        }

        public void CreateUpdateCashOutTransaction(CashOutDetailViewModel detail)
            => CreateUpdateCashOutTransactionAsync(detail).GetAwaiter().GetResult();

        public async Task<bool> CreateUpdateCashOutTransactionAsync(CashOutDetailViewModel detail, CancellationToken ct = default)
        {
            var now = _now;

            if (detail?.CashOutId != null && detail.CashOutId == 0)
            {
                detail.RequestedDate = now;                
                detail.ProcessedBy = string.Empty;
                detail.IsCompleted = 0;
            }

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var formattedDtl = string.Empty;

                if (detail?.CashOutId != null && detail.CashOutId > 0)
                {
                    formattedDtl = $"UPDATETABLE{{:}}wpCashOutTransactions{{|}}COLUMNSVALUESET{{:}}cashOutAmount ={detail.CashOutAmount}, " +
                        $"attachmentFileName = '{detail.AttachmentFilename}', receiverMobileNumber = '{detail.ReceiverMobileNumber}', receiverName='{detail.ReceiverName}'{{|}}WHERE{{:}}cashOutId ={detail.CashOutId}";
                }
                else
                {
                    formattedDtl = $"COLUMNSINSERT{{:}}wpCashOutTransactions(userId,requestedDate,cashOutAmount,isCompleted,attachmentFileName,receiverMobileNumber,receiverName,isDeleted){{|}}" +
                        $"VALUES{{:}}({detail?.UserId},'{detail?.RequestedDate}',{detail?.CashOutAmount},{detail?.IsCompleted},'{detail?.AttachmentFilename}','{detail?.ReceiverMobileNumber}'" +
                        $",'{detail?.ReceiverName}',0)";
                }

                var (okDtl, _) = await InsertUpdateTableDataAsync(formattedDtl, cm, ct);
                if (!okDtl)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateUpdateCashOutTransactionAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }

            return true;
        }

        public void CreateUpdateLoadTransaction(PlayerLoadDetailViewModel loadDetail)
            => CreateUpdateLoadTransactionAsync(loadDetail).GetAwaiter().GetResult();

        public async Task<bool> CreateUpdateLoadTransactionAsync(PlayerLoadDetailViewModel loadDetail, CancellationToken ct = default)
        {
            var now = _now;

            if (loadDetail?.LoadId != null && loadDetail.LoadId == 0)
            {
                loadDetail.RequestedDate = now;
                loadDetail.ApprovedAmount = loadDetail.RequestedAmount;
                loadDetail.ApprovedBy = string.Empty;
                loadDetail.IsApproved = 0;
            }

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var formattedDtl = string.Empty;

                if (loadDetail?.LoadId != null && loadDetail.LoadId > 0)
                {
                    formattedDtl = $"UPDATETABLE{{:}}wpUserLoadTrans{{|}}COLUMNSVALUESET{{:}}requestedAmount ={loadDetail.RequestedAmount}, attachmentFileName = '{loadDetail.AttachmentFilename}'" +
                        $", receiverMobileNumber = '{loadDetail.ReceiverMobileNumber}'{{|}}WHERE{{:}}loadId ={loadDetail.LoadId}";
                }
                else
                {
                    formattedDtl = $"COLUMNSINSERT{{:}}wpUserLoadTrans(userId,requestedDate,requestedAmount,isApproved,attachmentFileName,receiverMobileNumber){{|}}" +
                        $"VALUES{{:}}({loadDetail?.UserId},'{loadDetail?.RequestedDate}',{loadDetail?.RequestedAmount},{loadDetail?.IsApproved},'{loadDetail?.AttachmentFilename}','{loadDetail?.ReceiverMobileNumber}')";
                }

                var (okDtl, _) = await InsertUpdateTableDataAsync(formattedDtl, cm, ct);
                if (!okDtl)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateUpdateLoadTransactionAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }


            return true;
        }

        public void CompleteCashOut(CashOutDetailViewModel cashOut)
            => CompleteCashOutAsync(cashOut).GetAwaiter().GetResult();

        public async Task<bool> CompleteCashOutAsync(CashOutDetailViewModel cashOut, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");
            var drawDate = GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            var queryDeleteBetDtl = $"UPDATETABLE{{:}}wpCashOutTransactions{{|}}COLUMNSVALUESET{{:}}isCompleted=1, processedBy='{cashOut.ProcessedBy}', attachmentFileName = '{cashOut.AttachmentFilename}'" +
                $"{{|}}WHERE{{:}}cashOutId = {cashOut.CashOutId} AND userId = {cashOut.UserId}";

            string formatted = queryDeleteBetDtl;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var (ok, _) = await InsertUpdateTableDataAsync(formatted, cm, ct);
                if (!ok)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CompleteCashOutAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
            return true;
        }

        public void DeleteCashOut(CashOutDetailViewModel cashOut)
            => DeleteCashOutAsync(cashOut).GetAwaiter().GetResult();

        public async Task<bool> DeleteCashOutAsync(CashOutDetailViewModel cashOut, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");
            var drawDate = GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            var queryDeleteBetDtl = $"UPDATETABLE{{:}}wpCashOutTransactions{{|}}COLUMNSVALUESET{{:}}isDeleted=1{{|}}WHERE{{:}}cashOutId = {cashOut.CashOutId} AND userId = {cashOut.UserId}";

            string formatted = queryDeleteBetDtl;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var (ok, _) = await InsertUpdateTableDataAsync(formatted, cm, ct);
                if (!ok)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateLoginTransactionLogAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
            return true;
        }

        public void DeleteWpBetDetail(WpBetDetailViewModel detail)
            => DeleteWpBetDetailAsync(detail).GetAwaiter().GetResult();

        public async Task<bool> DeleteWpBetDetailAsync(WpBetDetailViewModel betDtl, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");
            var drawDate = GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            var queryDeleteBetDtl = $"DELETE{{:}}{{|}}TABLES{{:}}wpBetDetail{{|}}WHERE{{:}}betId = '{betDtl.BetId}' AND betDetailId = '{betDtl.BetDetailId}' AND drawDate ='{drawDate}'";

            string formatted = queryDeleteBetDtl;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;
                
                var (ok,_) = await InsertUpdateTableDataAsync(formatted,cm,ct);
                if (!ok)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateLoginTransactionLogAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
            return true;
        }

        public void UpdateWpClient(WpAppUserViewModel client)
            => UpdateWpClientAsync(client).GetAwaiter().GetResult();

        public async Task<bool> UpdateWpClientAsync(WpAppUserViewModel client, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");
            var drawDate = GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");            

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;
               
                var formattedDtl = $"UPDATETABLE{{:}}wpAppUsers{{|}}COLUMNSVALUESET{{:}}betType = '{this.EscapeSqlString(client.betType)}',betTicketPrice={client.BetTicketPrice}," +
                    $"winningPrize={client.WinningPrize}{{|}}WHERE{{:}}userId ={client.UserId}";
                var (okDtl, _) = await InsertUpdateTableDataAsync(formattedDtl, cm, ct);
                if (!okDtl)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "UpdateWpClientAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
            return true;
        }

        public void CreateWpBetHeader(WpBetHeaderViewModel header)
            => CreateWpBetHeaderAsync(header).GetAwaiter().GetResult();

        public async Task<bool> CreateWpBetHeaderAsync(WpBetHeaderViewModel header, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");
            var drawDate = GetDrawDate().ToString("yyyy-MM-dd HH:mm:ss");

            var queryBetHdr = $"COLUMNS{{:}}*{{|}}TABLES{{:}}wpBetHeader{{|}}WHERE{{:}}userId = '{header?.UserId}' AND drawDate ='{drawDate}'";
            var betDbHdr = GetTableDataModel<WpBetHeaderViewModel>(queryBetHdr)?.FirstOrDefault();

            var sb = new StringBuilder();

            sb.Append("COLUMNSINSERT{:}wpBetHeader(userid,aspNetUserID,agentCode,betReferenceNo,drawDate,betTicketPrice,winningPrize,rambleWinningPrize)")
              .Append("{|}VALUES{:}(")
              .Append(header?.UserId).Append(",'")
              .Append(EscapeSqlString(header?.AspNetUserID)).Append("','")
              .Append(EscapeSqlString(header?.AgentCode)).Append("','")
              .Append(EscapeSqlString(header?.BetReferenceNo)).Append("','")
              .Append(drawDate).Append("','")
              .Append(header?.BetTicketPrice).Append("',")
              .Append(header?.WinningPrize).Append(",")
              .Append(header?.RambleWinningPrize).Append(")");

            string formatted = sb.ToString();

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                if (betDbHdr == null)
                {
                    var (ok, newId) = await InsertUpdateTableDataAsync(formatted, cm, ct);
                    if (!ok)
                    {
                        await trans.RollbackAsync(ct);
                        return false;
                    }

                    header.BetId = newId is decimal d ? d : Convert.ToDecimal(newId);
                }
                else {
                    header.BetId = betDbHdr.BetId;
                }

                var sbWpBetDtl = new StringBuilder();
                if (header.BetDetails != null && header.BetDetails.FirstOrDefault()?.BetDetailId > 0)
                {
                    sbWpBetDtl = header.BetDetails.Aggregate(sbWpBetDtl, (sb, detail) => sb.Append("UPDATETABLE{:}wpBetDetail")
                      .Append("{|}COLUMNSVALUESET{:}combination = '")                      
                      .Append(EscapeSqlString(detail.Combination)).Append("',baseCombination = '")
                      .Append(GetBaseCombination(EscapeSqlString(detail.Combination))).Append("',betAmount =")
                      .Append(detail.BetAmount).Append(", betType = '")
                      .Append(detail.betType).Append("', firstDrawSelected = ")
                      .Append(detail.FirstDrawSelected).Append(", secondDrawSelected = ")
                      .Append(detail.SecondDrawSelected).Append(", thirdDrawSelected = ")
                      .Append(detail.ThirdDrawSelected).Append(", includeRamble = ")
                      .Append(detail.IncludeRamble).Append("{|}WHERE{:}betDetailId = ")
                      .Append(detail.BetDetailId).Append(" AND betId = ")
                      .Append(header.BetId));
                }
                else
                {
                    sbWpBetDtl = header.BetDetails.Aggregate(sbWpBetDtl, (sb, detail) => sb.Append("COLUMNSINSERT{:}wpBetDetail(betId,drawDate,dateCreated,combination,baseCombination,betAmount,firstDrawSelected,secondDrawSelected,thirdDrawSelected,betType,includeRamble)")
                      .Append("{|}VALUES{:}(")
                      .Append(header.BetId).Append(",'")
                      .Append(EscapeSqlString(drawDate)).Append("','")
                      .Append(EscapeSqlString(transDate)).Append("','")
                      .Append(EscapeSqlString(detail.Combination)).Append("','")
                      .Append(GetBaseCombination(detail.Combination)).Append("','")
                      .Append(detail.BetAmount).Append("',")
                      .Append(detail.FirstDrawSelected).Append(",")
                      .Append(detail.SecondDrawSelected).Append(",")
                      .Append(detail.ThirdDrawSelected).Append(",'")
                      .Append(detail.betType).Append("',")
                      .Append(detail.IncludeRamble).Append(")"));
                }
                var formattedDtl = sbWpBetDtl.ToString();
                var (okDtl, _) = await InsertUpdateTableDataAsync(formattedDtl, cm, ct);
                if (!okDtl)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }              

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateLoginTransactionLogAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
            return true;
        }

        public bool CreateWpAppUser(WpAppUserViewModel user)
            => CreateWpAppUserAsync(user).GetAwaiter().GetResult();

        public async Task<bool> CreateWpAppUserAsync(WpAppUserViewModel user, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");

            var primeAgentQuery = $"COLUMNS{{:}}usr.*{{|}}TABLES{{:}}wpAgents agent INNER JOIN wpAppUsers usr ON usr.agentCode = agent.agentCode" +
                $"{{|}}WHERE{{:}}agent.agentCode='PRIME'";
            var firstRegisteredOwner = GetTableDataModel<WpAppUserViewModel>(primeAgentQuery)?.FirstOrDefault()!;

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                if(user.AgentCode == GetPowerOwnerCode())
                {
                    var powerOwnerCode = GetPowerOwnerCode();
                    var insertOwner = $"COLUMNSINSERT{{:}}wpOwner(UserName,mobileNumber)" +
                        $"{{|}}VALUES{{:}}('{EscapeSqlString(user.UserName)}','{EscapeSqlString(user.MobileNumber)}')";

                    var (okAgent, _) = await InsertUpdateTableDataAsync(insertOwner, cm, ct);
                    if (!okAgent)
                    {
                        await trans.RollbackAsync(ct);
                        return false;
                    }

                    if (firstRegisteredOwner == null)
                    {
                        var inserAgent = $"COLUMNSINSERT{{:}}wpAgents(AgentCode,userName,agentName,commissionPct,activeStatus)" +
                            $"{{|}}VALUES{{:}}('{powerOwnerCode}','{user.UserName}','{user.FirstName}',{Convert.ToDecimal(GetAgentDefaultCommissionPct())},1)";

                        var (okOwner, _) = await InsertUpdateTableDataAsync(inserAgent, cm, ct);
                        if (!okOwner)
                        {
                            await trans.RollbackAsync(ct);
                            return false;
                        }
                    }
                    user.AgentCode = powerOwnerCode;
                }
                else if (user.AgentCode == GetPowerAgentCode())
                {
                    string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    Random random = new Random();

                    string resultAgentCode = new string(
                        Enumerable.Range(0, 5)
                                  .Select(_ => chars[random.Next(chars.Length)])
                                  .ToArray()
                    );

                    var insertAgent = $"COLUMNSINSERT{{:}}wpAgents(AgentCode,userName,agentName,commissionPct,activeStatus)" +
                        $"{{|}}VALUES{{:}}('{resultAgentCode}','{user.UserName}','{user.FirstName}',{Convert.ToDecimal(GetAgentDefaultCommissionPct())},1)";

                    var (okAgent, _) = await InsertUpdateTableDataAsync(insertAgent, cm, ct);
                    if (!okAgent)
                    {
                        await trans.RollbackAsync(ct);
                        return false;
                    }
                    user.AgentCode = resultAgentCode;
                }

                var sb = new StringBuilder();

                sb.Append("COLUMNSINSERT{:}wpAppUsers(aspNetUserID,AgentCode,dateRegistered,userName,email,firstName,betTicketPrice,winningPrize,rambleWinningPrize,betType,mobileNumber)")
                    .Append("{|}VALUES{:}('")
                    .Append(EscapeSqlString(user.AspNetUserId)).Append("','")
                    .Append(EscapeSqlString(user.AgentCode)).Append("','")
                    .Append(transDate).Append("','")
                    .Append(EscapeSqlString(user.UserName)).Append("','")
                    .Append(EscapeSqlString(user.Email)).Append("','")
                    .Append(EscapeSqlString(user.FirstName)).Append("',")
                    .Append(user.BetTicketPrice).Append(",")
                    .Append(user.WinningPrize).Append(",")
                    .Append(user.RambleWinningPrize).Append(",'")
                    .Append(user.betType).Append("','")
                    .Append(user.MobileNumber).Append("')");

                string formatted = sb.ToString();

                var (ok, newId) = await InsertUpdateTableDataAsync(formatted, cm, ct);
                if (!ok)
                {
                    await trans.RollbackAsync(ct);
                    return false;
                }                

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateWpAppUserAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
            return true;
        }

        // -----------------------------
        //  CreateLoginLogoutTransactionLog
        // -----------------------------
        public async Task CreateLoginLogoutTransactionLogAsync(string transType, string userName, CancellationToken ct = default)
        {
            string transDate = _now.ToString("yyyy-MM-dd HH:mm:ss");

            var sb = new StringBuilder();

            if (transType == Constants.LOGINTRANSTYPE)
            {
                sb.Append("COLUMNSINSERT{:}wpUserLogTransaction(RequestDate,UserName,TransactionType,RequestDetails)")
                  .Append("{|}VALUES{:}('")
                  .Append(transDate).Append("','")
                  .Append(EscapeSqlString(userName)).Append("','")
                  .Append("Login").Append("','")
                  .Append("Login Request by user").Append("')");
            }
            else
            {
                sb.Append("COLUMNSINSERT{:}wpUserLogTransaction(RequestDate,UserName,TransactionType,RequestDetails)")
                  .Append("{|}VALUES{:}('")
                  .Append(transDate).Append("','")
                  .Append(EscapeSqlString(userName)).Append("','")
                  .Append("Logout").Append("','")
                  .Append("Logout Request by user").Append("')");
            }

            string formatted = sb.ToString();

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                var (ok, newId) = await InsertUpdateTableDataAsync(formatted, cm, ct);
                if (!ok)
                {
                    await trans.RollbackAsync(ct);
                    return;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateLoginLogoutTransactionLogAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
        }

        // Optional exact sync version (to match your original signature)
        public void CreateLoginLogoutTransactionLog(string transType, string userName)
            => CreateLoginLogoutTransactionLogAsync(transType,userName).GetAwaiter().GetResult();

        // ---------------------------------
        //  InsertUpdateTableData (Async)
        // ---------------------------------
        public async Task<(bool ok, decimal? newId)> InsertUpdateTableDataAsync(string rawQuery, SqlCommand cm, CancellationToken ct = default)
        {
            try
            {
                string sqlQuery = ParseQuery(rawQuery /*, pageNumber: null, pageSize: null */);

                cm.Parameters.Clear();
                cm.CommandType = CommandType.StoredProcedure;
                cm.CommandTimeout = _dbTimeout;
                cm.CommandText = "dbo.spInsertTableDataQuery";
                cm.Parameters.Add(new SqlParameter("@sqlQuery", SqlDbType.NVarChar) { Value = sqlQuery });

                Logger.Logger.Status("DataHelper", "InsertUpdateTableDataAsync", "Query: " + sqlQuery);


                var outId = new SqlParameter("@newId", SqlDbType.Decimal)
                {
                    Precision = 38,
                    Scale = 0,
                    Direction = ParameterDirection.Output
                };
                cm.Parameters.Add(outId);

                await cm.ExecuteNonQueryAsync(ct);

                decimal? newId = outId.Value == DBNull.Value ? (decimal?)null : Convert.ToDecimal(outId.Value);
                return (true, newId);


                //using var dt = new DataTable();
                //await using var reader = await cm.ExecuteReaderAsync(ct);
                //dt.Load(reader);

                //// Keep same behavior (consume rows if needed)
                //foreach (DataRow _ in dt.Rows) { /* no-op */ }

                //return (true,;
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "InsertUpdateTableDataAsync", ex.Message);
                return (false,null);
            }
        }

        // Optional exact sync version
        public (bool ok, decimal? newId) InsertUpdateTableData(string rawQuery, SqlCommand cm)
            => InsertUpdateTableDataAsync(rawQuery, cm).GetAwaiter().GetResult();

        // ------------------------------------------------------
        //  ParseQuery — faithful to your token rules + paging
        //  Tokens supported:
        //    COLUMNS | TABLES | WHERE | GROUP | SORT
        //    COLUMNSINSERT | VALUES
        //    UPDATETABLE  | COLUMNSVALUESET
        //  Paging (optional): requires SORT/ORDER BY present
        // ------------------------------------------------------
        private static string ParseQuery(string rawQuery, int? pageNumber = null, int? pageSize = null)
        {
            var clauses = rawQuery.Split(new[] { "{|}" }, StringSplitOptions.None);
            var sql = new StringBuilder();
            bool hasOrderBy = false;

            foreach (var clause in clauses)
            {
                var parseClause = clause.Split(new[] { "{:}" }, StringSplitOptions.None);
                if (parseClause.Length > 1)
                {
                    var key = parseClause[0].Trim().ToUpperInvariant();
                    var value = parseClause[1].Trim();

                    switch (key)
                    {
                        case "COLUMNS":
                            sql.Append("SELECT ").Append(value).Append(' ');
                            break;
                        case "TABLES":
                            sql.Append("FROM ").Append(value).Append(' ');
                            break;
                        case "WHERE":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("WHERE ").Append(value).Append(' ');
                            break;
                        case "GROUP":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("GROUP BY ").Append(value).Append(' ');
                            break;
                        case "SORT":
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                sql.Append("ORDER BY ").Append(value).Append(' ');
                                hasOrderBy = true;
                            }
                            break;
                        case "COLUMNSINSERT":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("INSERT INTO ").Append(value).Append(' ');
                            break;
                        case "VALUES":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("VALUES ").Append(value).Append(' ');
                            break;
                        case "UPDATETABLE":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("UPDATE ").Append(value).Append(' ');
                            break;
                        case "COLUMNSVALUESET":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("SET ").Append(value).Append(' ');
                            break;
                        case "DELETE":                            
                            sql.Append("DELETE ").Append(value).Append(' ');
                            break;
                    }
                }
            }

            // Optional pagination: only valid when ORDER BY exists
            if (pageNumber.HasValue && pageSize.HasValue && hasOrderBy)
            {
                int page = Math.Max(1, pageNumber.Value);
                int size = Math.Max(1, pageSize.Value);
                int offset = (page - 1) * size;
                sql.Append($"OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY ");
            }

            // Ensure trailing semicolon for EXEC
            var finalSql = sql.ToString().TrimEnd();
            if (!finalSql.EndsWith(";")) finalSql += ";";
            return finalSql;
        }

        public async Task<DataTable?> GetTableDataAsync(
                string query,
                int? pageNumber = null,
                int? pageSize = null,
                CancellationToken ct = default)
        {
            try
            {
                var dt = new DataTable();

                await using var cn = _factory.Create();
                await cn.OpenAsync(ct);

                await using var cm = cn.CreateCommand();
                cm.CommandType = CommandType.StoredProcedure;
                cm.CommandTimeout = _dbTimeout;
                cm.CommandText = "dbo.spGetTableDataQuery";

                var sqlQuery = ParseQuery(query, pageNumber, pageSize);
                cm.Parameters.Add(new SqlParameter("@sqlQuery", SqlDbType.NVarChar) { Value = sqlQuery });

                Logger.Logger.Status("Datahelper", "GetTableDataAsync", "Query: " + sqlQuery);

                // Execute and load into DataTable (replaces SqlDataAdapter.Fill)
                await using var reader = await cm.ExecuteReaderAsync(ct);
                dt.Load(reader);

                return dt;
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("Datahelper", "GetTableDataAsync", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Optional sync wrapper to preserve a sync call-site.
        /// </summary>
        public DataTable? GetTableData(string query, int? pageNumber = null, int? pageSize = null)
            => GetTableDataAsync(query, pageNumber, pageSize).GetAwaiter().GetResult();


        /// <summary>
        /// .NET 8 async version of GetTableDataModel&lt;T&gt;.
        /// Builds dynamic SQL from your tokenized query and executes dbo.spGetTableDataQuery.
        /// Maps the result to a list of T using reflection (case-insensitive property matching).
        /// Returns null on error to mirror original behavior.
        /// </summary>
        public async Task<List<T>?> GetTableDataModelAsync<T>(
            string query,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken ct = default) where T : new()
        {
            try
            {
                await using var cn = _factory.Create();
                await cn.OpenAsync(ct);

                await using var cm = cn.CreateCommand();
                cm.CommandType = CommandType.StoredProcedure;
                cm.CommandTimeout = _dbTimeout;
                cm.CommandText = "dbo.spGetTableDataQuery";

                var sqlQuery = ParseQuery(query, pageNumber, pageSize);
                cm.Parameters.Add(new SqlParameter("@sqlQuery", SqlDbType.NVarChar) { Value = sqlQuery });

                Logger.Logger.Status("Datahelper", "GetTableDataModelAsync", "Query: " + sqlQuery);

                var list = new List<T>();

                await using var reader = await cm.ExecuteReaderAsync(ct);
                if (!reader.HasRows)
                    return list;

                // Build a column name -> ordinal map once
                var fieldCount = reader.FieldCount;
                var columnOrdinals = new Dictionary<string, int>(fieldCount, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < fieldCount; i++)
                {
                    columnOrdinals[reader.GetName(i)] = i;
                }

                // Get (or build) property map for T (case-insensitive)
                var propMap = GetPropertyMap<T>();

                while (await reader.ReadAsync(ct))
                {
                    var obj = new T();

                    // For each column, map to property if present
                    foreach (var kvp in columnOrdinals)
                    {
                        var colName = kvp.Key;
                        var ordinal = kvp.Value;

                        if (!propMap.TryGetValue(colName, out var prop) || prop == null || !prop.CanWrite)
                            continue;

                        if (await reader.IsDBNullAsync(ordinal, ct))
                        {
                            // Assign null to nullable reference types/nullable value types; skip for non-nullables
                            if (IsNullableProperty(prop))
                                prop.SetValue(obj, null);
                            continue;
                        }

                        var value = reader.GetValue(ordinal);
                        var converted = ConvertToPropertyType(value, prop.PropertyType);
                        prop.SetValue(obj, converted);
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("Datahelper", "GetTableDataModelAsync", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Optional sync wrapper to preserve existing sync call-sites.
        /// </summary>
        public List<T>? GetTableDataModel<T>(string query, int? pageNumber = null, int? pageSize = null) where T : new()
            => GetTableDataModelAsync<T>(query, pageNumber, pageSize).GetAwaiter().GetResult();

        // ----------------- Helpers -----------------

        private static Dictionary<string, PropertyInfo> GetPropertyMap<T>()
        {
            var type = typeof(T);
            if (_propMapCache.TryGetValue(type, out var cached))
                return cached;

            // Case-insensitive property map (public instance, readable/writable)
            var map = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite)
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _propMapCache[type] = map;
            return map;
        }

        private static bool IsNullableProperty(PropertyInfo prop)
        {
            var t = prop.PropertyType;
            return !t.IsValueType || Nullable.GetUnderlyingType(t) != null;
        }

        private static object? ConvertToPropertyType(object value, Type propertyType)
        {
            if (value is null) return null;

            // Handle Nullable<T>
            var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // If already assignable, short-circuit
            if (targetType.IsInstanceOfType(value))
                return value;

            // Enums
            if (targetType.IsEnum)
            {
                if (value is string s) return Enum.Parse(targetType, s, ignoreCase: true);
                return Enum.ToObject(targetType, Convert.ChangeType(value, Enum.GetUnderlyingType(targetType)));
            }

            // Guid
            if (targetType == typeof(Guid))
            {
                if (value is Guid g) return g;
                return Guid.Parse(value.ToString()!);
            }

            // DateTime/DateOnly/TimeOnly conversions if needed
            if (targetType == typeof(DateTime))
                return Convert.ToDateTime(value);
            #if NET8_0_OR_GREATER
            if (targetType == typeof(DateOnly))
            {
                if (value is DateTime dt) return DateOnly.FromDateTime(dt);
                return DateOnly.FromDateTime(Convert.ToDateTime(value));
            }
            if (targetType == typeof(TimeOnly))
            {
                if (value is TimeSpan ts) return TimeOnly.FromTimeSpan(ts);
                if (value is DateTime dtt) return TimeOnly.FromDateTime(dtt);
            }
            #endif

            // Boolean (handle 0/1, "true"/"false")
            if (targetType == typeof(bool))
            {
                if (value is bool b) return b;
                if (value is byte by) return by != 0;
                if (value is short sh) return sh != 0;
                if (value is int i) return i != 0;
                var s = value.ToString()!.Trim();
                if (string.Equals(s, "1") || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "0") || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
                return Convert.ChangeType(value, targetType);
            }

            // Fallback
            return Convert.ChangeType(value, targetType);
        }


    }

}
