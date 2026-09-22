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

`current_rates."OutboxMessages"` записывается в одной транзакции с текущими ставками. Поля: `Id` (UUID, PK и идентификатор события), `OccurredOnUtc`, `Exchange` (`varchar(32)`), `Symbol` (`varchar(64)`), nullable `NextFundingTime` (`timestamptz`), nullable `ProcessedOnUtc`, `Attempts`, nullable `Error`. `Exchange`, `Symbol` и `NextFundingTime` — снимок события на момент записи, а не ссылка на изменяемую текущую ставку. При публикации событие собирается из этих колонок.

Индекс `(ProcessedOnUtc, OccurredOnUtc)` обслуживает выборку ожидающих событий. Один последовательный dispatcher читает их без lease, публикует в RabbitMQ и отмечает `ProcessedOnUtc` только после publisher confirm без returned message. `Attempts` считает неудачные попытки, `Error` содержит последнюю ошибку и очищается при успехе. Сбой между confirm и отметкой допускает повторную доставку и повторный API-запрос; upsert истории предотвращает дубли строк.

## Исторические ставки

Таблица `historical_rates."HistoricalFundingRates"`:

| Поле | Тип PostgreSQL | Назначение |
| --- | --- | --- |
| `Exchange` | `varchar(32)` | Часть PK |
| `NormalizedSymbol` | `varchar(64)` | Часть PK |
| `FundingTime` | `timestamptz` | Часть PK, время выплаты |
| `FundingRate` | `numeric(20,12)` | Ставка |
| `CollectedOnUtc` | `timestamptz` | Время сбора |

Composite PK `(Exchange, NormalizedSymbol, FundingTime)` делает сохранение идемпотентным. Дополнительные индексы: `(Exchange, NormalizedSymbol)` и `FundingTime`. Пакетная запись использует параметризованный PostgreSQL `INSERT ... ON CONFLICT ... DO UPDATE`; повторная выплата обновляет `FundingRate` и `CollectedOnUtc` без создания дубля.

Inbox отсутствует. Каждая доставка вызывает сбор истории; один SQL upsert атомарно сохраняет пакет, затем consumer отправляет manual ACK. При ошибке записи используется штатный retry/DLQ workflow. Пустой успешный результат не требует записи в БД.

## Миграции

Исходники миграций и model snapshots находятся в:

- [CurrentRates Infrastructure/Persistence/Migrations](../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Infrastructure/Persistence/Migrations)
- [HistoricalRates Infrastructure/Persistence/Migrations](../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Infrastructure/Persistence/Migrations)

Применение из корня репозитория:

```bash
dotnet run --project src/FundingMonitor.DatabaseMigrator
dotnet run --project src/FundingMonitor.ApiHost
```

Вторая команда допустима только после успешного завершения первой. ApiHost не применяет миграции. Первоначальный переход на модульную схему (ADR 0012) использовал baseline migrations без переноса прежних development-данных; повторный запуск migrator безопасен для уже актуальной схемы. При изменении модели новая миграция и snapshot коммитятся вместе с кодом владельца.

Миграции `SimplifyOutbox` и `RemoveHistoricalInbox` обновляют существующую модульную схему без потери Outbox events и истории. Они удаляют `Type`, `LockOwner`, `LockedUntilUtc`, lease-индекс и таблицу Inbox. Откат восстанавливает структуру, но не прежний журнал Inbox. Перед обновлением остановить ApiHost, выполнить DatabaseMigrator, затем запустить ровно один экземпляр новой версии. Подробнее: [ADR 0013](adr/0013-simple-outbox-without-inbox.md).

Миграция `FlattenOutboxEvent` переносит `Exchange`, `Symbol` и `NextFundingTime` из прежнего `Payload` (`jsonb`) в типизированные колонки, затем удаляет `Payload`. Ожидающие события, `Attempts` и `Error` сохраняются. Откат восстанавливает JSON из колонок и метаданных события. Перед применением миграции остановить ApiHost и выполнить DatabaseMigrator; миграция не запускается при старте ApiHost.

Правила изоляции и миграций закреплены в [ADR 0012](adr/0012-strict-modular-monolith.md). Команды проверки БД и брокера: [Docker](deployment/docker.md).
