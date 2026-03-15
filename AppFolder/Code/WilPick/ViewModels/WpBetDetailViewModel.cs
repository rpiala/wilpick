using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{
    public class WpBetDetailViewModel
    {       
        public decimal BetDetailId { get; set; }        
        public decimal BetId { get; set; }        
        public decimal UserId { get; set; }        
        public DateTime? DateCreated { get; set; }
        // Combination: exactly 4 unique uppercase letters, validated server-side
        [Required(ErrorMessage = "Combination is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Combination must be 4 characters.")]
        [RegularExpression(@"^(?!.*(.).*\1)[A-Z]{4}$", ErrorMessage = "Combination must be 4 unique uppercase letters with no repeats.")]
        public string? Combination { get; set; }

        public string? PrevCombination { get; set; }
        public string? BaseCombination { get; set; }
        public string? PrevBaseCombination { get; set; }
        public decimal? BetAmount { get; set; }
        public decimal? PrevBetAmount { get; set; }            
        public int? FirstDrawSelected { get; set; }
        public int? PrevFirstDrawSelected { get; set; }
        public int? SecondDrawSelected { get; set; }
        public int? PrevSecondDrawSelected { get; set; }
        public int? ThirdDrawSelected { get; set; }
        public int? PrevThirdDrawSelected { get; set; }
    }
}
