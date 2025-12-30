namespace AutoMarket.Models
{
    public class Anuncio
    {
        public int Id { get; set; }
        public int VendedorId { get; set; }
        public Vendedor Vendedor { get; set; } = null!;

        public int ModeloId { get; set; }
        public Modelo Modelo { get; set; } = null!;

        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public string? Estado { get; set; }
        public string? Defeito { get; set; }
        public string? Localizacao { get; set; }
        public string? Caixa { get; set; }
        public string? Combustivel { get; set; }
        public int? Quilometragem { get; set; }
        public decimal? Preco { get; set; }
        public int? Ano { get; set; }
        public string? Categoria { get; set; }

        public string Cor { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        // Navegação
        public ICollection<Imagem>? Imagens { get; set; }
        public ICollection<Reserva>? Reservas { get; set; }
        public ICollection<Compra>? Compras { get; set; }
        public ICollection<HistoricoAdmin>? Historicos { get; set; }
    }

}
