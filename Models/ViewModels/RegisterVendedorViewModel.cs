using System.ComponentModel.DataAnnotations;

namespace AutoMarket.Models.ViewModels
{
    public class RegisterVendedorViewModel
    {
        [Required(ErrorMessage = "O tipo de vendedor é obrigatório.")]
        public string TipoVendedor { get; set; } = string.Empty;

        [Required(ErrorMessage = "O NIF é obrigatório.")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "O NIF deve ter 9 dígitos.")]
        public string NIF { get; set; } = string.Empty;
    }
}
