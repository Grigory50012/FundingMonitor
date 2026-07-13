using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraint_Exchange_NormalizedSymbol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol_Exchange",
                table: "FundingRateCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol_Exchange",
                table: "FundingRateCurrent",
                columns: new[] { "NormalizedSymbol", "Exchange" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol_Exchange",
                table: "FundingRateCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol_Exchange",
                table: "FundingRateCurrent",
                columns: new[] { "NormalizedSymbol", "Exchange" });
        }
    }
}
