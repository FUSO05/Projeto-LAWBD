using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoMarket.Models
{
    [Table("Utilizador")]
    public class Utilizador
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100)]
        [Column("Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email inserido não é válido.")]
        [MaxLength(150)]
        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [MaxLength(50)]
        [Column("Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        [MaxLength(200)]
        [Column("Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo de utilizador é obrigatório.")]
        [MaxLength(20)]
        [Column("Tipo_user")]
        public string TipoUser { get; set; } = "Comprador"; // valor padrão

        [Column("Data_registo")]
        public DateTime DataRegisto { get; set; } = DateTime.Now;

        [Column("Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("EmailConfirmed")]
        public bool EmailConfirmed { get; set; } = false; // padrão: false até ativar

        [MaxLength(100)]
        [Column("ActivationToken")]
        public string? ActivationToken { get; set; }

        [Column("ActivationTokenExpiry")]
        public DateTime? ActivationTokenExpiry { get; set; }

        [MaxLength(100)]
        [Column("PasswordResetToken")]
        public string? PasswordResetToken { get; set; }

        [Column("PasswordResetTokenExpiry")]
        public DateTime? PasswordResetTokenExpiry { get; set; }

        public ICollection<Bloqueio>? Bloqueios { get; set; }

        [MaxLength(250)]
        [Column("FotoUrl")]
        public string? FotoUrl { get; set; } = "/img/avatar.png";

        [MaxLength(250)]
        [Column("Morada")]
        public string? Morada { get; set; }

        [MaxLength(9)]
        [Column("Contacto")]
        public string? Contacto { get; set; }

        [Column("PedidoVendedorPendente")]
        public bool PedidoVendedorPendente { get; set; } = false; 

        [Column("RejeitadoVendedor")] // Opcional, para rastrear rejeições
        public bool RejeitadoVendedor { get; set; } = false;

        public Vendedor? VendedorInfo { get; set; }
        public Administrador? AdministradorInfo { get; set; }
        public Comprador? CompradorInfo { get; set; }
    }
}
