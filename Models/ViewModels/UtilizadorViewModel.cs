using System.ComponentModel.DataAnnotations.Schema;

namespace AutoMarket.Models.ViewModels
{
    public class UtilizadorViewModel
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Morada { get; set; }
        public string Contacto { get; set; }
        public string FotolUrl { get; set; }
        public string Password { get; set; }
        public bool PedidoVendedorPendente { get; set; } = false;

        public string TipoUser { get; set; }
    }

}
