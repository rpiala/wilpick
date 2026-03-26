using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace WilPick.ViewModels
{
    public class PlayerLoadTransactionsViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedApproveStatus { get; set; }
        public IList<PlayerLoadDetailViewModel>? LoadDetails { get; set; }
        public int? TotalRows { get; set; }        
    }
}