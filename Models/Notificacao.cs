namespace AutoMarket.Models
{
    public class Notificacao
    {
        public int Id { get; set; }

        // FK para o utilizador que recebe a notificação
        public int UtilizadorId { get; set; }
        public Utilizador Utilizador { get; set; } = null!;

        public string Titulo { get; set; } = null!;
        public string Mensagem { get; set; } = null!;
        public bool Lida { get; set; } = false;

        public DateTime DataCriada { get; set; } = DateTime.Now;
    }

}