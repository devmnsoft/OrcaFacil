using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HomologPendingModelCheck_20261007 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As tabelas e índices já são criados pelos hotfixes SQL idempotentes
            // V6.7, V6.8 e V6.9. Esta migration só alinha o snapshot do modelo
            // para que `database update` deixe de recusar a instalação limpa.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
