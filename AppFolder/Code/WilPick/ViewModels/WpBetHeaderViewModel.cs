using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{
    public class WpBetHeaderViewModel
    {
        public decimal BetId { get; set; }
        public decimal UserId { get; set; }
        public string AspNetUserID { get; set; }
        public string AgentCode { get; set; }
        public string BetReferenceNo { get; set; }
        public DateTime DrawDate { get; set; }
        public decimal?  BetTicketPrice { get; set; }
        public decimal? WinningPrize { get; set; }
        public decimal? RambleWinningPrize { get; set; }
        public IList<WpBetDetailViewModel> BetDetails { get; set; }
        public bool IsCuttOff { get; set; }
        public decimal? PlayerRemainingload { get; set; }
        public string? BetType { get; set; }
        public decimal? TotalBetAmount { get; set; }
        public decimal? TotalCashIn { get; set; }
        public decimal? OverallTotalBet { get; set; }
        public decimal? TotalCashOut { get; set; }

    }

    public class WpCashFlowViewModel 
    { 
        public decimal? TotalCashIn { get; set; }
        public decimal? OverallTotalBet { get; set; }
        public decimal? TotalCashOut { get; set; }
    }
}
