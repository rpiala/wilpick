using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace WilPick.ViewModels
{
    public class PlayerHistoryBetHeaderViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }        
        public IList<WpBetDetailViewModel>? BetDetails { get; set; }
        public int? TotalRows { get; set; }
        public decimal? TotalBetAmount { get; set; }
    }
}