using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoricalFundingRate_Exchange_Symbol_Time",
                table: "HistoricalFundingRate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HistoricalFundingRate_Exchange_Symbol_Time",
                table: "HistoricalFundingRate",
                columns: new[] { "Exchange", "NormalizedSymbol", "FundingTime" },
                unique: true);
        }
    }
}
