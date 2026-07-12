# Диаграмма использования FundingMonitor

## Use Case Diagram

```plantuml
left to right direction
skinparam packageStyle rectangle
skinparam actorStyle awesome

actor "Пользователь (трейдер)" as User

rectangle "Система мониторинга арбитражных возможностей\nна криптовалютных биржах" {

    ' === Выбор параметров (основные действия пользователя) ===
    usecase "Выбор бирж для\nмониторинга" as UC1
    usecase "Выбор криптовалют\n(символов)" as UC2

    ' === Автоматическое отображение данных на основе выбора ===
    usecase "Просмотр текущих ставок\nфинансирования" as UC3
    usecase "Просмотр арбитражных\nвозможностей" as UC4
    usecase "Просмотр исторических\nданных" as UC5

    ' === Детализация исторических данных ===
    usecase "Настройка периода\nотображения" as UC12
    usecase "Визуализация графиков" as UC13
    usecase "Просмотр APR-статистики" as UC14
}

' === Связи актера с основными вариантами использования (ВЫБОР) ===
User -- UC1 : "Выбирает"
User -- UC2 : "Выбирает"

' === Выбор влияет на отображение данных ===
UC1 ..> UC3 : <<include>>
UC1 ..> UC4 : <<include>>
UC1 ..> UC5 : <<include>>
UC2 ..> UC3 : <<include>>
UC2 ..> UC4 : <<include>>
UC2 ..> UC5 : <<include>>


' === Детализация исторических данных ===
UC5 -- UC12
UC5 -- UC13
UC5 -- UC14
UC12 .> UC5 : <<extend>>
UC13 .> UC5 : <<extend>>
UC14 .> UC5 : <<extend>>
```

## Детальное описание сценариев использования

### 1. Пользовательские сценарии (Frontend → API)

| ID      | Сценарий                          | Actor        | Описание                                                                |
| ------- | --------------------------------- | ------------ | ----------------------------------------------------------------------- |
| **UC1** | Получить текущие ставки           | Пользователь | GET `/api/v1/FundingRates` - получение актуальных ставок по всем биржам |
| **UC2** | Получить исторические данные      | Пользователь | GET `/api/v1/History` - получение истории ставок за период              |
| **UC3** | Получить APR статистику           | Пользователь | GET `/api/v1/History/apr-stats` — расчёт APR за 1/2/3/7/14/21/30 дней |
| **UC4** | Проверить доступность бирж        | Пользователь | GET `/api/v1/exchanges/health` — проверка доступности API бирж          |
| **UC5** | Просмотр арбитражных возможностей | Пользователь | GET `/api/v1/Arbitrage` — разницы ставок между биржами, сортировка по APR |

### 2. Фоновые сценарии (Background Services)

| ID       | Сценарий                       | Actor   | Описание                                                                                |
| -------- | ------------------------------ | ------- | --------------------------------------------------------------------------------------- |
| **UC10** | Сбор текущих ставок            | Система | `CurrentCollectionBackgroundService` - опрос бирж каждые 10 секунд                      |
| **UC11** | Детектирование изменений       | Система | `FundingRateChangeDetector` - обнаружение новых символов или изменений времени выплаты  |
| **UC12** | Создание задач сбора истории   | Система | `HistoricalCollectionProducer` - создание задач для Redis очереди при изменении ставок  |
| **UC13** | Сбор исторических данных       | Система | `HistoricalCollectionBackgroundService` - обработка очереди с ограничением параллелизма |
| **UC14** | Поиск арбитражных возможностей | Система | `FundingArbitrageDetector` + `FundingArbitrageService` — анализ разниц ставок между биржами |
| **UC15** | Расчёт APR статистики          | Система | `AprStatsService` - вычисление годовой процентной ставки с кэшированием                 |

### 3. Инфраструктурные сценарии

| ID       | Сценарий                 | Actor   | Описание                                                         |
| -------- | ------------------------ | ------- | ---------------------------------------------------------------- |
| **UC20** | Сохранение в PostgreSQL  | Система | EF Core Bulk Extensions для эффективной записи данных            |
| **UC21** | Очередь задач Redis      | Система | Персистентная очередь для надёжного хранения задач сбора истории |
| **UC22** | Получение данных Binance | Система | Binance.Net API - получение текущих и исторических ставок        |
| **UC23** | Получение данных Bybit   | Система | Bybit.Net API - получение текущих и исторических ставок          |
| **UC24** | Получение данных OKX     | Система | JK.OKX.Net API - получение текущих и исторических ставок         |

## Sequence Diagram - Сбор текущих данных

```mermaid
sequenceDiagram
    participant BS as BackgroundService
    participant C as CurrentFundingRateCollector
    participant B as BinanceClient
    participant Y as BybitClient
    participant O as OkxClient
    participant D as ChangeDetector
    participant P as HistoryProducer
    participant R as CurrentRepository
    participant DB as PostgreSQL

    BS->>C: CollectAsync()
    par Параллельный опрос бирж
        C->>B: GetCurrentFundingRatesAsync()
        C->>Y: GetCurrentFundingRatesAsync()
        C->>O: GetCurrentFundingRatesAsync()
    end
    B-->>C: List<FundingRate>
    Y-->>C: List<FundingRate>
    O-->>C: List<FundingRate>

    C->>D: DetectChangesAsync(rates)
    D-->>C: List<FundingRateChangedEvent>

    alt Если есть изменения
        C->>P: ProduceTasksAsync(events)
        P->>R: EnqueueAsync(task)
        R->>R: Redis List Push
    end

    C->>R: UpdateAsync(rates)
    R->>DB: BulkInsertOrUpdate()
```

## Sequence Diagram - API запрос

```mermaid
sequenceDiagram
    participant U as User (Frontend)
    participant M as ExceptionMiddleware
    participant C as FundingRatesController
    participant R as CurrentRepository
    participant DB as PostgreSQL

    U->>M: GET /api/v1/FundingRates
    M->>C: GetRatesAsync(exchanges, symbols)
    C->>R: GetRatesAsync(exchanges, symbols)
    R->>DB: SELECT * FROM "CurrentFundingRate"
    DB-->>R: List<CurrentFundingRateDb>
    R-->>C: List<CurrentFundingRate>
    C->>C: Map to DTO
    C-->>M: List<FundingRateDto>
    M-->>U: JSON Response (200 OK)
```

## Sequence Diagram - Сбор исторических данных

```mermaid
sequenceDiagram
    participant BS as HistoricalBackgroundService
    participant Q as RedisQueue
    participant H as HistoricalCollector
    participant B as BinanceClient
    participant DB as HistoricalRepository
    participant PG as PostgreSQL

    BS->>Q: DequeueAsync()
    Q-->>BS: HistoricalCollectionTask

    par Ограничение параллелизма (SemaphoreSlim)
        BS->>H: CollectAsync(task)
        H->>B: GetHistoricalFundingRatesAsync()
        B-->>H: List<HistoricalFundingRate>
        H->>DB: InsertAsync(rates)
        DB->>PG: BulkInsertOrUpdate()
    end
```

## Компонентная диаграмма

```mermaid
classDiagram
    class "FundingMonitor.Api" {
        +FundingRatesController
        +HistoryController
        +ArbitrageController
        +ExchangesController
        +ExceptionHandlingMiddleware
        +DTOs
    }

    class "FundingMonitor.Application" {
        +CurrentFundingRateCollector
        +HistoricalFundingRateCollector
        +FundingRateChangeDetector
        +HistoricalCollectionProducer
        +AprStatsService
        +FundingArbitrageDetector
        +FundingArbitrageService
        +CurrentCollectionBackgroundService
        +HistoricalCollectionBackgroundService
    }

    class "FundingMonitor.Core" {
        +CurrentFundingRate (Entity)
        +HistoricalFundingRate (Entity)
        +ICurrentFundingRateRepository
        +IHistoricalFundingRateRepository
        +FundingRateChangedEvent
    }

    class "FundingMonitor.Infrastructure" {
        +FundingMonitorDbContext
        +CurrentFundingRateRepository
        +HistoricalFundingRateRepository
        +BinanceFundingRateClient
        +BybitFundingRateClient
        +OkxFundingRateClient
        +RedisHistoryTaskQueue
    }

    class "External Services" {
        <<external>>
        Binance API
        Bybit API
        OKX API
    }

    class "Data Stores" {
        <<database>>
        PostgreSQL
        Redis
    }

    FundingMonitor.Api --> FundingMonitor.Application
    FundingMonitor.Application --> FundingMonitor.Core
    FundingMonitor.Infrastructure --> FundingMonitor.Core
    FundingMonitor.Api --> FundingMonitor.Infrastructure
    FundingMonitor.Infrastructure --> "External Services"
    FundingMonitor.Infrastructure --> "Data Stores"
```

## Развёртывание (локальная разработка)

```mermaid
graph TB
    subgraph "Docker Compose (инфраструктура)"
        PG[(PostgreSQL<br/>17)]
        Redis[(Redis<br/>7)]
    end

    subgraph "Локально (dotnet run)"
        API[FundingMonitor.Api<br/>.NET 10]
        BG1[CurrentCollection<br/>BackgroundService]
        BG2[HistoricalCollection<br/>BackgroundService]
    end

    subgraph "External"
        Binance[Binance API]
        Bybit[Bybit API]
        OKX[OKX API]
    end

    subgraph "Client"
        Frontend[React Frontend<br/>Vite + TypeScript<br/>:5173]
    end

    Frontend -->|HTTP/REST /api| API
    API --> PG
    API --> Redis
    BG1 -->|Query| Binance
    BG1 -->|Query| Bybit
    BG1 -->|Query| OKX
    BG2 -->|Dequeue| Redis
    BG2 -->|Query| Binance
    BG2 -->|Query| Bybit
    BG2 -->|Query| OKX
    BG1 --> PG
    BG2 --> PG
```

## Сводная таблица акторов и сценариев

| Актор                             | Сценарии использования             |
| --------------------------------- | ---------------------------------- |
| **Пользователь (Frontend)**       | UC1, UC2, UC3, UC4, UC5, UC6       |
| **Система (Background Services)** | UC10, UC11, UC12, UC13, UC14, UC15 |
| **Внешние биржи**                 | UC22, UC23, UC24                   |
| **PostgreSQL**                    | UC20                               |
| **Redis**                         | UC21                               |
