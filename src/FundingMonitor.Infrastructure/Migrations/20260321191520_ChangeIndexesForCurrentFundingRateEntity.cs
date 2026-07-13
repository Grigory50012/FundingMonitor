using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIndexesForCurrentFundingRateEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurrentFundingRate_BaseAsset_IsActive",
                table: "CurrentFundingRate");

            migrationBuilder.DropIndex(
                name: "IX_CurrentFundingRate_Exchange",
                table: "CurrentFundingRate");

            migrationBuilder.DropIndex(
                name: "IX_CurrentFundingRate_NormalizedSymbol",
                table: "CurrentFundingRate");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentFundingRate_IsActive_BaseAsset",
                table: "CurrentFundingRate",
                columns: new[] { "IsActive", "BaseAsset" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurrentFundingRate_IsActive_BaseAsset",
                table: "CurrentFundingRate");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentFundingRate_BaseAsset_IsActive",
                table: "CurrentFundingRate",
                columns: new[] { "BaseAsset", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentFundingRate_Exchange",
                table: "CurrentFundingRate",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentFundingRate_NormalizedSymbol",
                table: "CurrentFundingRate",
                column: "NormalizedSymbol");
        }
    }
}
