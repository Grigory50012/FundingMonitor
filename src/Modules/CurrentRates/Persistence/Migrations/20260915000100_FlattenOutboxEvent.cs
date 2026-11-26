using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FundingMonitor.CurrentRates.Infrastructure.Persistence.Migrations;

public partial class FlattenOutboxEvent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Exchange", schema: "current_rates", table: "OutboxMessages",
            type: "character varying(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Symbol", schema: "current_rates", table: "OutboxMessages",
            type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset?>(name: "NextFundingTime", schema: "current_rates", table: "OutboxMessages",
            type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql("""
            UPDATE current_rates."OutboxMessages"
            SET "Exchange" = "Payload" ->> 'exchange',
                "Symbol" = "Payload" ->> 'symbol',
                "NextFundingTime" = ("Payload" ->> 'nextFundingTime')::timestamptz;
            """);

        migrationBuilder.AlterColumn<string>(name: "Exchange", schema: "current_rates", table: "OutboxMessages",
            type: "character varying(32)", maxLength: 32, nullable: false,
            oldClrType: typeof(string), oldType: "character varying(32)", oldMaxLength: 32, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "Symbol", schema: "current_rates", table: "OutboxMessages",
            type: "character varying(64)", maxLength: 64, nullable: false,
            oldClrType: typeof(string), oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);
        migrationBuilder.DropColumn(name: "Payload", schema: "current_rates", table: "OutboxMessages");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Payload", schema: "current_rates", table: "OutboxMessages",
            type: "jsonb", nullable: true);
        migrationBuilder.Sql("""
            UPDATE current_rates."OutboxMessages"
            SET "Payload" = jsonb_build_object(
                'eventId', "Id", 'occurredOnUtc', "OccurredOnUtc",
                'exchange', "Exchange", 'symbol', "Symbol",
                'nextFundingTime', "NextFundingTime");
            """);
        migrationBuilder.AlterColumn<string>(name: "Payload", schema: "current_rates", table: "OutboxMessages",
            type: "jsonb", nullable: false, oldClrType: typeof(string), oldType: "jsonb", oldNullable: true);
        migrationBuilder.DropColumn(name: "Exchange", schema: "current_rates", table: "OutboxMessages");
        migrationBuilder.DropColumn(name: "Symbol", schema: "current_rates", table: "OutboxMessages");
        migrationBuilder.DropColumn(name: "NextFundingTime", schema: "current_rates", table: "OutboxMessages");
    }
}
