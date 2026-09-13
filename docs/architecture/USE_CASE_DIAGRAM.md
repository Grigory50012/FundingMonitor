# Сценарии использования FundingMonitor

Пользователь выбирает символы и биржи, просматривает текущие ставки, историю, APR и арбитражные возможности. Backend собирает данные в фоне; frontend отображает результаты расчётов.

| Сценарий | HTTP-маршрут | Модуль |
| --- | --- | --- |
| Текущие ставки | `GET /api/v1/FundingRates` | CurrentRates |
| История | `GET /api/v1/History` | HistoricalRates |
| APR статистика | `GET /api/v1/History/apr-stats` | HistoricalRates |
| Арбитражные возможности | `GET /api/v1/Arbitrage` | Arbitrage |
| Состояние бирж | `GET /api/v1/exchanges/health` | ExchangeIntegration |

## Сбор и доставка изменений

```mermaid
sequenceDiagram
    participant C as CurrentRates
    participant E as ExchangeIntegration.Contracts
    participant DB as current_rates
    participant O as Outbox dispatcher
    participant Q as RabbitMQ
    participant H as HistoricalRates
    participant HD as historical_rates
    C->>E: Получить текущие ставки
    E-->>C: Snapshot биржи
    C->>DB: Транзакция: ставки + событие Outbox
    O->>DB: Прочитать ожидающие события
    O->>Q: Публикация в current-rates.events
    Q-->>O: Publisher confirm
    O->>DB: Отметить отправку
    Q->>H: Доставка в очередь биржи
    H->>E: Собрать историю (включая повторную доставку)
    E-->>H: Выплаты
    H->>HD: Атомарный upsert истории
    H->>Q: Manual ACK
```

У каждой биржи отдельная очередь и worker pool, по умолчанию 10 consumers с `prefetch=1`. Повтор того же `EventId` снова вызывает API биржи; upsert не создаёт дубли исторических точек. Backend запускается одним экземпляром с одним последовательным Outbox dispatcher. Временная ошибка ведёт к confirmed retry, permanent error и исчерпанные попытки — к confirmed DLQ. Только после подтверждения новой публикации исходное сообщение получает ACK.

## Зависимости модулей

```mermaid
flowchart TD
    Host[FundingMonitor.ApiHost] --> CP[CurrentRates.Presentation]
    Host --> HP[HistoricalRates.Presentation]
    Host --> AP[Arbitrage.Presentation]
    Host --> EP[ExchangeIntegration.Presentation]
    C[CurrentRates.Application] --> EC[ExchangeIntegration.Contracts]
    H[HistoricalRates.Application] --> EC
    A[Arbitrage.Application] --> CC[CurrentRates.Contracts]
    HI[HistoricalRates.Infrastructure] --> CC
```

Каждый из четырёх модулей имеет `Contracts`, `Domain`, `Application`, `Infrastructure`, `Presentation`. Межмодульные зависимости идут только через Contracts. HistoricalRates использует публичное событие CurrentRates, не его persistence. Arbitrage получает ставки через контракт, выполняет расчёт в памяти и не имеет БД.

## Локальное развёртывание

```mermaid
flowchart LR
    Migrator[DatabaseMigrator] --> PG[(PostgreSQL)]
    Frontend[React / Vite :5173] --> API[ApiHost :5000]
    API --> PG
    API --> MQ[(RabbitMQ :5672)]
    API --> Exchanges[Binance / Bybit / OKX]
```

Compose запускает PostgreSQL и RabbitMQ. Сначала успешно завершается DatabaseMigrator, затем запускается ApiHost. Host сам не меняет схему БД. Проверка `/health` дополняется HTTP-проверками данных и отдельной проверкой RabbitMQ consumers.

Подробности: [архитектура](index.md), [схема БД](../database-schema.md), [развёртывание](../deployment/docker.md).
