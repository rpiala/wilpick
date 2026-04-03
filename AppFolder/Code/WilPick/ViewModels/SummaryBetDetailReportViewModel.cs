using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace WilPick.ViewModels
{
    public class SummaryBetDetailReportViewModel
    {
        [Display(Name = "No.")]
        public int RowNum { get; set; }
        public string Combination { get; set; }
        public int FirstTotalBet { get; set; }
        public int FirstTotalRambleBet { get; set; }
        public int SecondTotalBet { get; set; }
        public int SecondTotalRambleBet { get; set; }
        public int ThirdTotalBet { get; set; }
        public int ThirdTotalRambleBet { get; set; }
        public decimal TotalBet { get; set; }
    }
}