# Схема базы данных

Одна PostgreSQL 17 database содержит две независимые схемы. `CurrentRatesDbContext` владеет `current_rates`, `HistoricalRatesDbContext` — `historical_rates`. Каждый контекст имеет собственные миграции, model snapshot и таблицу `__EFMigrationsHistory` в своей схеме. Cross-schema queries, foreign keys и EF navigation properties между модулями запрещены.

`Arbitrage` вычисляет результат в памяти и не имеет schema или DbContext. `ExchangeIntegration` не использует persistence. Доступ к данным другого модуля возможен через его `Contracts`, см. [архитектуру](architecture/index.md).

## Текущие ставки

Таблица `current_rates."CurrentFundingRates"`:

| Поле | Тип PostgreSQL | Назначение |
| --- | --- | --- |
| `Id` | `integer`, identity | Primary key |
| `Exchange` | `varchar(32)` | Биржа |
| `NormalizedSymbol` | `varchar(64)` | Нормализованный символ |
| `BaseAsset`, `QuoteAsset` | `varchar(32)` | Активы пары |
| `MarkPrice`, `IndexPrice` | `numeric(28,12)` | Цены, обязательные поля |
| `FundingRate` | `numeric(20,12)` | Ставка за период |
| `FundingIntervalHours` | `integer`, nullable | Интервал выплат |
| `NextFundingTime` | `timestamptz`, nullable | Следующая выплата |
| `PredictedNextRate` | `numeric(20,12)`, nullable | Прогноз |
| `LastSeenOnUtc` | `timestamptz` | Последний успешный сбор пары |
| `IsActive` | `boolean` | Доступна в обычной выдаче и арбитраже |

Уникальный индекс `(NormalizedSymbol, Exchange)` защищает бизнес-ключ. Индекс `(IsActive, BaseAsset)` поддерживает выдачу активных ставок. Чтение current rates дополнительно требует `LastSeenOnUtc >= now - DeactivateMissingAfterMinutes`; просроченные значения исключаются из выдачи даже при неудачных опросах биржи, но остаются в БД. При успешном snapshot отсутствующие просроченные пары получают `IsActive = false`; повторное появление обновляет timestamp и активирует пару.

## Outbox

`current_rates."OutboxMessages"` записывается в одной транзакции с текущими ставками. Поля: `Id` (UUID, PK и идентификатор события), `Type`, `Payload` (`jsonb`), `OccurredOnUtc`, nullable `ProcessedOnUtc`, `Attempts`, nullable `Error`, `LockedUntilUtc`, `LockOwner`.

Индексы `(ProcessedOnUtc, OccurredOnUtc)` и `(ProcessedOnUtc, LockedUntilUtc)` обслуживают выборку и lease отправки. Dispatcher публикует событие в RabbitMQ и отмечает `ProcessedOnUtc` после publisher confirm. Сбой между confirm и отметкой может дать повторную доставку, которую обработает Inbox.

## Исторические ставки и Inbox

Таблица `historical_rates."HistoricalFundingRates"`:

| Поле | Тип PostgreSQL | Назначение |
| --- | --- | --- |
| `Exchange` | `varchar(32)` | Часть PK |
| `NormalizedSymbol` | `varchar(64)` | Часть PK |
| `FundingTime` | `timestamptz` | Часть PK, время выплаты |
| `FundingRate` | `numeric(20,12)` | Ставка |
| `CollectedOnUtc` | `timestamptz` | Время сбора |

Composite PK `(Exchange, NormalizedSymbol, FundingTime)` делает сохранение идемпотентным. Дополнительные индексы: `(Exchange, NormalizedSymbol)` и `FundingTime`. Пакетная запись использует параметризованный PostgreSQL `INSERT ... ON CONFLICT ... DO UPDATE`; повторная выплата обновляет `FundingRate` и `CollectedOnUtc` без создания дубля.

Таблица `historical_rates."InboxMessages"` хранит `EventId` (UUID), `Consumer` (`varchar(256)`) и `ProcessedOnUtc`. PK `(EventId, Consumer)` подавляет redelivery одного события для конкретного обработчика. История и Inbox фиксируются в одной транзакции, затем consumer отправляет manual ACK. Разные `EventId` одной пары обрабатываются независимо.

## Миграции

Исходники миграций и model snapshots находятся в:

- [CurrentRates Infrastructure/Persistence/Migrations](../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Infrastructure/Persistence/Migrations)
- [HistoricalRates Infrastructure/Persistence/Migrations](../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Infrastructure/Persistence/Migrations)

Применение из корня репозитория:

```bash
dotnet run --project src/FundingMonitor.DatabaseMigrator
dotnet run --project src/FundingMonitor.ApiHost
```

Вторая команда допустима только после успешного завершения первой. ApiHost не применяет миграции. Переход использует новые baseline migrations и не переносит прежние development-данные; повторный запуск migrator безопасен для уже актуальной схемы. При изменении модели новая миграция и snapshot коммитятся вместе с кодом владельца.

Правила изоляции и миграций закреплены в [ADR 0012](adr/0012-strict-modular-monolith.md). Команды проверки БД и брокера: [Docker](deployment/docker.md).
