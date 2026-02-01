using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingMonitor.Data.Configurations;

public class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
{
    public void Configure(EntityTypeBuilder<Exchange> builder)
    {
        builder.ToTable("exchanges");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ApiBaseUrl)
            .HasMaxLength(255);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Индексы
        builder.HasIndex(e => e.Name)
            .IsUnique();

        // Seed данные (начальные данные)
        builder.HasData(
            new Exchange
            {
                Id = 1,
                Name = "Binance",
                ApiBaseUrl = "https://api.binance.com",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Exchange
            {
                Id = 2,
                Name = "Bybit",
                ApiBaseUrl = "https://api.bybit.com",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}