using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Migrations
{
    public partial class CorrecaoVendedor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign keys que dependem de Id
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios");

            // 2. Drop primary key da tabela Vendedores
            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores");

            // 3. Adiciona coluna temporária sem IDENTITY
            migrationBuilder.AddColumn<int>(
                name: "TempId",
                table: "Vendedores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 4. Copia os valores da coluna antiga
            migrationBuilder.Sql("UPDATE Vendedores SET TempId = Id");

            // 5. Remove a coluna antiga
            migrationBuilder.DropColumn(
                name: "Id",
                table: "Vendedores");

            // 6. Renomeia a coluna temporária para Id
            migrationBuilder.RenameColumn(
                name: "TempId",
                table: "Vendedores",
                newName: "Id");

            // 7. Recria primary key
            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores",
                column: "Id");

            // 8. Recria foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores",
                column: "Id",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 9. Adiciona a nova coluna UtilizadorId como nullable
            migrationBuilder.AddColumn<int>(
                name: "UtilizadorId",
                table: "Vendedores",
                type: "int",
                nullable: true);

            // 10. Preenche os valores corretos (aqui usamos Id como exemplo)
            migrationBuilder.Sql("UPDATE Vendedores SET UtilizadorId = Id");

            // 11. Altera a coluna para não-nullable
            migrationBuilder.AlterColumn<int>(
                name: "UtilizadorId",
                table: "Vendedores",
                type: "int",
                nullable: false);

            // 12. Cria índice único agora que não há duplicados
            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_UtilizadorId",
                table: "Vendedores",
                column: "UtilizadorId",
                unique: true);

            // 13. Cria foreign key para UtilizadorId
            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Utilizador_UtilizadorId",
                table: "Vendedores",
                column: "UtilizadorId",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FKs e índice de UtilizadorId
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Utilizador_UtilizadorId",
                table: "Vendedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios");

            migrationBuilder.DropIndex(
                name: "IX_Vendedores_UtilizadorId",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "UtilizadorId",
                table: "Vendedores");

            // Reverte alteração do Id
            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores");

            migrationBuilder.AddColumn<int>(
                name: "OldId",
                table: "Vendedores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Vendedores SET OldId = Id");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Vendedores");

            migrationBuilder.RenameColumn(
                name: "OldId",
                table: "Vendedores",
                newName: "Id");

            // Recria PK
            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores",
                column: "Id");

            // Recria FKs originais
            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores",
                column: "Id",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
