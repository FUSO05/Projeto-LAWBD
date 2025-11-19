namespace AutoMarket.Models
{
    public class Modelo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        // FK
        public int MarcaId { get; set; }
        public Marca Marca { get; set; } = null!;

        // Navegação
        public ICollection<Anuncio>? Anuncios { get; set; }
    }

}
