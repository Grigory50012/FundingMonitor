# CurrentRates

`CurrentRates` периодически собирает текущие funding rates, хранит последнее состояние каждой пары, исключает устаревшие ставки из выдачи и публикует задания на исторический сбор. Модуль владеет schema `current_rates`, но не обращается к schema истории.

## Граница и слои

```mermaid
flowchart LR
    EI[ExchangeIntegration.Contracts] --> A[Application: collector / detector / read service]
    A --> D[Domain: CurrentFundingRate]
    A --> C[Contracts: view / integration event]
    A --> I[Infrastructure: repository / Outbox]
    I --> DB[(current_rates)]
    I --> MQ[RabbitMQ via Messaging]
    HTTP[GET FundingRates] --> P[Presentation]
    P --> C
    AR[Arbitrage.Application] -->|ICurrentRatesModule| C
```

| Слой | Главное содержимое |
| --- | --- |
| `Contracts` | `ICurrentRatesModule`, `CurrentRatesQuery`, `CurrentRateView`, `FundingRateChangedIntegrationEvent` |
| `Domain` | `CurrentFundingRate`, включая вычисление `Apr` и `Deviation` |
| `Application` | Таймер и collector, detector изменений, сценарий чтения, repository ports |
| `Infrastructure` | `CurrentRatesDbContext`, repository, Outbox publisher и его фоновый цикл |
| `Presentation` | `FundingRatesController` и mapping в HTTP DTO |

`CurrentRates.Application` использует только `ExchangeIntegration.Contracts` для получения market data. `Arbitrage` читает ставки только через `CurrentRates.Contracts`. Событие для `HistoricalRates` также принадлежит `CurrentRates.Contracts`, но доставляется асинхронно через Outbox и RabbitMQ.

## Периодический сбор

```mermaid
sequenceDiagram
    participant T as CurrentCollectionBackgroundService
    participant C as CurrentFundingRateCollector
    participant DB as CurrentRatesRepository
    participant EI as ExchangeIntegration.Contracts
    participant D as FundingRateChangeDetector
    T->>C: CollectAsync по таймеру
    C->>DB: GetAllRatesAsync (предыдущее состояние)
    par Binance
        C->>EI: GetCurrentRatesAsync(Binance)
        EI-->>C: Snapshots или ошибка
    and Bybit
        C->>EI: GetCurrentRatesAsync(Bybit)
        EI-->>C: Snapshots или ошибка
    and OKX
        C->>EI: GetCurrentRatesAsync(OKX)
        EI-->>C: Snapshots или ошибка
    end
    C->>D: Сравнить NextFundingTime по каждой успешной бирже
    D-->>C: FundingRateChangedIntegrationEvent[]
    loop Каждая успешно опрошенная биржа
        C->>DB: StoreSnapshotAsync(rates, events)
        DB->>DB: Транзакция: insert/update ставок + Outbox events
        DB-->>C: Commit
    end
```

`CurrentRateSnapshot` — наблюдение одной пары, полученное от `ExchangeIntegration`; `StoreSnapshotAsync` сохраняет уже весь успешный набор ставок конкретной биржи. По умолчанию цикл запускается каждые 10 секунд. Биржи опрашиваются параллельно, результаты сохраняются отдельно; ошибка одной биржи не отменяет данные успешных бирж.

Новую задачу истории создаёт новая пара либо изменение `NextFundingTime` у существующей пары. Изменение только `FundingRate` или `FundingIntervalHours` событие не создаёт. Ставки и созданные events коммитятся одной PostgreSQL-транзакцией на биржу: при ошибке записи не сохраняется ни то, ни другое.

При успешном наборе отсутствующая пара деактивируется, только если её `LastSeenOnUtc` старше порога актуальности. Повторное появление пары обновляет timestamp и активирует её. Последние значения остаются в БД.

## Чтение текущих ставок

```mermaid
sequenceDiagram
    participant User as HTTP client / Arbitrage
    participant P as FundingRatesController или ICurrentRatesModule
    participant S as CurrentRatesService
    participant R as CurrentRatesRepository
    participant DB as current_rates.CurrentFundingRates
    User->>P: symbol?, exchanges?
    P->>S: GetCurrentRatesAsync(query)
    S->>R: GetRatesAsync(query)
    R->>DB: IsActive и LastSeenOnUtc >= now - threshold
    DB-->>R: Только актуальные строки
    R-->>S: CurrentFundingRate[]
    S-->>P: CurrentRateView[] с Apr и Deviation
    P-->>User: HTTP DTO или module view
```

Порог `DeactivateMissingAfterMinutes` по умолчанию равен 5 минутам. Проверка свежести выполняется **при каждом чтении**, даже если биржа давно не дала успешный набор и поле `IsActive` в БД ещё равно `true`. Поэтому просроченная ставка не участвует и в расчёте `Arbitrage`.

`CurrentRatesQuery.Symbol` фильтрует по `BaseAsset` (например, `BTC`), а не по полной паре `BTC-USDT`. `Apr` и `Deviation` вычисляет доменная модель на backend; HTTP DTO берёт готовый `Apr`, не рассчитывая его на frontend.

## Публикация Outbox

```mermaid
sequenceDiagram
    participant T as OutboxPublisherBackgroundService
    participant O as OutboxPublisher
    participant DB as current_rates.OutboxMessages
    participant MQ as RabbitMQ
    T->>O: ProcessPendingAsync
    O->>DB: Читать pending по OccurredOnUtc, Id
    loop Каждое событие последовательно
        O->>O: Собрать event из Id, OccurredOnUtc, Exchange, Symbol, NextFundingTime
        O->>MQ: Publish current-rates.events / funding-rate.changed.exchange
        alt Confirmed и не returned
            MQ-->>O: Publisher confirm
            O->>DB: ProcessedOnUtc = now, Error = null
        else Ошибка публикации
            MQ-->>O: No confirm / returned / exception
            O->>DB: Attempts += 1, сохранить Error
        end
    end
```

По умолчанию Outbox опрашивается каждые 2 секунды пакетами до 100 событий. Неотправленная строка остаётся pending и будет выбрана снова. После confirm, но до отметки `ProcessedOnUtc`, возможна повторная публикация. Это `at-least-once`, не `exactly-once`. Текущий dispatcher не использует lease или распределённый захват строк и рассчитан на один экземпляр backend. Дальнейшая доставка и идемпотентное сохранение истории описаны в [сквозном сценарии](../scenarios.md).

## Данные и гарантии

`current_rates."CurrentFundingRates"` уникальна по `(NormalizedSymbol, Exchange)`. `current_rates."OutboxMessages"` содержит `Id`, `OccurredOnUtc`, снимок события в `Exchange`, `Symbol`, `NextFundingTime`, а также `ProcessedOnUtc`, `Attempts` и `Error`. JSON payload не хранится: publisher собирает `FundingRateChangedIntegrationEvent` непосредственно из типизированных колонок. Между двумя schemas нет foreign keys или EF navigation properties. Миграции применяет отдельный `DatabaseMigrator`.

## Основные файлы и проверки

- [Collector](../../../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Application/Collection/CurrentFundingRateCollector.cs) и [detector](../../../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Application/Collection/FundingRateChangeDetector.cs)
- [Доменные расчёты](../../../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Domain/CurrentFundingRate.cs)
- [Repository и транзакция](../../../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Infrastructure/Persistence/CurrentRatesRepository.cs)
- [Outbox publisher](../../../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Infrastructure/Outbox/OutboxPublisher.cs)
- [HTTP controller](../../../src/Modules/CurrentRates/FundingMonitor.CurrentRates.Presentation/Controllers/FundingRatesController.cs)
- [Unit tests](../../../tests/FundingMonitor.CurrentRates.UnitTests/FundingRateChangeDetectorTests.cs) и [integration tests](../../../tests/FundingMonitor.CurrentRates.IntegrationTests/Outbox/CurrentRatesOutboxTests.cs)

См. также [схему БД](../../database-schema.md) и [ADR 0013](../../adr/0013-simple-outbox-without-inbox.md).
