using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaFerias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaFerias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataEntradaFerias",
                table: "Ferias",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFinalizacaoFerias",
                table: "Ferias",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataEntradaFerias",
                table: "Ferias");

            migrationBuilder.DropColumn(
                name: "DataFinalizacaoFerias",
                table: "Ferias");
        }
    }
}
