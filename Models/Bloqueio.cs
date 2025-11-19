using AutoMarket.Models;

public class Bloqueio
{
    public int Id { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime DataBloqueio { get; set; } = DateTime.Now;

    // FK
    public int UtilizadorId { get; set; }
    public Utilizador Utilizador { get; set; } = null!;
}
