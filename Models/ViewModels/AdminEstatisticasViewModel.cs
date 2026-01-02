namespace AutoMarket.Models.ViewModels
{
    public class AdminEstatisticasViewModel
    {
        // Utilizadores
        public int TotalCompradores { get; set; }
        public int TotalVendedores { get; set; }

        // Anúncios
        public int TotalAnunciosAtivos { get; set; }

        // Vendas / Reservas
        public int VendasHoje { get; set; }
        public int VendasAnoPassado { get; set; }
        public int VendasEsteAno { get; set; }

        // Vendas mensais (Janeiro a Dezembro)
        public List<int> VendasMensaisAnoPassado { get; set; } = new();
        public List<int> VendasMensaisEsteAno { get; set; } = new();

        // Tops
        public List<TopItemViewModel> TopMarcas { get; set; } = new();
        public List<TopItemViewModel> TopModelos { get; set; } = new();
    }

    public class TopItemViewModel
    {
        public string Nome { get; set; }
        public int Total { get; set; }
    }
}