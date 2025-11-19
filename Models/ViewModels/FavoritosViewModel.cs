namespace AutoMarket.Models.ViewModels
{
    public class FavoritosViewModel
    {
        public List<Anuncio> Favoritos { get; set; } = new();
        public UtilizadorViewModel Utilizador { get; set; } = null!;
    }
}
