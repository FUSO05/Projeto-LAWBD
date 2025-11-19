namespace AutoMarket.Models
{
    public class Compra
    {
        public int Id { get; set; }
        public int CompradorId { get; set; }
        public Comprador Comprador { get; set; } = null!;

        public int AnuncioId { get; set; }
        public Anuncio Anuncio { get; set; } = null!;

        public DateTime DataCompra { get; set; } = DateTime.Now;
        public decimal? Valor { get; set; }
        public string? Estado { get; set; }
    }

}
