using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{
    public class SmSettingsViewModel
    {
        // SQL: varName varchar(20) not null
        [Required(ErrorMessage = "Variable name is required.")]
        [StringLength(20, ErrorMessage = "Variable name must be at most 20 characters.")]
        public string VarName { get; set; } = string.Empty;

        // SQL: varValue text null
        public string? VarValue { get; set; }

        // SQL: description varchar(100) null
        [StringLength(100, ErrorMessage = "Description must be at most 100 characters.")]
        public string? Description { get; set; }
    }
}