using AutoMarket.Models;

// EM Comprador.cs

public class Comprador
{
    // A PK (Id) será usada como FK para Utilizador
    public int Id { get; set; }
    public string? Morada { get; set; }
    public bool NotificacoesAtivas { get; set; } = true;
    public DateTime? DataValidacao { get; set; }
    public Utilizador Utilizador { get; set; } = null!; // Relação 1:1

    // Navegação
    public ICollection<Reserva>? Reservas { get; set; }
    public ICollection<Compra>? Compras { get; set; }
    public ICollection<Favorito>? Favoritos { get; set; }

}