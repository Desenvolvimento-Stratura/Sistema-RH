using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaFerias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeAutoReplyBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeAutoReplyBackups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FeriasId = table.Column<int>(type: "int", nullable: false),
                    LoginAd = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExternalAudience = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InternalReply = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalReply = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataBackup = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    BackupRestaurado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataRestauracao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeAutoReplyBackups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeAutoReplyBackups_Ferias_FeriasId",
                        column: x => x.FeriasId,
                        principalTable: "Ferias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeAutoReplyBackups_FeriasId",
                table: "ExchangeAutoReplyBackups",
                column: "FeriasId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeAutoReplyBackups");
        }
    }
}