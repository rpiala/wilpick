using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace WilPick.ViewModels
{
    public class SummaryBetReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Combination { get; set; }
        public int? FirstDrawSelected { get; set; }
        public int? SecondDrawSelected { get; set; }
        public int? ThirdDrawSelected { get; set; }
        public IList<SummaryBetDetailReportViewModel>? BetDetails { get; set; }
        public int? TotalRows { get; set; }
        public decimal? TotalBetAmount { get; set; }
        public bool IsCuttOff { get; set; }
    }
}