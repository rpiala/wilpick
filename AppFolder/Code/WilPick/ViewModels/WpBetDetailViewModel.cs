using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{
    public class WpBetDetailViewModel
    {       
        public decimal BetDetailId { get; set; }
        public string? BetDetailIdEnc { get; set; }
        public decimal BetId { get; set; }        
        public decimal UserId { get; set; }
        public DateTime? DrawDate { get; set; }
        public DateTime? DateCreated { get; set; }
        // Combination: exactly 4 unique uppercase letters, validated server-side
        [Required(ErrorMessage = "Combination is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Combination must be 4 characters.")]
        [RegularExpression(@"^(?!.*(.).*\1)[A-Z]{4}$", ErrorMessage = "Combination must be 4 unique uppercase letters with no repeats.")]
        [Display(Name = "Combi")]
        public string? Combination { get; set; }

        public string? PrevCombination { get; set; }
        public string? BaseCombination { get; set; }
        public string? PrevBaseCombination { get; set; }
        [Display(Name = "Straight")]
        //[Remote(action: "VerifyBetAmount", controller: "Transactions", HttpMethod = "GET", AdditionalFields = nameof(Combination), ErrorMessage = "Available bet amount {0}.")]
        public int? BetAmount { get; set; }
        public int? PrevBetAmount { get; set; }            
        public int? FirstDrawSelected { get; set; }
        public int? PrevFirstDrawSelected { get; set; }
        public int? SecondDrawSelected { get; set; }
        public int? PrevSecondDrawSelected { get; set; }
        public int? ThirdDrawSelected { get; set; }
        public int? PrevThirdDrawSelected { get; set; }
        [Display(Name = "Draw")]
        public string? DrawDisplay { get; set; }
        public string? betType { get; set; }
        [Display(Name = "Total")]
        public int? TotalBet {  get; set; }
        [Display(Name = "No.")]
        public int? RowNum { get; set; }
        public decimal? ComputedAmount { get; set; }
        public string? PlayerName { get; set; }
        public int? IncludeRamble { get; set; }
        [Display(Name = "Include Ramble")]
        public string? RambleDisplay => IncludeRamble == 1 ? "Yes" : "No";
        public decimal? LoadBalance { get; set; }
        [Display(Name = "Ramble")]      
        public int? RambleBetAmount { get; set; }
        public decimal? WinningPrize { get; set; }
        public decimal? RambleWinningPrize { get; set; }
    }
}
