namespace AutoMarket.Models
{
    public class MarcaFavorita
    {
        public int Id { get; set; }

        public int UtilizadorId { get; set; }
        public Utilizador Utilizador { get; set; } = null!;

        public int MarcaId { get; set; }
        public Marca Marca { get; set; } = null!;
    }
}


