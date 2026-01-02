using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Migrations
{
    /// <inheritdoc />
    public partial class addnotificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MarcasFavoritas_Compradores_CompradorId",
                table: "MarcasFavoritas");

            migrationBuilder.RenameColumn(
                name: "CompradorId",
                table: "MarcasFavoritas",
                newName: "UtilizadorId");

            migrationBuilder.RenameIndex(
                name: "IX_MarcasFavoritas_CompradorId",
                table: "MarcasFavoritas",
                newName: "IX_MarcasFavoritas_UtilizadorId");

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lida = table.Column<bool>(type: "bit", nullable: false),
                    DataCriada = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UtilizadorId",
                table: "Notificacoes",
                column: "UtilizadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MarcasFavoritas_Utilizador_UtilizadorId",
                table: "MarcasFavoritas",
                column: "UtilizadorId",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MarcasFavoritas_Utilizador_UtilizadorId",
                table: "MarcasFavoritas");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.RenameColumn(
                name: "UtilizadorId",
                table: "MarcasFavoritas",
                newName: "CompradorId");

            migrationBuilder.RenameIndex(
                name: "IX_MarcasFavoritas_UtilizadorId",
                table: "MarcasFavoritas",
                newName: "IX_MarcasFavoritas_CompradorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MarcasFavoritas_Compradores_CompradorId",
                table: "MarcasFavoritas",
                column: "CompradorId",
                principalTable: "Compradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
