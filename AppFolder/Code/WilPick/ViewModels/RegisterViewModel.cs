using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Agent Code is required.")]
        [Remote(action: "VerifyAgentCode", controller: "Account", HttpMethod = "GET", ErrorMessage = "Agent code not found.")]
        public string AgentCode { get; set; }

        [Required(ErrorMessage ="Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(40, MinimumLength = 4, ErrorMessage = "The {0} must be at {2} and at max {1} characters long.")]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Password does not match.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage = "Mobile number is required.")]
        [Display(Name = "Mobile Number")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Invalid mobile number.")]
        public string? MobileNumber { get; set; }
    }
}
