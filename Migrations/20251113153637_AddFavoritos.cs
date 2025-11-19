using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoritos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarcaFavorita",
                table: "Compradores");

            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompradorId = table.Column<int>(type: "int", nullable: false),
                    AnuncioId = table.Column<int>(type: "int", nullable: false),
                    DataAdicionado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favoritos_Anuncios_AnuncioId",
                        column: x => x.AnuncioId,
                        principalTable: "Anuncios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favoritos_Compradores_CompradorId",
                        column: x => x.CompradorId,
                        principalTable: "Compradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_AnuncioId",
                table: "Favoritos",
                column: "AnuncioId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_CompradorId",
                table: "Favoritos",
                column: "CompradorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favoritos");

            migrationBuilder.AddColumn<string>(
                name: "MarcaFavorita",
                table: "Compradores",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
