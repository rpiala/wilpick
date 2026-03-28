using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Constants = WilPick.Common.Constant;

namespace WilPick.ViewModels
{
    public class PlayerLoadDetailViewModel
    {
        [Display(Name = "No.")]
        public string? ReferenceNo { get; set; }
        public int? RowNum { get; set; }
        public decimal LoadId { get; set; }
        public string? LoadIdEnc { get; set; }
        public decimal UserId { get; set; }
        public string? RequestedByUsername { get; set; }
        public DateTime? RequestedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public decimal RequestedAmount { get; set; }        
        public decimal ApprovedAmount { get; set; }
        public string? ApprovedBy { get; set; }
        public int IsApproved { get; set; }
        public string? IsApprovedDisplay => IsApproved == 1 ? "Yes" : IsApproved == 2 ? "Disapproved" : "No";
        public IFormFile? Attachment { get; set; }
        public string? AttachmentFilename { get; set; }
        public string? pathAttachmentFileName => AttachmentFilename != null ? $"{Path.Combine(Path.Combine(Directory.GetCurrentDirectory(),Constants.OPENPATH), AttachmentFilename)}" : null;
        public string? PlayerName { get; set; }
        public decimal? ResultId { get; set; }
        public string? Remarks { get; set; }
    }
}