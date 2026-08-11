# Architecture Documentation

## Модули и слои backend

Backend остаётся единым приложением с четырьмя проектами Clean Architecture. Внутри каждого проекта код организован сначала по бизнес-модулю, затем по технической роли.

```text
src/
├── FundingMonitor.Core
│   ├── CurrentRates
│   ├── HistoricalRates
│   └── Shared
├── FundingMonitor.Application
│   ├── CurrentRates
│   └── HistoricalRates
├── FundingMonitor.Infrastructure
│   ├── CurrentRates
│   ├── HistoricalRates
│   ├── Shared
│   └── Migrations
└── FundingMonitor.Api
    ├── CurrentRates
    ├── HistoricalRates
    └── Shared
```

Dependency rule:

```text
Api -> Application -> Core
Api -> Infrastructure -> Core
Application -> Core
Infrastructure -> Core
```

`Core` не зависит от внешних слоёв. `Infrastructure` реализует интерфейсы из `Core`. `Api` собирает зависимости и публикует HTTP surface.

Модуль `CurrentRates` отвечает за текущие ставки, staleness и арбитраж. `HistoricalRates` отвечает за исторический сбор, RabbitMQ processing и APR. Оба модуля используют только действительно общие компоненты из `Shared`; `HistoricalRates` не зависит от внутренних типов `CurrentRates`.

## Data Flows

### Current rates

```text
CurrentCollectionBackgroundService
-> CurrentFundingRateCollector
-> Binance/Bybit/OKX exchange clients
-> FundingRateChangeDetector
-> CurrentFundingRateRepository
-> PostgreSQL
-> DirectRabbitMqHistoricalCollectionTaskPublisher
-> funding.history direct exchange
-> history.binance / history.bybit / history.okx
```

Интервал обновления задаётся в `CurrentDataCollectionOptions.UpdateIntervalSeconds`, сейчас `10` секунд. Отсутствующие current rates не удаляются: `LastSeenAt` фиксирует последний успешный сбор пары, а `CurrentDataCollectionOptions.DeactivateMissingAfterMinutes` задаёт порог деактивации, сейчас `5` минут.

### Historical rates

```text
HistoricalCollectionBackgroundService
-> отдельный pool для каждой биржи
-> manual ack, prefetch = 1
-> HistoricalFundingRateCollector
-> exchange client
-> HistoricalFundingRateRepository
-> PostgreSQL
-> BasicAck
```

Ограничения сбора задаются в `HistoricalDataCollectionOptions`: `DefaultConcurrencyPerExchange`, `ExchangeConcurrency`, `ApiPageSize`, `MaxHistoryMonths`, `MaxRetries`, `RetryDelaySeconds`.

Временная ошибка оставляет original message unacked, после задержки публикует confirmed-копию с увеличенным `RetryCount` и только затем подтверждает original. Permanent error и исчерпание retries направляют confirmed-копию в общую `history.dead`.

Consumer side гарантирует `at-least-once`. В Phase 1 producer публикует после PostgreSQL commit напрямую, поэтому crash-window до RabbitMQ confirm имеет гарантию `best effort`. Transactional Outbox запланирован отдельной итерацией.

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

### OpenAPI contract

```text
Controllers and DTO metadata
-> Microsoft.AspNetCore.OpenApi
-> GET /openapi/v1.json
-> openapi-typescript
-> frontend/FundingMonitor.Web/src/types/generated/api.ts
```

Scalar предоставляет интерактивную документацию по адресу `/scalar`. Axios API client остаётся ручным, генерируются только TypeScript types. Для генерации backend должен быть запущен локально, после чего во frontend выполняется `npm run generate:api-types`.

Числовой JSON-контракт использует `JsonNumberHandling.Strict`, чтобы OpenAPI описывал `decimal` и целые значения как числа, а не как `number | string`.

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
RabbitMQ   :5672
Management :15672
```

API и frontend запускаются локально:

```bash
dotnet run --project src/FundingMonitor.Api
cd frontend/FundingMonitor.Web
npm run dev
```

Vite proxy отправляет `/api` на backend. Подробности: [Docker Deployment](../deployment/docker.md).

## Связанная документация

- [Documentation Index](../index.md)
- [ADR Index](../adr/index.md)
- [Database Schema](../database-schema.md)
- [Frontend Components](../frontend/components/index.md)
- [Docker Deployment](../deployment/docker.md)
