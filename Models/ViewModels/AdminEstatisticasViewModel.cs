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
        public int VendasMes { get; set; }
        public int VendasAno { get; set; }

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

