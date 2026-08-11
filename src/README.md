# FundingMonitor Backend

Backend FundingMonitor: .NET 10, ASP.NET Core, EF Core, PostgreSQL, RabbitMQ.

Этот README нужен как быстрый вход в backend-часть проекта. Подробная документация ведётся в `docs/` и Obsidian.

## Команды

```bash
dotnet restore
dotnet build FundingMonitor.sln
dotnet run --project src/FundingMonitor.Api
```

Инфраструктура для локального запуска:

```bash
docker-compose up -d
```

Development API:

```text
Scalar:  http://localhost:5000/scalar
OpenAPI: http://localhost:5000/openapi/v1.json
```

API использует встроенный `Microsoft.AspNetCore.OpenApi`; интерактивная документация отображается через Scalar. Решение и trade-offs описаны в [ADR 0009](../docs/adr/0009-built-in-openapi-and-scalar.md).

## Проекты

- `FundingMonitor.Api` — HTTP API, DTO и middleware.
- `FundingMonitor.Application` — use cases и background services.
- `FundingMonitor.Core` — entities, contracts, configuration и events.
- `FundingMonitor.Infrastructure` — EF Core, RabbitMQ и exchange clients.

Внутри проектов используется module-first организация:

- `CurrentRates` — текущие ставки, staleness и арбитраж;
- `HistoricalRates` — исторический сбор, retry/DLQ и APR;
- `Shared` — общие exchange clients, persistence и HTTP infrastructure.

Подробнее: [ADR 0011](../docs/adr/0011-module-first-modular-monolith.md).

## Документация

- [Documentation Index](../docs/index.md)
- [Project Overview](../docs/project/overview.md)
- [Architecture](../docs/architecture/index.md)
- [ADR](../docs/adr/index.md)
- [Database Schema](../docs/database-schema.md)
- [Docker Deployment](../docs/deployment/docker.md)
