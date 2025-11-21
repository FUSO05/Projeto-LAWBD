using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using AutoMarket.Models;

namespace AutoMarket.ViewModels
{
    public class CreateAnuncioViewModel
    {
        [Required(ErrorMessage = "A marca é obrigatória")]
        [Display(Name = "Marca")]
        public int MarcaId { get; set; }

        [Required(ErrorMessage = "O modelo é obrigatório")]
        [Display(Name = "Modelo")]
        public int ModeloId { get; set; }

        [Required(ErrorMessage = "O título é obrigatório")]
        [MaxLength(200, ErrorMessage = "O título não pode ter mais de 200 caracteres")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0, double.MaxValue, ErrorMessage = "O preço deve ser maior que 0")]
        [Display(Name = "Preço")]
        public decimal Preco { get; set; }

        [Required(ErrorMessage = "O ano é obrigatório")]
        [Range(1900, 2100, ErrorMessage = "Ano inválido")]
        [Display(Name = "Ano")]
        public int Ano { get; set; }

        [Required(ErrorMessage = "A caixa de velocidades é obrigatória")]
        [Display(Name = "Caixa de Velocidades")]
        public string Caixa { get; set; } = string.Empty;

        [Required(ErrorMessage = "A quilometragem é obrigatória")]
        [Range(0, int.MaxValue, ErrorMessage = "A quilometragem deve ser maior ou igual a 0")]
        [Display(Name = "Quilometragem")]
        public int Quilometragem { get; set; }

        [Required(ErrorMessage = "O combustível é obrigatório")]
        [Display(Name = "Combustível")]
        public string Combustivel { get; set; } = string.Empty;

        [Required(ErrorMessage = "A categoria é obrigatória")]
        [Display(Name = "Categoria")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "A cor é obrigatória")]
        [MaxLength(50, ErrorMessage = "A cor não pode ter mais de 50 caracteres")]
        [Display(Name = "Cor")]
        public string Cor { get; set; } = string.Empty;

        [Required(ErrorMessage = "O estado é obrigatório")]
        [Display(Name = "Estado do Veículo")]
        public string Estado { get; set; } = "Usado";

        [Display(Name = "Defeito (se houver)")]
        [MaxLength(500, ErrorMessage = "O defeito não pode ter mais de 500 caracteres")]
        public string? Defeito { get; set; }

        [Required(ErrorMessage = "A localização é obrigatória")]
        [MaxLength(200, ErrorMessage = "A localização não pode ter mais de 200 caracteres")]
        [Display(Name = "Localização")]
        public string Localizacao { get; set; } = string.Empty;
        //public double? Latitude { get; set; }
        //public double? Longitude { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [MinLength(50, ErrorMessage = "A descrição deve ter pelo menos 50 caracteres")]
        [MaxLength(2000, ErrorMessage = "A descrição não pode ter mais de 2000 caracteres")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [Display(Name = "Imagens")]
        public List<IFormFile>? Imagens { get; set; }

        // Lista para popular o dropdown de marcas
        public List<Marca>? Marcas { get; set; }
    }
}