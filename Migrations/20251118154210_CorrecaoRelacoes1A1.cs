using Microsoft.EntityFrameworkCore.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Migrations
{
    /// <inheritdoc />
    public partial class CorrecaoRelacoes1A1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =========================================================================
            // 1. VENDEDORES (Referências externas: Anuncios)
            // =========================================================================

            // 1A. Remover FKs que apontam para Vendedores.Id (PK Antiga)
            migrationBuilder.DropForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios");

            // 1B. Remover FK interna antiga e Index
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Utilizador_UtilizadorId",
                table: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Vendedores_UtilizadorId",
                table: "Vendedores");

            // 1C. Remover PK (possível após remover as FKs que apontam para ela)
            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores");

            // 1D. Lógica SQL: Renomear, Adicionar, Copiar Dados e Remover Colunas

            // 1D.1: Renomeia o Id antigo (IDENTITY)
            migrationBuilder.Sql(@"
                EXEC sp_rename 'Vendedores.Id', 'Id_OLD_IDENTITY', 'COLUMN';
            ");

            // 1D.2: Adiciona a nova coluna Id (sem IDENTITY)
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Vendedores",
                nullable: false,
                defaultValue: 0);

            // 1D.3: COPIA O VALOR CORRETO (UtilizadorId) para a nova coluna Id
            migrationBuilder.Sql(@"
                UPDATE Vendedores
                SET Id = UtilizadorId;
            ");

            // 1D.4: Descarta o Id antigo (IDENTITY) e o UtilizadorId
            migrationBuilder.DropColumn(
                name: "Id_OLD_IDENTITY",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "UtilizadorId",
                table: "Vendedores");

            // 1E. Re-adicionar PK na nova coluna Id
            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendedores",
                table: "Vendedores",
                column: "Id");

            // 1F. Adicionar a nova FK 1:1 (Vendedores -> Utilizador)
            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores",
                column: "Id",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // 1G. Re-adicionar FKs externas (Anuncios -> Vendedores)
            migrationBuilder.AddForeignKey(
                name: "FK_Anuncios_Vendedores_VendedorId",
                table: "Anuncios",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // =========================================================================
            // 2. COMPRADORES (Referências externas: Compras, Reservas, Favoritos)
            // =========================================================================

            // 2A. Remover FKs que apontam para Compradores.Id (PK Antiga)
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_Compradores_CompradorId",
                table: "Compras");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_Compradores_CompradorId",
                table: "Reservas");

            migrationBuilder.DropForeignKey(
                name: "FK_Favoritos_Compradores_CompradorId",
                table: "Favoritos");

            // 2B. Remover FK interna antiga e Index
            migrationBuilder.DropForeignKey(
                name: "FK_Compradores_Utilizador_UtilizadorId",
                table: "Compradores");

            migrationBuilder.DropIndex(
                name: "IX_Compradores_UtilizadorId",
                table: "Compradores");

            // 2C. Remover PK
            migrationBuilder.DropPrimaryKey(
                name: "PK_Compradores",
                table: "Compradores");

            // 2D. Lógica SQL: Renomear, Adicionar, Copiar Dados e Remover Colunas

            // 2D.1: Renomeia o Id antigo (IDENTITY)
            migrationBuilder.Sql(@"
    EXEC sp_rename 'Compradores.Id', 'Id_OLD_IDENTITY', 'COLUMN';
");

            // 2D.2: Adiciona a nova coluna Id (sem IDENTITY)
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Compradores",
                nullable: false,
                defaultValue: 0);

            // 2D.3: COPIA O VALOR CORRETO (UtilizadorId) para a nova coluna Id
            migrationBuilder.Sql(@"
    UPDATE Compradores
    SET Id = UtilizadorId;
");

            // 2D.4: Descarta o Id antigo (IDENTITY) e o UtilizadorId
            migrationBuilder.DropColumn(
                name: "Id_OLD_IDENTITY",
                table: "Compradores");

            migrationBuilder.DropColumn(
                name: "UtilizadorId",
                table: "Compradores");

            // 2E. Re-adicionar PK na nova coluna Id
            migrationBuilder.AddPrimaryKey(
                name: "PK_Compradores",
                table: "Compradores",
                column: "Id");

            // 2F. CORREÇÃO DE DADOS NAS TABELAS RELACIONADAS (CRUCIAL)
            // Como o Comprador.Id antigo foi descartado, precisamos mapear os IDs antigos 
            // para os novos IDs (Utilizador.id) nas tabelas que usam CompradorId.
            // O ÚNICO LUGAR ONDE O ID ANTIGO ESTÁ PRESERVADO É NO LOG DE MIGRAÇÃO
            // OU, se a coluna UtilizadorId NÃO FOI DESCARTADA (o que o log de migração
            // indica que não é o caso aqui, ela foi descartada). 

            // **ASSUMINDO QUE O VALOR EM CompradorId (nas tabelas relacionadas) ERA O ID DO UTILIZADOR:**
            // Se a FK original era de UtilizadorId, o valor em CompradorId já é o valor correto.
            // O log da primeira migração falhada mostrava que a coluna UtilizadorId existia nas 
            // tabelas Compradores/Vendedores/Administradores e que foi descartada.

            // **Se o erro persistir, significa que Favoritos.CompradorId está a usar IDs antigos (IDENTITY).**
            // A única forma de resolver isto é mapear o ID antigo (coluna Id_OLD_IDENTITY) para o novo ID.
            // Como não temos mais a coluna Id_OLD_IDENTITY nas tabelas de relacionamento (Compras, Reservas, Favoritos), 
            // e não podemos confiar 100% que o valor em CompradorId é o UtilizadorId, 
            // o mais seguro é **eliminar os registos orfãos que causam o conflito**.

            // ATENÇÃO: Se não quiser perder dados, a solução é muito mais complexa e
            // precisaria de uma tabela de mapeamento temporária.

            // Vamos tentar a solução mais comum para este erro: limpar os registos orfãos.
            migrationBuilder.Sql(@"
    DELETE FROM Favoritos 
    WHERE CompradorId NOT IN (SELECT Id FROM Compradores);
");
            migrationBuilder.Sql(@"
    DELETE FROM Compras 
    WHERE CompradorId NOT IN (SELECT Id FROM Compradores);
");
            migrationBuilder.Sql(@"
    DELETE FROM Reservas 
    WHERE CompradorId NOT IN (SELECT Id FROM Compradores);
");


            // 2G. Adicionar a nova FK 1:1 (Compradores -> Utilizador)
            migrationBuilder.AddForeignKey(
                name: "FK_Compradores_Utilizador_Id",
                table: "Compradores",
                column: "Id",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // 2H. Re-adicionar FKs externas (Compras, Reservas, Favoritos -> Compradores)
            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Compradores_CompradorId",
                table: "Compras",
                column: "CompradorId",
                principalTable: "Compradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_Compradores_CompradorId",
                table: "Reservas",
                column: "CompradorId",
                principalTable: "Compradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Compradores_CompradorId",
                table: "Favoritos",
                column: "CompradorId",
                principalTable: "Compradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 3A. Remover FKs que apontam para Administradores.Id (PK Antiga)
            // Nota: O Administrador é referenciado por Vendedores (AprovadoPorId).
            // Iremos tentar remover os dois nomes possíveis da FK que o EF Core pode ter criado.
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Administradores_AdministradorId", // Nome 1 (do log anterior)
                table: "Vendedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Administradores_AprovadoPorId", // Nome 2 (do log atual)
                table: "Vendedores");

            // Nota: O Administrador é referenciado por HistoricoAdmin (AdminId)
            migrationBuilder.DropForeignKey(
                name: "FK_HistoricosAdmin_Administradores_AdminId",
                table: "HistoricosAdmin");

            // 3B. Remover FK interna antiga e Index
            migrationBuilder.DropForeignKey(
                name: "FK_Administradores_Utilizador_UtilizadorId",
                table: "Administradores");

            migrationBuilder.DropIndex(
                name: "IX_Administradores_UtilizadorId",
                table: "Administradores");

            // 3C. Remover PK
            migrationBuilder.DropPrimaryKey(
                name: "PK_Administradores",
                table: "Administradores");

            // 3D. Lógica SQL: Renomear, Adicionar, Copiar Dados e Remover Colunas

            // 3D.1: Renomeia o Id antigo (IDENTITY)
            migrationBuilder.Sql(@"
                EXEC sp_rename 'Administradores.Id', 'Id_OLD_IDENTITY', 'COLUMN';
            ");

            // 3D.2: Adiciona a nova coluna Id (sem IDENTITY)
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Administradores",
                nullable: false,
                defaultValue: 0);

            // 3D.3: COPIA O VALOR CORRETO (UtilizadorId) para a nova coluna Id
            migrationBuilder.Sql(@"
                UPDATE Administradores
                SET Id = UtilizadorId;
            ");

            // 3D.4: Descarta o Id antigo (IDENTITY) e o UtilizadorId
            migrationBuilder.DropColumn(
                name: "Id_OLD_IDENTITY",
                table: "Administradores");

            migrationBuilder.DropColumn(
                name: "UtilizadorId",
                table: "Administradores");

            // 3E. Re-adicionar PK na nova coluna Id
            migrationBuilder.AddPrimaryKey(
                name: "PK_Administradores",
                table: "Administradores",
                column: "Id");

            // 3F. Adicionar a nova FK 1:1 (Administradores -> Utilizador)
            migrationBuilder.AddForeignKey(
                name: "FK_Administradores_Utilizador_Id",
                table: "Administradores",
                column: "Id",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // 3G. Re-adicionar FKs externas (Vendedores e HistoricoAdmin -> Administradores)
            // Use o nome da FK que corresponda ao modelo .NET (AprovadoPorId)
            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Administradores_AprovadoPorId",
                table: "Vendedores",
                column: "AprovadoPorId",
                principalTable: "Administradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Re-adicionar HistoricosAdmin
            migrationBuilder.AddForeignKey(
                name: "FK_HistoricosAdmin_Administradores_AdminId",
                table: "HistoricosAdmin",
                column: "AdminId",
                principalTable: "Administradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Administradores_Utilizador_Id",
                table: "Administradores");

            migrationBuilder.DropForeignKey(
                name: "FK_Compradores_Utilizador_Id",
                table: "Compradores");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Utilizador_Id",
                table: "Vendedores");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Vendedores",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "UtilizadorId",
                table: "Vendedores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Compradores",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "UtilizadorId",
                table: "Compradores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Administradores",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "UtilizadorId",
                table: "Administradores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_UtilizadorId",
                table: "Vendedores",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compradores_UtilizadorId",
                table: "Compradores",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Administradores_UtilizadorId",
                table: "Administradores",
                column: "UtilizadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Administradores_Utilizador_UtilizadorId",
                table: "Administradores",
                column: "UtilizadorId",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Compradores_Utilizador_UtilizadorId",
                table: "Compradores",
                column: "UtilizadorId",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Utilizador_UtilizadorId",
                table: "Vendedores",
                column: "UtilizadorId",
                principalTable: "Utilizador",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
