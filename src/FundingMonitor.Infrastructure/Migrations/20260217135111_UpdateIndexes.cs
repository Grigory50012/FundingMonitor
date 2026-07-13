using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoricalFundingRate_CollectedAt",
                table: "HistoricalFundingRate");

            migrationBuilder.DropIndex(
                name: "IX_HistoricalFundingRate_Exchange_NormalizedSymbol_FundingTime",
                table: "HistoricalFundingRate");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricalFundingRate_FundingTime",
                table: "HistoricalFundingRate",
                newName: "IX_HistoricalFundingRate_Time");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricalFundingRate_Exchange_NormalizedSymbol",
                table: "HistoricalFundingRate",
                newName: "IX_HistoricalFundingRate_Exchange_Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_Exchange_Symbol_Time",
                table: "HistoricalFundingRate",
                columns: new[] { "Exchange", "NormalizedSymbol", "FundingTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoricalFundingRate_Exchange_Symbol_Time",
                table: "HistoricalFundingRate");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricalFundingRate_Time",
                table: "HistoricalFundingRate",
                newName: "IX_HistoricalFundingRate_FundingTime");

            migrationBuilder.RenameIndex(
                name: "IX_HistoricalFundingRate_Exchange_Symbol",
                table: "HistoricalFundingRate",
                newName: "IX_HistoricalFundingRate_Exchange_NormalizedSymbol");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_CollectedAt",
                table: "HistoricalFundingRate",
                column: "CollectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_Exchange_NormalizedSymbol_FundingTime",
                table: "HistoricalFundingRate",
                columns: new[] { "Exchange", "NormalizedSymbol", "FundingTime" },
                unique: true);
        }
    }
}
