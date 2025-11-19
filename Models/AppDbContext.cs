using AutoMarket.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Utilizador> Utilizadores { get; set; }
        public DbSet<Bloqueio> Bloqueios { get; set; }
        public DbSet<Administrador> Administradores { get; set; }
        public DbSet<Comprador> Compradores { get; set; }
        public DbSet<Vendedor> Vendedores { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Modelo> Modelos { get; set; }
        public DbSet<Anuncio> Anuncios { get; set; }
        public DbSet<Imagem> Imagens { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<HistoricoAdmin> HistoricosAdmin { get; set; }
        public DbSet<Favorito> Favoritos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Modelo -> Marca
            modelBuilder.Entity<Modelo>()
                .HasOne(m => m.Marca)
                .WithMany(ma => ma.Modelos)
                .HasForeignKey(m => m.MarcaId)
                .OnDelete(DeleteBehavior.Cascade); // seguro, não gera conflito

            // Anuncio -> Modelo
            modelBuilder.Entity<Anuncio>()
                .HasOne(a => a.Modelo)
                .WithMany(m => m.Anuncios)
                .HasForeignKey(a => a.ModeloId)
                .OnDelete(DeleteBehavior.Restrict); // evita cascata múltipla

            // Anuncio -> Vendedor
            modelBuilder.Entity<Anuncio>()
                .HasOne(a => a.Vendedor)
                .WithMany(v => v.Anuncios)
                .HasForeignKey(a => a.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Anuncio -> Imagens
            modelBuilder.Entity<Imagem>()
                .HasOne(i => i.Anuncio)
                .WithMany(a => a.Imagens)
                .HasForeignKey(i => i.AnuncioId)
                .OnDelete(DeleteBehavior.Cascade); // seguro, não conflita

            // Vendedor -> Administrador (AprovadoPor)
            modelBuilder.Entity<Vendedor>()
                .HasOne(v => v.AprovadoPor)
                .WithMany()
                .HasForeignKey(v => v.AprovadoPorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Comprador -> Utilizador
            modelBuilder.Entity<Comprador>()
                .HasOne(c => c.Utilizador)
                .WithOne(u => u.CompradorInfo) // Agora usa a propriedade de navegação
                .HasForeignKey<Comprador>(c => c.Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Compra -> Comprador e Anuncio
            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Comprador)
                .WithMany(co => co.Compras)
                .HasForeignKey(c => c.CompradorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Anuncio)
                .WithMany(a => a.Compras)
                .HasForeignKey(c => c.AnuncioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reserva -> Comprador e Anuncio
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Comprador)
                .WithMany(c => c.Reservas)
                .HasForeignKey(r => r.CompradorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Anuncio)
                .WithMany(a => a.Reservas)
                .HasForeignKey(r => r.AnuncioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bloqueio -> Utilizador
            modelBuilder.Entity<Bloqueio>()
                .HasOne(b => b.Utilizador)
                .WithMany(u => u.Bloqueios)
                .HasForeignKey(b => b.UtilizadorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Administrador -> Utilizador
            modelBuilder.Entity<Administrador>()
                .HasOne(a => a.Utilizador)
                .WithOne(u => u.AdministradorInfo) // Agora usa a propriedade de navegação
                .HasForeignKey<Administrador>(a => a.Id)
                .OnDelete(DeleteBehavior.Restrict);

            // HistoricoAdmin -> Admin, UtilizadorAlvo, AnuncioAlvo
            modelBuilder.Entity<HistoricoAdmin>()
                .HasOne(h => h.Admin)
                .WithMany(a => a.Historicos)
                .HasForeignKey(h => h.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistoricoAdmin>()
                .HasOne(h => h.UtilizadorAlvo)
                .WithMany()
                .HasForeignKey(h => h.UtilizadorAlvoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HistoricoAdmin>()
                .HasOne(h => h.AnuncioAlvo)
                .WithMany(a => a.Historicos)
                .HasForeignKey(h => h.AnuncioAlvoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ajuste os decimais para SQL Server
            modelBuilder.Entity<Anuncio>()
                .Property(a => a.Preco)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Compra>()
                .Property(c => c.Valor)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Vendedor>()
                .HasOne(v => v.Utilizador)
                .WithOne(u => u.VendedorInfo)
                .HasForeignKey<Vendedor>(v => v.Id)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}

