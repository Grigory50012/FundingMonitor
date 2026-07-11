# Architecture Documentation

## Backend Layers

```text
src/
├── FundingMonitor.Core
│   ├── Entities
│   ├── Interfaces
│   ├── Configuration
│   ├── Events
│   └── Results
├── FundingMonitor.Application
│   ├── Services
│   └── BackgroundServices
├── FundingMonitor.Infrastructure
│   ├── Data
│   ├── ExchangeClients
│   └── Queues
└── FundingMonitor.Api
    ├── Controllers
    ├── Models
    ├── Mappers
    ├── Middleware
    └── Extensions
```

Dependency rule:

```text
Api -> Application -> Core
Api -> Infrastructure -> Core
Application -> Core
Infrastructure -> Core
```

`Core` не зависит от внешних слоёв. `Infrastructure` реализует интерфейсы из `Core`. `Api` собирает зависимости и публикует HTTP surface.

## Data Flows

### Current rates

```text
CurrentCollectionBackgroundService
-> CurrentFundingRateCollector
-> Binance/Bybit/OKX exchange clients
-> FundingRateChangeDetector
-> HistoricalCollectionProducer
-> RedisHistoryTaskQueue
-> CurrentFundingRateRepository
-> PostgreSQL
```

Интервал обновления задаётся в `CurrentDataCollectionOptions.UpdateIntervalSeconds`, сейчас `10` секунд.

### Historical rates

```text
HistoricalCollectionBackgroundService
-> RedisHistoryTaskQueue
-> HistoricalFundingRateCollector
-> exchange client
-> HistoricalFundingRateRepository
-> PostgreSQL
```

Ограничения сбора задаются в `HistoricalDataCollectionOptions`: `MaxConcurrentTasks`, `ApiPageSize`, `MaxHistoryMonths`, `MaxRetries`.

### API request

```text
Frontend
-> /api/v1/*
-> ExceptionHandlingMiddleware
-> Controller
-> repository/service
-> DTO mapper
-> JSON response
```

## API Surface

| Controller | Route | Назначение |
| --- | --- | --- |
| `FundingRatesController` | `GET /api/v1/FundingRates` | Текущие funding rates |
| `HistoryController` | `GET /api/v1/History` | Исторические funding rates |
| `HistoryController` | `GET /api/v1/History/apr-stats` | APR statistics |
| `ArbitrageController` | `GET /api/v1/Arbitrage` | Funding arbitrage opportunities |
| `ExchangesController` | `GET /api/v1/exchanges/health` | Health status бирж |

## Frontend Architecture

```text
frontend/FundingMonitor.Web/src/
├── api
├── config
├── entities
├── features
│   ├── arbitrage
│   ├── current-rates
│   ├── filters
│   └── history
├── hooks
├── shared
│   ├── api
│   ├── lib
│   └── ui
├── types
└── widgets
    └── dashboard
```

`DashboardPage` находится в `widgets/dashboard` и собирает feature-компоненты. Данные загружаются через existing custom hooks: `useCurrentRates`, `useHistoryRates`, `useArbitrageRates`, `useAprStats`. Runtime state/data-fetching library вроде Redux, Zustand или TanStack Query пока не используется.

## Deployment View

Docker Compose поднимает только инфраструктуру:

```text
PostgreSQL :5432
Redis      :6379
```

API и frontend запускаются локально:

```bash
dotnet run --project src/FundingMonitor.Api
cd frontend/FundingMonitor.Web
npm run dev
```

Vite proxy отправляет `/api` на backend. Подробности: [Docker Deployment](../deployment/docker.md).

## Связанная документация

- [Vault Index](../Vault%20Index.md)
- [ADR Index](../adr/index.md)
- [Database Schema](../database-schema.md)
- [Frontend Components](../frontend/components/index.md)
- [Docker Deployment](../deployment/docker.md)
