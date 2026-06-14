# Database Schema Documentation

PostgreSQL 17 schema for FundingMonitor, managed via EF Core migrations.

---

## Tables

### 1. CurrentFundingRate

Хранит **актуальные** ставки финансирования. Обновляется каждые 10 секунд фоновым сервисом.

| Колонка | Тип | Nullable | Описание |
|---------|-----|----------|----------|
| `Id` | `integer` | NO | Surrogate PK (identity) |
| `Exchange` | `text` | NO | Биржа: 'Binance', 'Bybit', 'OKX' |
| `NormalizedSymbol` | `text` | NO | Символ: 'BTC-USDT', 'ETH-USDT' |
| `BaseAsset` | `text` | NO | Базовый актив: 'BTC' |
| `QuoteAsset` | `text` | NO | Котируемый актив: 'USDT' |
| `MarkPrice` | `decimal(18,8)` | YES | Mark Price |
| `IndexPrice` | `decimal(18,8)` | YES | Index Price |
| `FundingRate` | `decimal(10,8)` | NO | Ставка финансирования (на период) |
| `FundingIntervalHours` | `integer` | NO | Интервал выплат в часах (8, 4, 1) |
| `NextFundingTime` | `timestamp with time zone` | YES | Время следующей выплаты (UTC) |
| `LastCheck` | `timestamp with time zone` | NO | Когда последний раз опрашивали |
| `PredictedNextRate` | `decimal(10,8)` | YES | Прогноз следующей ставки |
| `IsActive` | `boolean` | NO | Активен ли символ (true по умолчанию) |

**Индексы**:
- `PK_CurrentFundingRate` — Primary Key на `Id`
- `IX_CurrentFundingRate_NormalizedSymbol_Exchange` — **Unique** на `(NormalizedSymbol, Exchange)` — один символ на биржу
- `IX_CurrentFundingRate_IsActive_BaseAsset` — для фильтрации активных по базе

**Примечание**: EF Core использует `Id` как PK, но бизнес-ключ — `(Exchange, NormalizedSymbol)`. Unique индекс гарантирует уникальность.

---

### 2. HistoricalFundingRate

Хранит **историю** начислений ставок. Только `INSERT`, никогда не обновляется.

| Колонка | Тип | Nullable | Описание |
|---------|-----|----------|----------|
| `Exchange` | `text` | NO | Биржа (часть PK) |
| `NormalizedSymbol` | `text` | NO | Символ (часть PK) |
| `Id` | `integer` | NO | Surrogate column (не входит в PK, генерируется EF) |
| `FundingRate` | `decimal(10,8)` | NO | Ставка финансирования |
| `FundingTime` | `timestamp with time zone` | NO | Время выплаты (часть PK) |
| `CollectedAt` | `timestamp with time zone` | NO | Когда записали в БД |

**Индексы**:
- `PK_HistoricalFundingRate` — **Composite Primary Key** на `(Exchange, NormalizedSymbol, FundingTime)`
- `IX_HistoricalFundingRate_Exchange_Symbol` — на `(Exchange, NormalizedSymbol)` для выборки истории по символу
- `IX_HistoricalFundingRate_Time` — на `FundingTime` для временных окон

**Объём**: ~1000 записей/цикл × 3 биржи × 3 выплаты/день = ~9,000 записей/день. За месяц ~270K строк на символ.

---

## Relationships

```
CurrentFundingRate (1) ──────< (0..*) HistoricalFundingRate
   │
   └── связаны по (Exchange, NormalizedSymbol)
       (нет FK constraint — исторические данные не удаляются при деактивации current)
```

Нет явных Foreign Keys — производительность bulk insert и независимость таблиц.

---

## Migrations

Расположены в `src/FundingMonitor.Infrastructure/Migrations/`.

> **Первый запуск**: если папка миграций пуста, создайте начальную миграцию:
> ```bash
> dotnet ef migrations add InitialCreate --project src/FundingMonitor.Infrastructure --startup-project src/FundingMonitor.Api
> ```

Применение:
```bash
# Автоматически при старте API (Program.cs)
await dbContext.Database.MigrateAsync();

# Или вручную
dotnet ef database update --project src/FundingMonitor.Infrastructure
```

---

## Performance Notes

| Операция | Метод | Комментарий |
|----------|-------|-------------|
| Запись current rates (1000+ за цикл) | `EFCore.BulkExtensions.BulkInsertOrUpdateAsync` | ~50ms vs 2-5 сек на SaveChanges |
| Запись history (batch) | `BulkInsertOrUpdateAsync` | Отключены change tracking, triggers |
| Чтение current | `AsNoTracking().Where(...)` | Индекс по Symbol+Exchange |
| Чтение history | `AsNoTracking().Where(...).OrderByDescending(t => t.FundingTime)` | Индекс по Time + Symbol |

---

## Future Improvements

1. **Partitioning** `HistoricalFundingRate` по `FundingTime` (месячные партиции) при >50M строк
2. **TimescaleDB** — гипертаблицы для автоматического партиционирования и компрессии
3. **Materialized Views** для предрасчёта APR статистики
4. **Archive policy** — перемещение старых данных в cold storage (S3/Parquet)