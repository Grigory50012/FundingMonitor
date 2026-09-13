# FundingMonitor Backend

Backend FundingMonitor: .NET 10, ASP.NET Core, EF Core, PostgreSQL, RabbitMQ.

Этот README нужен как быстрый вход в backend-часть проекта. Подробная документация ведётся в `docs/` и Obsidian.

## Команды

```bash
dotnet restore
dotnet build FundingMonitor.sln
dotnet run --project src/FundingMonitor.DatabaseMigrator
dotnet run --project src/FundingMonitor.ApiHost
```

Инфраструктура для локального запуска:

```bash
docker compose up -d --remove-orphans
```

Development API:

```text
Scalar:  http://localhost:5000/scalar
OpenAPI: http://localhost:5000/openapi/v1.json
```

API использует встроенный `Microsoft.AspNetCore.OpenApi`; интерактивная документация отображается через Scalar. Решение и trade-offs описаны в [ADR 0009](../docs/adr/0009-built-in-openapi-and-scalar.md).

## Проекты

- `FundingMonitor.ApiHost` — единый composition root, HTTP pipeline и регистрация модулей.
- `FundingMonitor.DatabaseMigrator` — отдельное применение миграций перед запуском API.
- `FundingMonitor.SharedKernel` — общие примитивы без бизнес-логики.
- `FundingMonitor.Messaging` — транспортная механика без бизнес-контрактов.
- `Modules/ExchangeIntegration` — адаптеры бирж, нормализация и health.
- `Modules/CurrentRates` — текущие ставки, актуальность, схема `current_rates` и Outbox.
- `Modules/HistoricalRates` — история, APR, схема `historical_rates`, идемпотентный upsert и RabbitMQ consumers.
- `Modules/Arbitrage` — расчёт арбитража через `CurrentRates.Contracts`, без persistence.

Каждый модуль содержит отдельные проекты `Contracts`, `Domain`, `Application`, `Infrastructure`, `Presentation`. Зависимость от другого модуля разрешена только через его `Contracts`; правила проверяют architecture tests. HTTP/OpenAPI-контракт сохранён.

Подробнее: [ADR 0012](../docs/adr/0012-strict-modular-monolith.md).

## Документация

- [Documentation Index](../docs/index.md)
- [Project Overview](../docs/project/overview.md)
- [Architecture](../docs/architecture/index.md)
- [ADR](../docs/adr/index.md)
- [Database Schema](../docs/database-schema.md)
- [Docker Deployment](../docs/deployment/docker.md)
