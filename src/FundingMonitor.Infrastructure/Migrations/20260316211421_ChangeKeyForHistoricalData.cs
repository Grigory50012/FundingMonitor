using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeKeyForHistoricalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_HistoricalFundingRate",
                table: "HistoricalFundingRate");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "HistoricalFundingRate",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistoricalFundingRate",
                table: "HistoricalFundingRate",
                columns: new[] { "Exchange", "NormalizedSymbol", "FundingTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_HistoricalFundingRate",
                table: "HistoricalFundingRate");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "HistoricalFundingRate",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistoricalFundingRate",
                table: "HistoricalFundingRate",
                column: "Id");
        }
    }
}
