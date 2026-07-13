using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddndexByBaseAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FundingRateCurrent_BaseAsset_IsActive",
                table: "FundingRateCurrent",
                columns: new[] { "BaseAsset", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundingRateCurrent_BaseAsset_IsActive",
                table: "FundingRateCurrent");
        }
    }
}
