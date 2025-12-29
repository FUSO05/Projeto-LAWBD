using System.ComponentModel.DataAnnotations;

public class CheckoutCarViewModel
{
    public int ReservaId { get; set; }

    public string Veiculo { get; set; }

    public decimal Preco { get; set; }

    [Required]
    [Display(Name = "Titular do Cartão")]
    public string NomeTitular { get; set; }

    [Required]
    [CreditCard]
    [Display(Name = "Número do Cartão")]
    public string NumeroCartao { get; set; }

    [Required]
    [Display(Name = "Validade (MM/AA)")]
    public string Validade { get; set; }

    [Required]
    [StringLength(4, MinimumLength = 3)]
    public string CVV { get; set; }
}
