using System.ComponentModel.DataAnnotations;

namespace AutoMarket.Models.ViewModels
{
    public class EditAnuncioViewModel
    {
        public int Id { get; set; }

        [Required] public string Titulo { get; set; }
        [Required] public string Descricao { get; set; }
        [Required] public decimal Preco { get; set; }
        [Required] public int Ano { get; set; }
        [Required] public string Caixa { get; set; }
        [Required] public string Combustivel { get; set; }
        [Required] public string Categoria { get; set; }
        [Required] public string Cor { get; set; }
        public string? Defeito { get; set; }
        [Required] public string Localizacao { get; set; }
    }

}
