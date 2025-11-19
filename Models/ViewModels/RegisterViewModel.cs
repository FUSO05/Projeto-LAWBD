using System.ComponentModel.DataAnnotations;

namespace AutoMarket.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Insira um email válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        [MinLength(8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "O contacto é obrigatório.")]
        [Phone(ErrorMessage = "Insira um contacto válido.")]
        public string Contacto { get; set; } = string.Empty;

        [Display(Name = "Morada")]
        public string? Morada { get; set; }

        [Display(Name = "Tipo de Utilizador")]
        [Required]
        public string Tipo { get; set; } = "Comprador"; // valor padrão do formulário
    }
}
