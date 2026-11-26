# CurrentRates

`CurrentRates` периодически собирает текущие funding rates, хранит последнее состояние каждой пары, исключает устаревшие ставки из выдачи и публикует задания на исторический сбор. Модуль владеет schema `current_rates`, но не обращается к schema истории.

## Сценарии и границы

```mermaid
flowchart LR
    HTTP[GET FundingRates] --> R[GetCurrentRates]
    AR[Arbitrage] -->|ICurrentRatesModule| R
    C[CollectCurrentRates] --> EI[ExchangeIntegration.Contracts]
    R --> DB[(current_rates)]
    C -->|ставки и Outbox в одной транзакции| DB
    P[PublishPendingEvents] -->|чтение и отметка Outbox| DB
    P --> MQ[RabbitMQ]
```

| Каталог | Главное содержимое |
| --- | --- |
| `GetCurrentRates` | Контроллер, DTO и чтение ставок через `ICurrentRatesModule` |
| `CollectCurrentRates` | Независимые циклы опроса, сборщик и настройки |
| `PublishPendingEvents` | Издатель Outbox, фоновый цикл и настройки |
| `Persistence` | Общие DbContext, репозиторий, EF-сущности и миграции |

Три среза находятся в одном проекте `FundingMonitor.CurrentRates`. `ApiHost` подключает модуль целиком, а `DatabaseMigrator` регистрирует только БД. Отдельный `CurrentRates.Contracts` содержит интерфейс чтения для `Arbitrage` и тип события для `HistoricalRates`. Сборщик получает котировки через `ExchangeIntegration.Contracts`.

## Периодический сбор CollectCurrentRates

`CurrentCollectionBackgroundService` запускает отдельный цикл для Binance, Bybit и OKX. У каждого цикла свой таймер и DI scope на опрос. В `ApiHost/appsettings.json` интервал настроен на 5 секунд. Следующий опрос биржи начинается после завершения предыдущего. Готовый снимок сохраняется сразу, пока остальные биржи могут ещё отвечать.

```mermaid
sequenceDiagram
    participant T as Цикл одной биржи
    participant C as CurrentFundingRateCollector
    participant DB as CurrentRatesRepository
    participant EI as ExchangeIntegration.Contracts
    T->>C: CollectAsync(exchange) в отдельном scope
    C->>EI: GetCurrentRatesAsync(exchange)
    alt Успешный опрос
        EI-->>C: Снимок ставок
        C->>DB: StoreSnapshotAsync(exchange, rates)
        DB->>DB: В транзакции прочитать строки биржи и сравнить NextFundingTime
        DB->>DB: Записать ставки и Outbox
        DB-->>C: Commit и число записанных событий
    else Ошибка опроса
        EI-->>C: Ошибка
        C-->>T: Завершить текущую попытку
    end
```

`StoreSnapshotAsync` читает предыдущий набор только для своей биржи. Те же строки используются для сравнения времени платежа, обновления и деактивации. Запрос к API биржи завершается до открытия транзакции. Циклы обновляют отдельные группы строк в общей таблице по ключу `(NormalizedSymbol, Exchange)`. После коммита сборщик логирует биржу, число ставок и событий и длительность цикла.

Новую задачу истории создаёт новая пара либо изменение `NextFundingTime` у существующей пары. Изменение только `FundingRate` или `FundingIntervalHours` событие не создаёт. Ставки и созданные events коммитятся одной PostgreSQL-транзакцией на биржу: при ошибке записи не сохраняется ни то, ни другое.

При успешном наборе отсутствующая пара деактивируется, только если её `LastSeenOnUtc` старше порога актуальности. Повторное появление пары обновляет timestamp и активирует её. Последние значения остаются в БД.

## Чтение текущих ставок

В срезе `GetCurrentRates` контроллер разбирает HTTP-параметры и формирует DTO. Контроллер и `Arbitrage` используют `ICurrentRatesModule`, который реализует `CurrentRatesReader`. Чтение ставок и транзакционная запись снимка используют общий `ICurrentRatesRepository`.

```mermaid
sequenceDiagram
    participant User as HTTP client
    participant AR as Arbitrage
    participant P as FundingRatesController
    participant S as ICurrentRatesModule / CurrentRatesReader
    participant R as CurrentRatesRepository
    participant DB as current_rates.CurrentFundingRates
    alt HTTP
        User->>P: symbol?, exchanges?
        P->>S: GetCurrentRatesAsync(query)
    else Arbitrage
        AR->>S: GetCurrentRatesAsync(query)
    end
    S->>R: GetRatesAsync(query)
    R->>DB: IsActive и LastSeenOnUtc >= now - threshold
    DB-->>R: Актуальные строки
    R-->>S: CurrentFundingRate[]
    alt HTTP
        S-->>P: CurrentRateView[] с Apr и Deviation
        P-->>User: FundingRateDto[]
    else Arbitrage
        S-->>AR: CurrentRateView[]
    end
```

Порог `DeactivateMissingAfterMinutes` по умолчанию равен 5 минутам. Проверка свежести выполняется **при каждом чтении**, даже если биржа давно не дала успешный набор и поле `IsActive` в БД ещё равно `true`. Поэтому просроченная ставка не участвует и в расчёте `Arbitrage`.

`CurrentRatesQuery.Symbol` фильтрует по `BaseAsset` (например, `BTC`), а не по полной паре `BTC-USDT`. `Apr` и `Deviation` вычисляет доменная модель на backend; HTTP DTO берёт готовый `Apr`, не рассчитывая его на frontend.

## Публикация Outbox

Хранимую запись `OutboxMessage` используют сборщик при записи ставок и издатель при отправке.

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

- [Циклы сбора](../../../src/Modules/CurrentRates/CollectCurrentRates/CurrentCollectionBackgroundService.cs) и [сборщик](../../../src/Modules/CurrentRates/CollectCurrentRates/CurrentFundingRateCollector.cs)
- [Доменные расчёты](../../../src/Modules/CurrentRates/CurrentFundingRate.cs)
- [Repository и транзакция](../../../src/Modules/CurrentRates/Persistence/CurrentRatesRepository.cs)
- [Цикл публикации](../../../src/Modules/CurrentRates/PublishPendingEvents/OutboxPublisherBackgroundService.cs) и [обработка ожидающих событий](../../../src/Modules/CurrentRates/PublishPendingEvents/OutboxPublisher.cs)
- [HTTP-сценарий чтения](../../../src/Modules/CurrentRates/GetCurrentRates/FundingRatesController.cs) и [реализация межмодульного контракта](../../../src/Modules/CurrentRates/GetCurrentRates/CurrentRatesReader.cs)
- [Проверки независимости циклов](../../../tests/FundingMonitor.CurrentRates.UnitTests/CurrentFundingRateCollectorTests.cs) и [интеграционные тесты записи](../../../tests/FundingMonitor.CurrentRates.IntegrationTests/Outbox/CurrentRatesOutboxTests.cs)

См. также [схему БД](../../database-schema.md) и [ADR 0013](../../adr/0013-simple-outbox-without-inbox.md).
