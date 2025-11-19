namespace AutoMarket.Models
{
    public class Favorito
    {
        public int Id { get; set; }

        // FK
        public int CompradorId { get; set; }
        public Comprador Comprador { get; set; } = null!;

        public int AnuncioId { get; set; }
        public Anuncio Anuncio { get; set; } = null!;

        public DateTime DataAdicionado { get; set; } = DateTime.Now;
    }
}
