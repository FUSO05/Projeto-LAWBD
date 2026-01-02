namespace AutoMarket.Models.ViewModels
{
    public class NotificacoesViewModel
    {
        public List<Notificacao> Notificacoes { get; set; } = new();
        public UtilizadorViewModel Utilizador { get; set; } = null!;
    }
}
