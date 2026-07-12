# FundingMonitor

FundingMonitor — приложение для мониторинга funding rates на криптовалютных perpetual futures. Оно собирает текущие и исторические ставки с Binance, Bybit и OKX, хранит данные в PostgreSQL и показывает funding arbitrage opportunities через API и frontend dashboard.

## Быстрый старт

### Требования

- .NET SDK 10, см. `global.json`
- Node.js 18+
- Docker

### Инфраструктура

```bash
docker-compose up -d
```

Поднимаются только PostgreSQL и Redis:

- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`

### Backend

```bash
dotnet restore
dotnet build FundingMonitor.sln
dotnet run --project src/FundingMonitor.Api
```

Development API / Swagger:

```text
http://localhost:5000/
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

## Основные возможности

- Сбор текущих funding rates каждые 10 секунд.
- Исторический сбор через Redis-backed очередь.
- REST API для текущих ставок, истории, APR statistics, arbitrage и health бирж.
- React/Vite dashboard с фильтрами по символу и биржам.
- APR analytics по периодам 1, 2, 3, 7, 14, 21 и 30 дней.
- Clean Architecture: `Core`, `Application`, `Infrastructure`, `Api`.

## Технологии

| Область | Стек |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, EF Core 10, NLog |
| Database | PostgreSQL 17 |
| Queue | Redis 7 |
| Exchange APIs | Binance.Net, Bybit.Net, JK.OKX.Net |
| Frontend | React 19, TypeScript 5.8, Vite 7, Tailwind CSS 4, Recharts 3, Axios |
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

## README политика

README в корне и модулях остаются короткими входными точками: команды, быстрый старт, ссылки. Подробные объяснения архитектуры, решений, планов и процесса живут в `docs/`.
