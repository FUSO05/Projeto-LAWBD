using AutoMarket.Models;
using System.ComponentModel.DataAnnotations.Schema; 
using System.ComponentModel.DataAnnotations;

public class Vendedor
{
    [Key]
    public int Id { get; set; }
    public string? TipoVendedor { get; set; }
    public string? Morada { get; set; }
    public string? Contacto { get; set; }
    public string? NIF { get; set; }
    public string? DadosFaturacao { get; set; }

    // FK
    public int? AprovadoPorId { get; set; }
    public Administrador? AprovadoPor { get; set; }

    public Utilizador Utilizador { get; set; } = null!; 

    // Navegação
    public ICollection<Anuncio>? Anuncios { get; set; }
}
