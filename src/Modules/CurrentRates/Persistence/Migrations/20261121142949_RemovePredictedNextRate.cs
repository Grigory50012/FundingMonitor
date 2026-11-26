using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.CurrentRates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePredictedNextRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PredictedNextRate",
                schema: "current_rates",
                table: "CurrentFundingRates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PredictedNextRate",
                schema: "current_rates",
                table: "CurrentFundingRates",
                type: "numeric(20,12)",
                precision: 20,
                scale: 12,
                nullable: true);
        }
    }
}
