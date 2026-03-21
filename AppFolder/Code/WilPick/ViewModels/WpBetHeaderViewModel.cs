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
        public IList<WpBetDetailViewModel> BetDetails { get; set; }
        public bool IsCuttOff { get; set; }
    }
}
