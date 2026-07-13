using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricalFundingRate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Exchange = table.Column<string>(type: "text", nullable: false),
                    NormalizedSymbol = table.Column<string>(type: "text", nullable: false),
                    FundingRate = table.Column<decimal>(type: "numeric(10,8)", nullable: false),
                    FundingTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalFundingRate", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_CollectedAt",
                table: "HistoricalFundingRate",
                column: "CollectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_Exchange_NormalizedSymbol",
                table: "HistoricalFundingRate",
                columns: new[] { "Exchange", "NormalizedSymbol" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_Exchange_NormalizedSymbol_FundingTime",
                table: "HistoricalFundingRate",
                columns: new[] { "Exchange", "NormalizedSymbol", "FundingTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_FundingTime",
                table: "HistoricalFundingRate",
                column: "FundingTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricalFundingRate");
        }
    }
}
