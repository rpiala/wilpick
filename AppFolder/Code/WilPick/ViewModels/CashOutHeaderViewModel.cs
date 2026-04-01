using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace WilPick.ViewModels
{
    public class CashOutHeaderViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedApproveStatus { get; set; }
        public IList<CashOutDetailViewModel>? details { get; set; }
        public int? TotalRows { get; set; }
    }

    public class CashOutDetailViewModel
    {
        [Display(Name = "No.")]        
        public int? RowNum { get; set; }
        public decimal CashOutId { get; set; }
        public string? CashOutIdEnc { get; set; }
        public decimal UserId { get; set; }
       
        public DateTime? RequestedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public decimal CashOutAmount { get; set; }       
        public string? ProcessedBy { get; set; }
        public int IsCompleted { get; set; }
        public string? IsCompletedDisplay => IsCompleted == 1 ? "Yes" : "No";
        public IFormFile? Attachment { get; set; }
        public string? AttachmentFilename { get; set; }       
        public string? PlayerName { get; set; }       
        public string? ReceiverMobileNumber { get; set; }
        public string? ReceiverName { get; set; }        
        public decimal? LoadBalance {  get; set; }
        public int? IsDeleted { get; set; }
        public string? IsDeletedDisplay => IsDeleted == 1 ? "Yes" : "No";
    }
}