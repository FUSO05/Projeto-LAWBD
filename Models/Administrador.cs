using AutoMarket.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Administrador
{
    [Key]
    public int Id { get; set; }
    public string? Permissoes { get; set; }

    // FK explícita
    public int UtilizadorId { get; set; }

    [ForeignKey("UtilizadorId")]
    public Utilizador Utilizador { get; set; } = null!;

    public bool IsSuperAdmin { get; set; } 
    public ICollection<Vendedor>? VendedoresAprovados { get; set; }
    public ICollection<HistoricoAdmin>? Historicos { get; set; }
}
