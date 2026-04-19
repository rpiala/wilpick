using System;
using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{
    public class WpAppUserViewModel
    {
        // SQL: userId numeric identity
        public decimal UserId { get; set; }
        public string? UserIdEnc { get; set; }

        // SQL: aspNetUserID nvarchar(255) not null
        [Required]
        [StringLength(255)]
        public string AspNetUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Display(Name = "Agent Code")]
        public string AgentCode { get; set; } = string.Empty;

        public DateTime? DateRegistered { get; set; }

        // SQL: userName varchar(30) null
        [StringLength(30)]
        [Display(Name = "Username")]
        public string? UserName { get; set; }

        // SQL: password varchar(150) null
        [StringLength(150)]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        // SQL: email varchar(100) null
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        // SQL: firstName varchar(50) null
        [StringLength(50)]
        [Display(Name = "Name")]
        public string? FirstName { get; set; }

        // SQL: lastName varchar(50) null
        [StringLength(50)]
        public string? LastName { get; set; }

        // SQL: middleName varchar(50) null
        [StringLength(50)]
        public string? MiddleName { get; set; }

        // SQL: betTicketPrice decimal(10,2) null
        [DataType(DataType.Currency)]
        public int? BetTicketPrice { get; set; }

        // SQL: winningPrize decimal(10,2) null
        [DataType(DataType.Currency)]
        public decimal? WinningPrize { get; set; }
        public decimal? RambleWinningPrize { get; set; }

        public string AccessRole { get; set; } = string.Empty;
        [Display(Name = "Bet Type")]
        public string betType { get; set; } = string.Empty;
        public decimal? RemainingLoad { get; set; }
        public string? UserType { get; set; }
        [Display(Name = "Handler")]
        public string? AgentName { get; set; }
        [Display(Name = "Mobile")]
        public string? MobileNumber {  get; set; }
        public int? SwCwn_id { get; set; }
        public int? SwCw_id { get; set; }
        public int? SwCo_id { get; set; }
        public int? SwWp_id { get; set; }
        [Display(Name = "3D Winning Prize")]
        public decimal? SwPrize { get; set; }
        [Display(Name = "3D Commission Pct")]
        public decimal? SwCommission { get; set; }
        public decimal? SwCoPrize { get; set; }
        public decimal? SwCoCommission { get; set; }

    }
}