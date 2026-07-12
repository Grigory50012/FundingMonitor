# FundingMonitor Backend

Backend FundingMonitor: .NET 10, ASP.NET Core, EF Core, PostgreSQL, Redis.

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

- `FundingMonitor.Api` — HTTP API, controllers, DTO, middleware.
- `FundingMonitor.Application` — application services и background services.
- `FundingMonitor.Core` — domain entities, interfaces, configuration, events.
- `FundingMonitor.Infrastructure` — EF Core, repositories, queues, exchange clients.

## Документация

- [Project Dashboard](../docs/dashboard.md)
- [Documentation Index](../docs/index.md)
- [Architecture](../docs/architecture/index.md)
- [ADR](../docs/adr/index.md)
- [Database Schema](../docs/database-schema.md)
- [Docker Deployment](../docs/deployment/docker.md)
