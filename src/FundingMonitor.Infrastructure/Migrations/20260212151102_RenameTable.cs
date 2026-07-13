using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FundingRateCurrent",
                table: "FundingRateCurrent");

            migrationBuilder.RenameTable(
                name: "FundingRateCurrent",
                newName: "CurrentFundingRate");

            migrationBuilder.RenameIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol_Exchange",
                table: "CurrentFundingRate",
                newName: "IX_CurrentFundingRate_NormalizedSymbol_Exchange");

            migrationBuilder.RenameIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol",
                table: "CurrentFundingRate",
                newName: "IX_CurrentFundingRate_NormalizedSymbol");

            migrationBuilder.RenameIndex(
                name: "IX_FundingRateCurrent_Exchange",
                table: "CurrentFundingRate",
                newName: "IX_CurrentFundingRate_Exchange");

            migrationBuilder.RenameIndex(
                name: "IX_FundingRateCurrent_BaseAsset_IsActive",
                table: "CurrentFundingRate",
                newName: "IX_CurrentFundingRate_BaseAsset_IsActive");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CurrentFundingRate",
                table: "CurrentFundingRate",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CurrentFundingRate",
                table: "CurrentFundingRate");

            migrationBuilder.RenameTable(
                name: "CurrentFundingRate",
                newName: "FundingRateCurrent");

            migrationBuilder.RenameIndex(
                name: "IX_CurrentFundingRate_NormalizedSymbol_Exchange",
                table: "FundingRateCurrent",
                newName: "IX_FundingRateCurrent_NormalizedSymbol_Exchange");

            migrationBuilder.RenameIndex(
                name: "IX_CurrentFundingRate_NormalizedSymbol",
                table: "FundingRateCurrent",
                newName: "IX_FundingRateCurrent_NormalizedSymbol");

            migrationBuilder.RenameIndex(
                name: "IX_CurrentFundingRate_Exchange",
                table: "FundingRateCurrent",
                newName: "IX_FundingRateCurrent_Exchange");

            migrationBuilder.RenameIndex(
                name: "IX_CurrentFundingRate_BaseAsset_IsActive",
                table: "FundingRateCurrent",
                newName: "IX_FundingRateCurrent_BaseAsset_IsActive");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FundingRateCurrent",
                table: "FundingRateCurrent",
                column: "Id");
        }
    }
}
