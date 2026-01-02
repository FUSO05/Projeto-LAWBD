using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Migrations
{
    /// <inheritdoc />
    public partial class addhistoricoadmins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoricosAdmin_Administradores_AdminId",
                table: "HistoricosAdmin");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricosAdmin_Anuncios_AnuncioAlvoId",
                table: "HistoricosAdmin");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricosAdmin_Utilizador_UtilizadorAlvoId",
                table: "HistoricosAdmin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistoricosAdmin",
                table: "HistoricosAdmin");

            migrationBuilder.RenameTable(
                name: "HistoricosAdmin",
                newName: "HistoricoAdmins");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricosAdmin_UtilizadorAlvoId",
                table: "HistoricoAdmins",
                newName: "IX_HistoricoAdmins_UtilizadorAlvoId");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricosAdmin_AnuncioAlvoId",
                table: "HistoricoAdmins",
                newName: "IX_HistoricoAdmins_AnuncioAlvoId");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricosAdmin_AdminId",
                table: "HistoricoAdmins",
                newName: "IX_HistoricoAdmins_AdminId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistoricoAdmins",
                table: "HistoricoAdmins",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricoAdmins_Administradores_AdminId",
                table: "HistoricoAdmins",
                column: "AdminId",
                principalTable: "Administradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricoAdmins_Anuncios_AnuncioAlvoId",
                table: "HistoricoAdmins",
                column: "AnuncioAlvoId",
                principalTable: "Anuncios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricoAdmins_Utilizador_UtilizadorAlvoId",
                table: "HistoricoAdmins",
                column: "UtilizadorAlvoId",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoricoAdmins_Administradores_AdminId",
                table: "HistoricoAdmins");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricoAdmins_Anuncios_AnuncioAlvoId",
                table: "HistoricoAdmins");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricoAdmins_Utilizador_UtilizadorAlvoId",
                table: "HistoricoAdmins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistoricoAdmins",
                table: "HistoricoAdmins");

            migrationBuilder.RenameTable(
                name: "HistoricoAdmins",
                newName: "HistoricosAdmin");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricoAdmins_UtilizadorAlvoId",
                table: "HistoricosAdmin",
                newName: "IX_HistoricosAdmin_UtilizadorAlvoId");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricoAdmins_AnuncioAlvoId",
                table: "HistoricosAdmin",
                newName: "IX_HistoricosAdmin_AnuncioAlvoId");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricoAdmins_AdminId",
                table: "HistoricosAdmin",
                newName: "IX_HistoricosAdmin_AdminId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistoricosAdmin",
                table: "HistoricosAdmin",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosAdmin_Administradores_AdminId",
                table: "HistoricosAdmin",
                column: "AdminId",
                principalTable: "Administradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosAdmin_Anuncios_AnuncioAlvoId",
                table: "HistoricosAdmin",
                column: "AnuncioAlvoId",
                principalTable: "Anuncios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosAdmin_Utilizador_UtilizadorAlvoId",
                table: "HistoricosAdmin",
                column: "UtilizadorAlvoId",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
