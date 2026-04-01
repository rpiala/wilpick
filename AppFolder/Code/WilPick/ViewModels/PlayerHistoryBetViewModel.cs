using Microsoft.AspNetCore.Mvc.Rendering;
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
        public List<decimal>? SelectedUserIds { get; set; } = new();
        public List<SelectListItem> PlayersLists { get; set; }
            = new();
    }

    public class Player
    {
        public decimal UserId { get; set; }
        public string? firstName { get; set; }
    }   
}