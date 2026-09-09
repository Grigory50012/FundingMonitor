# Архитектура FundingMonitor

Backend — строгий модульный монолит: один HTTP host и четыре изолированных модуля. Решение закреплено в [ADR 0012](../adr/0012-strict-modular-monolith.md), заменившем подход [ADR 0011](../adr/0011-module-first-modular-monolith.md).

## Модули и слои

| Каталог под `src/Modules` | Ответственность | Владение данными |
| --- | --- | --- |
| `ExchangeIntegration` | SDK бирж, нормализация, rate limits, health | Без persistence |
| `CurrentRates` | Сбор, актуальность и выдача текущих ставок | `current_rates`, Outbox |
| `HistoricalRates` | Исторический сбор, retry/DLQ, история и APR | `historical_rates`, Inbox |
| `Arbitrage` | Расчёт арбитражных возможностей | Без persistence |

Каждый модуль содержит пять проектов `FundingMonitor.<Module>.<Layer>`:

- `Contracts` — публичные DTO, интерфейсы и integration events.
- `Domain` — бизнес-модель и правила модуля.
- `Application` — сценарии использования и внутренние порты.
- `Infrastructure` — реализация портов, persistence и фоновые сервисы.
- `Presentation` — HTTP controllers, mapping и регистрация модуля.

Модуль может зависеть от другого модуля только через его `Contracts`. Внутри модуля зависимости направлены к `Domain` и `Contracts`; `Application` не ссылается на `Infrastructure` или `Presentation`. Architecture tests проверяют проекты, сборки, видимость типов и отсутствие общих EF-моделей.

`FundingMonitor.SharedKernel` содержит стабильные примитивы без бизнес-логики. `FundingMonitor.Messaging` содержит транспортную механику RabbitMQ, сериализацию и envelope; бизнес-контракты принадлежат модулям.

`FundingMonitor.ApiHost` — composition root: регистрирует четыре модуля через `Presentation`, настраивает HTTP pipeline и единый OpenAPI. `FundingMonitor.DatabaseMigrator` отдельно применяет обе группы миграций; ApiHost не мигрирует БД при старте.

## Потоки данных

`CurrentRates` и `HistoricalRates` получают данные бирж через `ExchangeIntegration.Contracts`. `Arbitrage` читает ставки через `CurrentRates.Contracts`, рассчитывает результат в памяти и не обращается к чужой схеме БД.

```text
CurrentRates: сбор → текущие ставки + integration event в одной транзакции
→ current_rates.OutboxMessages
→ Outbox dispatcher → RabbitMQ publisher confirm
→ current-rates.events
→ historical-rates.collect.binance / .bybit / .okx
→ проверка Inbox → сбор истории через ExchangeIntegration.Contracts
→ история + Inbox в одной транзакции → manual ACK
```

Outbox сохраняет событие вместе с текущими ставками и закрывает окно потери между commit БД и публикацией. Dispatcher отмечает отправку после publisher confirm; повторная доставка допустима. Inbox подавляет повторную обработку одного `EventId` для consumer. Разные события одной пары не дедуплицируются: время выплаты на бирже может измениться.

У каждой биржи независимый worker pool, по умолчанию по 10 consumers; manual ACK и `prefetch=1`. Временная ошибка повторяется после задержки с увеличенным retry count. Original message подтверждается после confirmed-публикации retry или DLQ; permanent error и исчерпанные retries направляются в `historical-rates.dead`. Это доставка `at-least-once` с идемпотентным сохранением истории.

Интервал текущего сбора задаёт `CurrentDataCollectionOptions.UpdateIntervalSeconds` (10 секунд). Порог актуальности — `DeactivateMissingAfterMinutes` (5 минут). При каждом чтении CurrentRates выдаёт только активные пары с `LastSeenOnUtc >= now - threshold`; после истечения порога ставка исчезает из текущей выдачи и расчёта Arbitrage даже при недоступной бирже. Последние значения остаются в БД. Успешный snapshot дополнительно деактивирует отсутствующие просроченные пары, а повторное появление пары обновляет timestamp и активирует её. Детали таблиц и транзакций: [схема БД](../database-schema.md).

## HTTP и OpenAPI

Маршруты и JSON-контракт при переходе сохранены:

| Метод | Маршрут | Назначение |
| --- | --- | --- |
| GET | `/api/v1/FundingRates` | Текущие ставки |
| GET | `/api/v1/History` | История |
| GET | `/api/v1/History/apr-stats` | APR по периодам |
| GET | `/api/v1/Arbitrage` | Арбитражные возможности |
| GET | `/api/v1/exchanges/health` | Состояние бирж |
| GET | `/health` | Состояние зарегистрированных health checks |

`/health` включает `rabbitmq-topology`: при каждом запросе выполняется passive probe exchange и четырёх очередей через AMQP с бюджетом отмены 5 секунд. Недоступный брокер или отсутствующая topology дают `Degraded`; восстановление доступности возвращает `Healthy`. Проверка не пересоздаёт topology, не проверяет bindings или число consumers и не обращается к биржам. При стандартном mapping ASP.NET Core статус `Degraded` также возвращается с HTTP 200, поэтому нужно учитывать тело ответа; consumers проверяются отдельно. В Development `Microsoft.AspNetCore.OpenApi` публикует `/openapi/v1.json`, Scalar — `/scalar`. `JsonNumberHandling.Strict` сохраняет числовой JSON-контракт. Команда `npm run generate:api-types` при запущенном API обновляет `frontend/FundingMonitor.Web/src/types/generated/api.ts`; Axios client остаётся ручным.

## Frontend и запуск

`app` и `widgets/dashboard` собирают features `current-rates`, `historical-rates`, `arbitrage`, `exchange-health` и `filters`. Каждый feature владеет API-функциями, hooks и UI; feature internals не импортируют друг друга. Общие Axios client, форматирование и UI находятся в `shared`, exchange primitives — в `entities`. APR, spread и freshness рассчитывает backend; frontend отображает данные, фильтрует и сортирует.

```bash
docker compose up -d --remove-orphans
dotnet run --project src/FundingMonitor.DatabaseMigrator
dotnet run --project src/FundingMonitor.ApiHost
```

Compose запускает PostgreSQL на `5432`, RabbitMQ на `5672` и Management UI на `15672`. Frontend запускается из `frontend/FundingMonitor.Web` командой `npm run dev`; Vite перенаправляет `/api` на backend.

## Связанная документация

- [Вход в документацию](../index.md)
- [ADR](../adr/index.md)
- [Сценарии использования](USE_CASE_DIAGRAM.md)
- [Схема БД](../database-schema.md)
- [Frontend-компоненты](../frontend/components/index.md)
- [Развёртывание](../deployment/docker.md)
