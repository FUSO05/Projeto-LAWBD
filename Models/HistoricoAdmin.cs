namespace AutoMarket.Models
{
    public class HistoricoAdmin
    {
        public int Id { get; set; }

        public int AdminId { get; set; }
        public Administrador Admin { get; set; } = null!;

        public int? UtilizadorAlvoId { get; set; }
        public Utilizador? UtilizadorAlvo { get; set; }

        public int? AnuncioAlvoId { get; set; }
        public Anuncio? AnuncioAlvo { get; set; }

        public string? Acao { get; set; }
        public DateTime DataAcao { get; set; } = DateTime.Now;
    }

}
