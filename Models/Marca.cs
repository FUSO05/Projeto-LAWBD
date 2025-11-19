namespace AutoMarket.Models
{
    public class Marca
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        // Navegação
        public ICollection<Modelo>? Modelos { get; set; }
    }

}
