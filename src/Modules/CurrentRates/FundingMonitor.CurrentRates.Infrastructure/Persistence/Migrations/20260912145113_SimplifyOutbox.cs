using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.CurrentRates.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_LockedUntilUtc",
                schema: "current_rates",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockOwner",
                schema: "current_rates",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                schema: "current_rates",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "current_rates",
                table: "OutboxMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LockOwner",
                schema: "current_rates",
                table: "OutboxMessages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntilUtc",
                schema: "current_rates",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "current_rates",
                table: "OutboxMessages",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_LockedUntilUtc",
                schema: "current_rates",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "LockedUntilUtc" });
        }
    }
}
