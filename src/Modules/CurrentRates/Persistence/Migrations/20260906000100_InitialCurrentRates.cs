using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FundingMonitor.CurrentRates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCurrentRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "current_rates");

            migrationBuilder.CreateTable(
                name: "CurrentFundingRates",
                schema: "current_rates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Exchange = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NormalizedSymbol = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BaseAsset = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    QuoteAsset = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MarkPrice = table.Column<decimal>(type: "numeric(28,12)", precision: 28, scale: 12, nullable: false),
                    IndexPrice = table.Column<decimal>(type: "numeric(28,12)", precision: 28, scale: 12, nullable: false),
                    FundingRate = table.Column<decimal>(type: "numeric(20,12)", precision: 20, scale: 12, nullable: false),
                    FundingIntervalHours = table.Column<int>(type: "integer", nullable: true),
                    NextFundingTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PredictedNextRate = table.Column<decimal>(type: "numeric(20,12)", precision: 20, scale: 12, nullable: true),
                    LastSeenOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentFundingRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "current_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockOwner = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentFundingRates_IsActive_BaseAsset",
                schema: "current_rates",
                table: "CurrentFundingRates",
                columns: new[] { "IsActive", "BaseAsset" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentFundingRates_NormalizedSymbol_Exchange",
                schema: "current_rates",
                table: "CurrentFundingRates",
                columns: new[] { "NormalizedSymbol", "Exchange" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_LockedUntilUtc",
                schema: "current_rates",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "LockedUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                schema: "current_rates",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentFundingRates",
                schema: "current_rates");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "current_rates");
        }
    }
}
