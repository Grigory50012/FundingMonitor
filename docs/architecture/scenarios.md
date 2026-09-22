# Сквозные сценарии FundingMonitor

Здесь собраны только потоки, затрагивающие несколько модулей или внешних компонентов. Внутренние сценарии находятся на страницах соответствующих [модулей](modules/index.md).

## Сбор текущих ставок и запуск исторического сбора

```mermaid
sequenceDiagram
    participant Timer as CurrentRates timer
    participant CR as CurrentRates
    participant EI as ExchangeIntegration.Contracts
    participant DB as current_rates
    participant O as Outbox dispatcher
    participant Q as RabbitMQ
    participant HR as HistoricalRates
    participant HDB as historical_rates
    Timer->>CR: Запустить цикл сбора
    CR->>EI: Получить текущие ставки биржи
    EI-->>CR: Нормализованный snapshot
    CR->>DB: Транзакция: insert/update ставок + Outbox events
    O->>DB: Прочитать ожидающие events
    DB-->>O: Типизированные поля события
    O->>Q: Publish по routing key биржи
    Q-->>O: Publisher confirm
    O->>DB: Установить ProcessedOnUtc
    Q->>HR: Доставить collection task
    HR->>EI: Запросить актуальную историю
    EI-->>HR: Исторические выплаты
    HR->>HDB: Atomic upsert исторических точек
    HR->>Q: Manual ACK
```

Outbox закрывает окно потери между PostgreSQL commit и RabbitMQ publish. Единственный тип события хранится в колонках `Id`, `OccurredOnUtc`, `Exchange`, `Symbol` и `NextFundingTime`; JSON payload отсутствует. Сбой после publisher confirm, но до установки `ProcessedOnUtc`, может привести к повторной публикации. Inbox отсутствует: повторная доставка снова вызывает API биржи, а уникальность хранения защищает upsert по `(Exchange, NormalizedSymbol, FundingTime)`.

## Ошибка исторического сбора

```mermaid
sequenceDiagram
    participant Q as Exchange queue
    participant HR as HistoricalRates worker
    participant EI as ExchangeIntegration.Contracts
    participant X as current-rates.events
    participant D as historical-rates.dead
    Q->>HR: Collection task
    HR->>EI: Запросить историю
    EI-->>HR: Ошибка
    alt Временная ошибка и попытки остались
        HR->>HR: Wait RetryDelaySeconds
        HR->>X: Publish копию в routing key биржи
        X-->>HR: Publisher confirm
        HR->>Q: ACK исходного сообщения
    else Permanent error или попытки исчерпаны
        HR->>D: Confirmed DLQ publish
        D-->>HR: Publisher confirm
        HR->>Q: ACK исходного сообщения
    else Публикация retry/DLQ не подтверждена
        HR--xQ: Исходное сообщение остаётся unacked
    end
```

Биржи имеют независимые очереди и worker pools с `prefetch=1`. Ошибка одной интеграции не занимает workers остальных бирж.

## Чтение данных пользователем

```mermaid
flowchart LR
    UI[React frontend] -->|GET FundingRates| CR[CurrentRates]
    UI -->|GET History / APR| HR[HistoricalRates]
    UI -->|GET Arbitrage| AR[Arbitrage]
    UI -->|GET exchanges/health| EI[ExchangeIntegration]
    AR -->|ICurrentRatesModule| CR
    CR --> CRDB[(current_rates)]
    HR --> HRDB[(historical_rates)]
    EI --> EX[Exchange APIs]
```

APR, spread и freshness рассчитывает backend. Frontend получает готовые business values и отвечает за отображение, фильтрацию и сортировку.

## Локальный runtime

```mermaid
flowchart LR
    Migrator[DatabaseMigrator] --> PG[(PostgreSQL :5432)]
    Frontend[React / Vite :5173] --> API[ApiHost]
    API --> PG
    API --> MQ[(RabbitMQ :5672)]
    API --> EX[Binance / Bybit / OKX]
```

Сначала применяется `DatabaseMigrator`, затем запускается один экземпляр `ApiHost`. `/health` проверяет зарегистрированные health checks, но HTTP endpoints и число RabbitMQ consumers проверяются отдельно.
