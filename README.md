# FundingMonitor

FundingMonitor — приложение для мониторинга funding rates на криптовалютных perpetual futures. Оно собирает текущие и исторические ставки с Binance, Bybit и OKX, хранит данные в PostgreSQL и показывает funding arbitrage opportunities через API и frontend dashboard.

## Быстрый старт

### Требования

- .NET SDK 10, см. `global.json`
- Node.js 18+
- Docker

### Инфраструктура

```bash
docker compose up -d --remove-orphans
```

Поднимаются PostgreSQL и RabbitMQ:

- PostgreSQL: `localhost:5432`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ Management UI: `http://localhost:15672`

### Backend

```bash
dotnet restore
dotnet build FundingMonitor.sln
dotnet run --project src/FundingMonitor.DatabaseMigrator
dotnet run --project src/FundingMonitor.ApiHost
```

Сначала `DatabaseMigrator` применяет миграции модулей; `ApiHost` запускается только после его успешного завершения и не меняет схему БД.

Документация API в Development (Scalar):

```text
http://localhost:5000/scalar
```

OpenAPI document:

```text
http://localhost:5000/openapi/v1.json
```

### Frontend

```bash
cd frontend/FundingMonitor.Web
npm install
npm run dev
```

Frontend dev server:

```text
http://localhost:5173
```

После изменения backend DTO обновите generated frontend types при запущенном API:

```bash
cd frontend/FundingMonitor.Web
npm run generate:api-types
```

## Основные возможности

- Сбор текущих funding rates каждые 10 секунд.
- Исторический сбор через независимые RabbitMQ queues Binance, Bybit и OKX.
- REST API для текущих ставок, истории, APR statistics, arbitrage и health бирж.
- React/Vite dashboard с фильтрами по символу и биржам.
- APR analytics по периодам 1, 2, 3, 7, 14, 21 и 30 дней.
- Строгий модульный монолит: `ExchangeIntegration`, `CurrentRates`, `HistoricalRates`, `Arbitrage`; каждый модуль содержит пять отдельных слоёв, внешние зависимости разрешены только через `Contracts`.
- Надёжная доставка изменений ставок: PostgreSQL Outbox → RabbitMQ → Inbox.

## Технологии

| Область | Стек |
| --- | --- |
| Backend | .NET 10, ASP.NET Core / EF Core 10.0.9, NLog 6.1.4 |
| Database | PostgreSQL 17 |
| Queue | RabbitMQ 4.2, RabbitMQ.Client 7.2.1 |
| Exchange APIs | Binance.Net 13.1.0, Bybit.Net 7.1.0, JK.OKX.Net 5.1.0 |
| API contract | Microsoft.AspNetCore.OpenApi 10.0.9, Microsoft.OpenApi 2.10.0, Scalar 2.16.11, openapi-typescript 7.10.1 |
| Frontend | React 19.1, TypeScript 5.8, Vite 7, Tailwind CSS 4.2, Recharts 3.8, Axios 1.13 |
| Docs | Markdown, Obsidian, Dataview, Tasks, Templater |

## Документация

Основная документация ведётся в `docs/` и открывается как Obsidian vault из корня репозитория.

- [Documentation Index](docs/index.md)
- [Architecture](docs/architecture/index.md)
- [ADR](docs/adr/index.md)
- [Database Schema](docs/database-schema.md)
- [Docker Deployment](docs/deployment/docker.md)
- [Frontend Components](docs/frontend/components/index.md)
- [Backend README](src/README.md)
- [Frontend README](frontend/FundingMonitor.Web/README.md)

## API

| Method | Path | Назначение |
| --- | --- | --- |
| GET | `/api/v1/FundingRates` | Текущие funding rates |
| GET | `/api/v1/History` | История funding rates |
| GET | `/api/v1/History/apr-stats` | APR statistics |
| GET | `/api/v1/Arbitrage` | Funding arbitrage opportunities |
| GET | `/api/v1/exchanges/health` | Health status бирж |
| GET | `/health` | Проверка доступности HTTP host |

## README политика

README в корне и модулях остаются короткими входными точками: команды, быстрый старт, ссылки. Подробные объяснения архитектуры, решений, планов и процесса живут в `docs/`.
