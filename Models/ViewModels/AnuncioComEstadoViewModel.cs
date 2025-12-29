using System.ComponentModel;

namespace AutoMarket.Models.ViewModels
{
    public class AnuncioComEstadoViewModel
    {
        public Anuncio Anuncio { get; set; }
        public string EstadoCarro { get; set; } // Disponível | Reservado | Vendido
        public int TotalReservas { get; set; }
    }

}
