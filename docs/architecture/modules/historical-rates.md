# HistoricalRates

`HistoricalRates` получает задания из RabbitMQ, запрашивает историю funding rates через `ExchangeIntegration`, сохраняет её идемпотентным upsert и предоставляет HTTP API истории и APR statistics. Модуль владеет schema `historical_rates` и не читает таблицы `CurrentRates`.

## Граница и слои

```mermaid
flowchart LR
    CR[CurrentRates.Contracts event] --> I[Infrastructure: RabbitMQ consumers]
    I --> A[Application: handler / classifier / read service]
    A --> EI[ExchangeIntegration.Contracts]
    A --> D[Domain: HistoricalFundingRate]
    A --> P[Application persistence ports]
    P --> DB[(historical_rates)]
    HTTP[GET History / apr-stats] --> PR[Presentation]
    PR --> C[HistoricalRates.Contracts]
    C --> A
```

| Слой | Главное содержимое |
| --- | --- |
| `Contracts` | `IHistoricalRatesModule`, history query/view и `AprPeriodStatsView` |
| `Domain` | `HistoricalFundingRate` — одна выплата конкретной биржи и пары |
| `Application` | Сбор истории, классификация сбоев, чтение истории и расчёт APR |
| `Infrastructure` | RabbitMQ topology и workers, retry/DLQ, PostgreSQL repository и health check |
| `Presentation` | `HistoryController` и mapping HTTP DTO |

`HistoricalRates.Application` обращается к биржам только через `ExchangeIntegration.Contracts`. Ссылка на `CurrentRates.Contracts` ограничена Infrastructure: consumer десериализует публичный `FundingRateChangedIntegrationEvent` и преобразует его во внутренний `HistoricalCollectionRequest`.

## Topology и workers

```mermaid
flowchart LR
    X[current-rates.events<br/>durable direct exchange]
    X -->|funding-rate.changed.binance| B[historical-rates.collect.binance]
    X -->|funding-rate.changed.bybit| Y[historical-rates.collect.bybit]
    X -->|funding-rate.changed.okx| O[historical-rates.collect.okx]
    X -->|historical-rates.dead| D[historical-rates.dead]
    B --> BW[Binance worker pool]
    Y --> YW[Bybit worker pool]
    O --> OW[OKX worker pool]
```

Все очереди — durable classic queues. По умолчанию на каждую биржу создаётся 10 workers; значение можно переопределить через `HistoricalDataCollectionOptions.ExchangeConcurrency`. Каждый worker имеет собственный channel, `prefetch=1` и manual ACK. Поэтому один worker одновременно держит не более одного unacked сообщения, а блокировка Binance не занимает workers Bybit и OKX.

Topology создаётся при старте до запуска consumers. `/health` пассивно проверяет exchange и четыре очереди с timeout 5 секунд; он не проверяет bindings, количество consumers или API бирж.

## Успешная обработка

```mermaid
sequenceDiagram
    participant Q as Очередь биржи
    participant W as HistoricalCollectionBackgroundService
    participant P as HistoricalMessageProcessor
    participant H as HistoricalEventHandler
    participant EI as ExchangeIntegration.Contracts
    participant DB as historical_rates
    Q->>W: FundingRateChangedIntegrationEvent
    W->>W: Проверить envelope, version, type, EventId и exchange
    W->>P: ProcessAsync(event, retryCount)
    P->>H: HandleAsync(HistoricalCollectionRequest)
    H->>EI: История за MaxHistoryMonths
    EI-->>H: HistoricalRatePoint[]
    H->>DB: Atomic batch upsert
    DB-->>H: Commit
    H-->>P: Успех
    P-->>W: Ack
    W->>Q: BasicAck
```

`NextFundingTime` в событии служит триггером на стороне `CurrentRates`, но `HistoricalRates` не восстанавливает состояние на этот момент. Handler запрашивает актуальную историю от `now - MaxHistoryMonths` до `now`; по умолчанию это один месяц и до 1000 точек за запрос.

Inbox отсутствует. Повторная доставка того же `EventId` снова вызывает API биржи. Batch upsert по `(Exchange, NormalizedSymbol, FundingTime)` атомарно вставляет новые точки и обновляет `FundingRate`/`CollectedOnUtc` существующих. Пустой успешный ответ не выполняет SQL и также допускает ACK.

## Retry, DLQ и malformed messages

```mermaid
sequenceDiagram
    participant Q as Исходная очередь
    participant P as HistoricalMessageProcessor
    participant X as current-rates.events
    participant D as historical-rates.dead
    Q->>P: Сообщение с x-retry-count
    alt Transient error и retryCount < MaxRetries
        P->>P: Wait RetryDelaySeconds
        P->>X: Publish копию в routing key биржи
        X-->>P: Confirmed и не returned
        P->>Q: ACK original
    else Permanent error или retries исчерпаны
        P->>X: Publish копию в historical-rates.dead
        X-->>P: Confirmed и не returned
        P->>Q: ACK original
    else Replacement не подтверждён или shutdown
        P-->>Q: Оставить original unacked
    end
```

По умолчанию разрешено 3 retries с задержкой 30 секунд. Timeout, HTTP 408/429/5xx, rate limit и прочие availability failures повторяются. Ошибки validation/not found/conflict, authentication и geographic restriction сразу направляются в DLQ. Некорректный JSON или envelope публикуется в DLQ как `MalformedHistoricalMessage` с исходным body в Base64. Исходное сообщение получает ACK только после confirmed-публикации replacement; иначе channel закрывается, и broker возвращает unacked delivery.

Worker пересоздаёт channel после shutdown и после `AsyncEventingBasicConsumer.UnregisteredAsync`. Второй сигнал важен при `basic.cancel`: удаление очереди может отменить consumer, не закрывая channel. После неожиданного disconnect worker ждёт 5 секунд перед новой попыткой.

## Чтение истории

```mermaid
sequenceDiagram
    participant User as HTTP client
    participant C as HistoryController
    participant S as HistoricalRatesService
    participant R as HistoricalRatesRepository
    participant DB as historical_rates.HistoricalFundingRates
    User->>C: GET /api/v1/History?symbol=...
    C->>S: HistoricalRatesQuery
    S->>R: GetHistoryAsync
    R->>DB: symbol + exchanges? + from? + to?, newest first
    DB-->>R: Не более limit строк
    R-->>S: HistoricalFundingRate[]
    S-->>C: HistoricalRateView[]
    C-->>User: HistoricalFundingRateDto[]
```

`symbol` обязателен. HTTP limit по умолчанию равен 1000 и ограничивается сверху 1000. Результат сортируется от новых выплат к старым.

## APR statistics

```mermaid
sequenceDiagram
    participant User as HTTP client
    participant C as HistoryController
    participant S as HistoricalRatesService
    participant Cache as IMemoryCache
    participant DB as historical_rates
    User->>C: GET /api/v1/History/apr-stats?symbol=...
    C->>S: GetAprStatsAsync(symbol, exchanges?)
    S->>Cache: Ключ symbol + отсортированный список exchanges
    alt Cache miss
        S->>DB: История до MaxHistoryDays, limit 10000
        DB-->>S: Выплаты по биржам
        S->>S: Периоды 1,2,3,7,14,21,30 дней
        S->>Cache: Сохранить результат на 5 минут
    end
    S-->>C: AprPeriodStatsView[]
    C-->>User: AprPeriodStatsDto[]
```

Расчёт выполняется на backend отдельно для каждой биржи. Для периода `days` берутся выплаты последних доступных календарных дат, после чего:

```text
TotalFundingRatePercent = Sum(FundingRate) × 100
Apr = TotalFundingRatePercent × 365 / days
AvgFundingRatePercent = TotalFundingRatePercent / PaymentsCount
StdDev = population standard deviation FundingRate × 100
```

Если доступно меньше календарных дней, знаменатель APR всё равно равен запрошенному `days`; это закреплённая legacy formula. Историческая запись не содержит funding interval, поэтому расчёт не использует `FundingIntervalHours`. Кэш живёт в памяти одного процесса и по умолчанию действует 5 минут.

## Основные файлы и проверки

- [RabbitMQ workers](../../../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Infrastructure/Messaging/HistoricalCollectionBackgroundService.cs)
- [Message processor](../../../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Infrastructure/Messaging/HistoricalMessageProcessor.cs)
- [Сбор истории](../../../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Application/HistoricalEventHandler.cs)
- [Failure classifier](../../../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Application/HistoricalCollectionFailureClassifier.cs)
- [Repository и upsert](../../../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Infrastructure/Persistence/HistoricalRatesRepository.cs)
- [Чтение и APR](../../../src/Modules/HistoricalRates/FundingMonitor.HistoricalRates.Application/HistoricalRatesService.cs)
- [Unit tests](../../../tests/FundingMonitor.HistoricalRates.UnitTests/HistoricalMessageProcessorTests.cs) и [integration tests](../../../tests/FundingMonitor.HistoricalRates.IntegrationTests/Persistence/HistoricalPersistenceTests.cs)

См. также [сквозные сценарии](../scenarios.md), [схему БД](../../database-schema.md), [ADR 0010](../../adr/0010-rabbitmq-for-parallel-historical-collection.md) и [ADR 0013](../../adr/0013-simple-outbox-without-inbox.md).
