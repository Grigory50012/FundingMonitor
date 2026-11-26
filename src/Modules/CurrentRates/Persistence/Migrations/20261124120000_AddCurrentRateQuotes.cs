using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.CurrentRates.Infrastructure.Persistence.Migrations;

public partial class AddCurrentRateQuotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AskPrice",
            schema: "current_rates",
            table: "CurrentFundingRates",
            type: "numeric(28,12)",
            precision: 28,
            scale: 12,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "BidPrice",
            schema: "current_rates",
            table: "CurrentFundingRates",
            type: "numeric(28,12)",
            precision: 28,
            scale: 12,
            nullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "FundingRate",
            schema: "current_rates",
            table: "CurrentFundingRates",
            type: "numeric(20,12)",
            precision: 20,
            scale: 12,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(20,12)",
            oldPrecision: 20,
            oldScale: 12);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE current_rates."CurrentFundingRates"
            SET "FundingRate" = 0
            WHERE "FundingRate" IS NULL;
            """);

        migrationBuilder.AlterColumn<decimal>(
            name: "FundingRate",
            schema: "current_rates",
            table: "CurrentFundingRates",
            type: "numeric(20,12)",
            precision: 20,
            scale: 12,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(20,12)",
            oldPrecision: 20,
            oldScale: 12,
            oldNullable: true);

        migrationBuilder.DropColumn(name: "AskPrice", schema: "current_rates", table: "CurrentFundingRates");
        migrationBuilder.DropColumn(name: "BidPrice", schema: "current_rates", table: "CurrentFundingRates");
    }
}
