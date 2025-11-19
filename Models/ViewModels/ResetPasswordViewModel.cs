using System.ComponentModel.DataAnnotations;

namespace AutoMarket.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string Token { get; set; } = string.Empty;
        [Required(ErrorMessage = "Introduza a nova password.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a nova password.")]
        [Compare("NewPassword", ErrorMessage = "As passwords não coincidem.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

}
