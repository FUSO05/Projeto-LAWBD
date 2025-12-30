namespace AutoMarket.Models.ViewModels
{
    public class ReagendarVisitaViewModel
    {
        public int ReservaOriginalId { get; set; }
        public int AnuncioId { get; set; }
        public DateTime DataHoraAtual { get; set; }
        public Anuncio Anuncio { get; set; }
    }

}
