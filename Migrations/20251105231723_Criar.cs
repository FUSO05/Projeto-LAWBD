using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Migrations
{
    /// <inheritdoc />
    public partial class Criar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilizador",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo_user = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Data_registo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ActivationToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActivationTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilizador", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Modelos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarcaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modelos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modelos_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Administradores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Permissoes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administradores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Administradores_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bloqueios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataBloqueio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloqueios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bloqueios_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Compradores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Morada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarcaFavorita = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotificacoesAtivas = table.Column<bool>(type: "bit", nullable: false),
                    DataValidacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compradores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compradores_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoVendedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Morada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contacto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NIF = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DadosFaturacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AprovadoPorId = table.Column<int>(type: "int", nullable: true),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false),
                    AdministradorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendedores_Administradores_AdministradorId",
                        column: x => x.AdministradorId,
                        principalTable: "Administradores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vendedores_Administradores_AprovadoPorId",
                        column: x => x.AprovadoPorId,
                        principalTable: "Administradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vendedores_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anuncios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendedorId = table.Column<int>(type: "int", nullable: false),
                    ModeloId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Defeito = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Localizacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Caixa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Combustivel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quilometragem = table.Column<int>(type: "int", nullable: true),
                    Preco = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Ano = table.Column<int>(type: "int", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cor = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anuncios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anuncios_Modelos_ModeloId",
                        column: x => x.ModeloId,
                        principalTable: "Modelos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anuncios_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompradorId = table.Column<int>(type: "int", nullable: false),
                    AnuncioId = table.Column<int>(type: "int", nullable: false),
                    DataCompra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Anuncios_AnuncioId",
                        column: x => x.AnuncioId,
                        principalTable: "Anuncios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compras_Compradores_CompradorId",
                        column: x => x.CompradorId,
                        principalTable: "Compradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricosAdmin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorAlvoId = table.Column<int>(type: "int", nullable: true),
                    AnuncioAlvoId = table.Column<int>(type: "int", nullable: true),
                    Acao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataAcao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosAdmin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosAdmin_Administradores_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Administradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoricosAdmin_Anuncios_AnuncioAlvoId",
                        column: x => x.AnuncioAlvoId,
                        principalTable: "Anuncios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HistoricosAdmin_Utilizador_UtilizadorAlvoId",
                        column: x => x.UtilizadorAlvoId,
                        principalTable: "Utilizador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Imagens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UrlImagem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: true),
                    AnuncioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Imagens_Anuncios_AnuncioId",
                        column: x => x.AnuncioId,
                        principalTable: "Anuncios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompradorId = table.Column<int>(type: "int", nullable: false),
                    AnuncioId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrazoExpiracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataHoraReserva = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Anuncios_AnuncioId",
                        column: x => x.AnuncioId,
                        principalTable: "Anuncios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Compradores_CompradorId",
                        column: x => x.CompradorId,
                        principalTable: "Compradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Administradores_UtilizadorId",
                table: "Administradores",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Anuncios_ModeloId",
                table: "Anuncios",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_Anuncios_VendedorId",
                table: "Anuncios",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueios_UtilizadorId",
                table: "Bloqueios",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compradores_UtilizadorId",
                table: "Compradores",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_AnuncioId",
                table: "Compras",
                column: "AnuncioId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_CompradorId",
                table: "Compras",
                column: "CompradorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosAdmin_AdminId",
                table: "HistoricosAdmin",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosAdmin_AnuncioAlvoId",
                table: "HistoricosAdmin",
                column: "AnuncioAlvoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosAdmin_UtilizadorAlvoId",
                table: "HistoricosAdmin",
                column: "UtilizadorAlvoId");

            migrationBuilder.CreateIndex(
                name: "IX_Imagens_AnuncioId",
                table: "Imagens",
                column: "AnuncioId");

            migrationBuilder.CreateIndex(
                name: "IX_Modelos_MarcaId",
                table: "Modelos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_AnuncioId",
                table: "Reservas",
                column: "AnuncioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_CompradorId",
                table: "Reservas",
                column: "CompradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_AdministradorId",
                table: "Vendedores",
                column: "AdministradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_AprovadoPorId",
                table: "Vendedores",
                column: "AprovadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_UtilizadorId",
                table: "Vendedores",
                column: "UtilizadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bloqueios");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropTable(
                name: "HistoricosAdmin");

            migrationBuilder.DropTable(
                name: "Imagens");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Anuncios");

            migrationBuilder.DropTable(
                name: "Compradores");

            migrationBuilder.DropTable(
                name: "Modelos");

            migrationBuilder.DropTable(
                name: "Vendedores");

            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.DropTable(
                name: "Administradores");

            migrationBuilder.DropTable(
                name: "Utilizador");
        }
    }
}
