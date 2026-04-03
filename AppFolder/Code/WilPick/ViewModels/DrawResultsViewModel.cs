using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{

    public class DrawResultHeaderViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }        
        public IList<DrawResultDetailViewModel>? Results { get; set; }
        public int? TotalRows { get; set; }
    }
    public class DrawResultDetailViewModel
    {
        [Display(Name = "No.")]
        public int RowNum { get; set; }
        public decimal ResultId { get; set; }
        public string? ResultIdEnc { get; set; }
        public DateTime? DrawDate { get; set; }
        public DateTime? DateEntered { get; set; }
        public string? FirstResult { get; set; }
        public string? SecondResult { get; set; }
        public string? ThirdResult { get; set; }       
    }

    public class DrawSkedWinningViewModel
    {
        public decimal? UserId { get; set; }
        public string? PlayerName { get; set; }
        public decimal? TotalWinningTargetBet { get; set; }
        public decimal? TargetWinningPrize { get; set; }
        public decimal? TotalWinningRambleBet { get; set; }
        public decimal? RambleWinningPrize { get; set; }
        public decimal? TotalWinningAmount { get; set; }
    }

    public class AgentDrawSkedSalesViewModel
    {
        public decimal? UserId { get; set; }
        public string? AgentName { get; set; }
        public decimal? Commission { get; set; }
        public decimal? TotalBet { get; set; }        
    }

    public class DrawHolidayHeaderViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public IList<DrawHolidayDetailViewModel>? Results { get; set; }
        public int? TotalRows { get; set; }
    }

    public class DrawHolidayDetailViewModel
    {
        [Display(Name = "No.")]
        public int RowNum { get; set; }
        public decimal HolidayId { get; set; }
        public string? HolidayIdEnc { get; set; }
        public DateTime? HolidayDate { get; set; }
        public string? HolidayName { get; set;}
        public string? AddedBy { get; set; }
        public DateTime? AddedDate { get; set; }
        public int? IsDeleted { get; set; }

    }
}
