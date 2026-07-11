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
http://localhost:5000/
```

## Проекты

- `FundingMonitor.Api` — HTTP API, controllers, DTO, middleware.
- `FundingMonitor.Application` — application services и background services.
- `FundingMonitor.Core` — domain entities, interfaces, configuration, events.
- `FundingMonitor.Infrastructure` — EF Core, repositories, queues, exchange clients.

## Документация

- [Project Dashboard](../docs/dashboard.md)
- [Vault Index](../docs/Vault%20Index.md)
- [Architecture](../docs/architecture/index.md)
- [ADR](../docs/adr/index.md)
- [Database Schema](../docs/database-schema.md)
- [Docker Deployment](../docs/deployment/docker.md)
