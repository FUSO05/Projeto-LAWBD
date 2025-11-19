namespace AutoMarket.Models
{
    public class Imagem
    {
        public int Id { get; set; }
        public string UrlImagem { get; set; } = string.Empty;
        public int? Ordem { get; set; }

        // FK
        public int AnuncioId { get; set; }
        public Anuncio Anuncio { get; set; } = null!;
    }

}
