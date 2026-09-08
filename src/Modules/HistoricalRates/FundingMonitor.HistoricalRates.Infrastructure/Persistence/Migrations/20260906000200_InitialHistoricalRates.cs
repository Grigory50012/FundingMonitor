using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.HistoricalRates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialHistoricalRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "historical_rates");

            migrationBuilder.CreateTable(
                name: "HistoricalFundingRates",
                schema: "historical_rates",
                columns: table => new
                {
                    Exchange = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NormalizedSymbol = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FundingTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FundingRate = table.Column<decimal>(type: "numeric(20,12)", precision: 20, scale: 12, nullable: false),
                    CollectedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalFundingRates", x => new { x.Exchange, x.NormalizedSymbol, x.FundingTime });
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "historical_rates",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Consumer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.EventId, x.Consumer });
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRates_Exchange_NormalizedSymbol",
                schema: "historical_rates",
                table: "HistoricalFundingRates",
                columns: new[] { "Exchange", "NormalizedSymbol" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRates_FundingTime",
                schema: "historical_rates",
                table: "HistoricalFundingRates",
                column: "FundingTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricalFundingRates",
                schema: "historical_rates");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "historical_rates");
        }
    }
}
