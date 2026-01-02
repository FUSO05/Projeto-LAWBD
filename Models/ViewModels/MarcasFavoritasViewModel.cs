namespace AutoMarket.Models.ViewModels
{
    public class MarcasFavoritasViewModel
    {
        public List<Marca> TodasMarcas { get; set; } = new();
        public List<MarcaFavorita> MarcasFavoritas { get; set; } = new();
    }

}
