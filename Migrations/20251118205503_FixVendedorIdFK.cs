using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Migrations
{
    public partial class FixVendedorIdFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Criar coluna temporária TempId não-nula
            migrationBuilder.AddColumn<int>(
                name: "TempId",
                table: "Vendedores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 2. Copiar valores do Id antigo para TempId
            migrationBuilder.Sql("UPDATE Vendedores SET TempId = Id");

            // 3. Dropar FKs que dependem da coluna Id
            migrationBuilder.DropForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios");

            // IMPORTANTE: Dropar a FK que liga Vendedores a Utilizador
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores");

            // 4. Dropar PK antiga
            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores");

            // 5. Remover a coluna Id antiga
            migrationBuilder.DropColumn(
                name: "Id",
                table: "Vendedores");

            // 6. Renomear TempId para Id
            migrationBuilder.RenameColumn(
                name: "TempId",
                table: "Vendedores",
                newName: "Id");

            // 7. Criar PK na nova coluna Id
            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores",
                column: "Id");

            // 8. Recriar a FK entre Vendedores e Utilizador
            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores",
                column: "Id",
                principalTable: "Utilizador",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade); // ou Restrict, dependendo da sua regra de negócio

            // 9. Recriar FK de Anuncios para Vendedores
            migrationBuilder.AddForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverter mudanças
            migrationBuilder.DropForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores");

            migrationBuilder.AddColumn<int>(
                name: "TempId",
                table: "Vendedores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Vendedores SET TempId = Id");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Vendedores");

            migrationBuilder.RenameColumn(
                name: "TempId",
                table: "Vendedores",
                newName: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores",
                column: "Id",
                principalTable: "Utilizador",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}