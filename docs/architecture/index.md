# Architecture Documentation

## 📐 Диаграммы

| Диаграмма | Формат | Описание |
|-----------|--------|----------|
| [Use Case & Sequence](USE_CASE_DIAGRAM.md) | PlantUML / Mermaid | Варианты использования, последовательности сбора данных, API запросы, компоненты, развёртывание |

## 🏗️ Слои Clean Architecture

```
src/
├── FundingMonitor.Core          # Domain Layer (нет внешних зависимостей)
│   ├── Entities                 # CurrentFundingRate, HistoricalFundingRate, ...
│   ├── Interfaces               # IRepository, IService, IClient
│   └── Events                   # FundingRateChangedEvent
├── FundingMonitor.Application   # Use Cases / Business Logic
│   ├── Services                 # 9 сервисов (Collector, Detector, AprStats, Arbitrage, ...)
│   └── BackgroundServices       # 2 Hosted Services
├── FundingMonitor.Infrastructure # External Concerns
│   ├── Data                     # EF Core, Repositories, Migrations
│   ├── ExchangeClients          # Binance, Bybit, OKX реализации
│   └── Queues                   # RedisHistoryTaskQueue
└── FundingMonitor.Api           # Presentation
    ├── Controllers              # FundingRates, History, Exchanges, Arbitrage
    ├── DTOs                     # Request/Response модели
    └── Middleware               # ExceptionHandling (ProblemDetails)
```

**Dependency Rule**: `Api` → `Application` → `Core` ← `Infrastructure`

## 🔄 Основные потоки данных

### 1. Сбор текущих ставок (каждые 10 сек)
```
BackgroundService → CurrentFundingRateCollector
    ├─→ BinanceClient
    ├─→ BybitClient
    └─→ OkxClient
         ↓ (parallel Task.WhenAll)
    → FundingRateChangeDetector (DetectChangesAsync)
         ↓ (если есть изменения)
    → HistoricalCollectionProducer (ProduceTasksAsync → Redis LPUSH)
    → CurrentFundingRateRepository (BulkInsertOrUpdate → PostgreSQL)
    → FundingArbitrageDetector (DetectOpportunities → FundingArbitrageService)
```

### 2. Сбор исторических данных (event-driven)
```
HistoricalBackgroundService → RedisQueue (BRPOP)
    → HistoricalFundingRateCollector → ExchangeClient
         ↓ (SemaphoreSlim MaxConcurrentTasks)
    → HistoricalFundingRateRepository (BulkInsert → PostgreSQL)
```

### 3. API запрос (Frontend → Backend)
```
GET /api/v1/FundingRates?symbol=BTC&exchanges=Binance,Bybit
    → ExceptionHandlingMiddleware
    → FundingRatesController.GetFundingRates()
    → ICurrentFundingRateRepository.GetRatesAsync()
    → PostgreSQL (Index Seek на NormalizedSymbol+Exchange)
    → Map to DTO → JSON Response
```

## 📦 Деплой

**Docker Compose** поднимает только инфраструктуру (PostgreSQL + Redis). API и фронтенд запускаются локально:

```
┌─────────────────────────────────────┐
│         Docker Network              │
│  ┌──────────┐  ┌────────┐           │
│  │Postgres  │  │ Redis  │           │
│  │  :5432   │  │ :6379  │           │
│  └──────────┘  └────────┘           │
└─────────────────────────────────────┘
         ▲              ▲
         │              │
┌────────┴──────────────┴─────────────┐
│  FundingMonitor.Api (dotnet run)    │
│  localhost:5000                     │
│  + 2 Background Services            │
└─────────────────────────────────────┘
         │
    ┌────┴────┐    ┌────┴────┐
    │ Binance │    │  Bybit  │  (+ OKX)
    │   API   │    │   API   │
    └─────────┘    └─────────┘
```

Фронтенд (`npm run dev`, :5173) проксирует `/api` на `localhost:5000` через Vite.

См. [deployment/docker.md](../deployment/docker.md) для деталей.

## 🔗 Связанная документация

- [ADR Index](../adr/index.md) — 8 архитектурных решений
- [Database Schema](../database-schema.md) — таблицы, индексы, performance
- [Frontend Components](../frontend/components/index.md) — 7 React компонентов