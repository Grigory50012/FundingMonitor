# ADR 0004: Entity Framework Core + PostgreSQL для хранения данных

**Статус**: Принято  
**Дата**: 2026-06-14

## Контекст

Нужно хранить:
- Текущие ставки финансирования (обновляются каждые 10 сек, ~1000+ записей)
- Исторические ставки (накапливаются, миллионы записей за месяц)
- Миграции схемы БД

Требования: ACID, производительность bulk-операций, LINQ, миграции, типизация.

## Решение

**Entity Framework Core 10 + PostgreSQL 17** с провайдером `Npgsql.EntityFrameworkCore.PostgreSQL`.

Особенности реализации:
- `FundingMonitorDbContext` с двумя `DbSet`: `CurrentFundingRates`, `HistoricalFundingRates`
- **Bulk Extensions** (`EFCore.BulkExtensions`) для `BulkInsertOrUpdateAsync` и `BulkInsertAsync` — критично для производительности при 1000+ записей за цикл
- Составные PK: `(Exchange, NormalizedSymbol)` для current, `(Exchange, NormalizedSymbol, FundingTime)` для history
- Индексы: по `Symbol`, `FundingTime`, `Exchange` для быстрых выборок
- Миграции в проекте `Infrastructure`, применяются автоматически при старте API (`dbContext.Database.MigrateAsync()`)

## Последствия

✅ **Плюсы**:
- Полная типизация, LINQ, change tracking
- Миграции в коде, версионируются в git
- Bulk Extensions дают 10-50x ускорение vs `SaveChangesAsync`
- PostgreSQL — зрелая, надежная, поддерживает JSONB при необходимости
- Есть в docker-compose, простое развёртывание

❌ **Минусы**:
- Change tracking overhead (отключен `AsNoTracking` для чтения)
- Bulk Extensions — платная лицензия для коммерческого использования (есть бесплатная Community версия с ограничениями)
- При очень больших объёмах истории (100M+) понадобится партиционирование / TimescaleDB

## Альтернативы

| Вариант | Причина отказа |
|---------|----------------|
| Dapper + ручные SQL | Много boilerplate, нет миграций, легко ошибиться в маппинге |
| MongoDB | Нет ACID транзакций для bulk, не реляционная природа данных |
| TimescaleDB (PostgreSQL extension) | Преждевременная оптимизация, обычный PG пока справляется |
| ClickHouse / Apache Druid | OLAP, не для транзакционных обновлений current rates |
| SQLite | Не для продакшена с конкурентной записью |

## Планы

При росте истории > 50M строк — внедрить партиционирование по `FundingTime` (месячные партиции) или мигрировать на TimescaleDB.