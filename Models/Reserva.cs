namespace AutoMarket.Models
{
    public class Reserva
    {
        public int Id { get; set; }
        public int CompradorId { get; set; }
        public Comprador Comprador { get; set; } = null!;

        public int AnuncioId { get; set; }
        public Anuncio Anuncio { get; set; } = null!;

        public string? Estado { get; set; }
        public DateTime? PrazoExpiracao { get; set; }
        public DateTime DataHoraReserva { get; set; } = DateTime.Now;
    }

}
