using System.ComponentModel.DataAnnotations;

namespace AutoMarket.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Insira um email válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Lembrar-me")]
        public bool RememberMe { get; set; } = false;
    }
}
