using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FundingMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundingRateCurrent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Exchange = table.Column<string>(type: "text", nullable: false),
                    NormalizedSymbol = table.Column<string>(type: "text", nullable: false),
                    BaseAsset = table.Column<string>(type: "text", nullable: false),
                    QuoteAsset = table.Column<string>(type: "text", nullable: false),
                    MarkPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    IndexPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    FundingRate = table.Column<decimal>(type: "numeric(10,8)", nullable: false),
                    FundingIntervalHours = table.Column<int>(type: "integer", nullable: false),
                    NextFundingTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCheck = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictedNextRate = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingRateCurrent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundingRateCurrent_Exchange",
                table: "FundingRateCurrent",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol",
                table: "FundingRateCurrent",
                column: "NormalizedSymbol");

            migrationBuilder.CreateIndex(
                name: "IX_FundingRateCurrent_NormalizedSymbol_Exchange",
                table: "FundingRateCurrent",
                columns: new[] { "NormalizedSymbol", "Exchange" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundingRateCurrent");
        }
    }
}
