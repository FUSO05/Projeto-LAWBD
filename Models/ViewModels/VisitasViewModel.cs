namespace AutoMarket.Models.ViewModels
{
    public class VisitasViewModel
    {
        public Anuncio Anuncio { get; set; }
        public List<Reserva> Visitas { get; set; }

        // ✅ Mantenha esta para compatibilidade com BookVisit
        public Utilizador Utilizador { get; set; }

        // ✅ Adicione esta para usar nas partial views
        public UtilizadorViewModel UtilizadorViewModel { get; set; }
    }
}