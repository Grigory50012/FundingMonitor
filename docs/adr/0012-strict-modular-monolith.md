# ADR 0012: Строгий модульный монолит с изолированными слоями и данными

**Статус**: Принято
**Дата**: 2026-09-06
**Заменяет**: [ADR 0011](0011-module-first-modular-monolith.md)

## Контекст

ADR 0011 сгруппировал backend-код по `CurrentRates` и `HistoricalRates` внутри общих проектов `Core`, `Application`, `Infrastructure` и `Api`. Это улучшило навигацию, но не создало compile-time isolation: модули продолжают использовать общий `FundingMonitorDbContext`, общие exchange clients и публично доступные внутренние типы.

Проект ожидает дальнейшее развитие арбитража, интеграций с биржами и процессов сбора. Пока development-данные можно пересоздать, строгие границы дешевле ввести до роста количества use cases и межмодульных зависимостей.

## Решение

Backend остаётся одним deployable приложением, но разделяется на четыре бизнес-модуля:

- `ExchangeIntegration` — SDK и API бирж, rate limits, нормализация и health;
- `CurrentRates` — сбор, актуальность и хранение текущих ставок;
- `HistoricalRates` — исторический сбор, RabbitMQ consumers, retry/DLQ и APR;
- `Arbitrage` — расчёт и выдача арбитражных возможностей.

Каждый модуль получает отдельные проекты:

```text
FundingMonitor.<Module>.Contracts
FundingMonitor.<Module>.Domain
FundingMonitor.<Module>.Application
FundingMonitor.<Module>.Infrastructure
FundingMonitor.<Module>.Presentation
```

Другой модуль может ссылаться только на `FundingMonitor.<Module>.Contracts`. Правило защищается `ProjectReference` и архитектурными тестами.

`FundingMonitor.SharedKernel` содержит только стабильные общие domain primitives. Техническая RabbitMQ-механика размещается отдельно в `FundingMonitor.Messaging` и не содержит business contracts.

`FundingMonitor.ApiHost` является единым composition root и подключает module Presentation assemblies. HTTP routes, единый `/openapi/v1.json` и `/scalar` сохраняются.

## Данные

Используется одна PostgreSQL database, но данные изолируются по schemas:

- `current_rates` принадлежит `CurrentRatesDbContext` и содержит current rates и Outbox;
- `historical_rates` принадлежит `HistoricalRatesDbContext` и содержит history и Inbox.

Каждый context имеет собственную `__EFMigrationsHistory` в своей schema. Cross-schema queries, foreign keys и EF navigation properties запрещены.

`Arbitrage` сохраняет результаты в памяти и не получает schema или `DbContext`. `ExchangeIntegration` остаётся stateless, пока не появится конкретный сценарий persistence.

Устаревшие development-данные не переносятся. Новые module migrations создают clean baseline. Миграции применяет отдельное console application `FundingMonitor.DatabaseMigrator`; `ApiHost` не изменяет database schema при запуске.

## Коммуникация

`CurrentRates` и `HistoricalRates` синхронно получают внешние данные через `ExchangeIntegration.Contracts`.

`Arbitrage` синхронно читает актуальные ставки через `CurrentRates.Contracts` и не обращается к чужой schema.

Связь `CurrentRates -> HistoricalRates` становится event-driven:

```text
CurrentRates transaction
-> FundingRateChangedIntegrationEvent in Outbox
-> RabbitMQ publisher confirm
-> per-exchange HistoricalRates queue
-> Inbox check
-> history + Inbox transaction
-> manual ACK
```

Inbox подавляет redelivery одного `EventId`, но разные бизнес-события для одной пары не дедуплицируются. Сохраняются independent per-exchange worker pools, retry, DLQ и at-least-once processing.

## Frontend

Frontend остаётся единым React/Vite application. Feature-каталоги выравниваются по `current-rates`, `historical-rates`, `arbitrage` и `exchange-health`. Backend остаётся владельцем APR, spread, freshness и других business calculations.

## Последствия

Преимущества:

- границы модулей и слоёв контролирует компилятор;
- каждый модуль владеет своим API и persistence;
- exchange SDK больше не связывают доменные модели current и historical flows;
- Outbox закрывает потерю события между PostgreSQL commit и RabbitMQ publish;
- модуль можно развивать и при необходимости извлекать без доступа к чужим internals.

Недостатки:

- solution получает около 24 production projects;
- увеличивается объём DI, mapping и migration boilerplate;
- переход требует последовательного переноса четырёх модулей и удаления legacy projects;
- правила границ требуют постоянной защиты architecture tests.

## Альтернативы

### Одна implementation assembly и Contracts на модуль

Даёт строгую внешнюю границу при меньшем числе проектов, но слои внутри модуля защищаются только `internal` и тестами. Не выбрано в пользу максимальной compile-time isolation.

### Полные слои только для сложных модулей

Уменьшает количество пустых проектов, но создаёт несимметричную структуру и неодинаковые правила для модулей. Не выбрано ради предсказуемого масштабирования solution.

### Сохранить ADR 0011 без дальнейшего разделения

Сохраняет простоту текущей структуры, но оставляет общий `DbContext`, общий infrastructure project и возможность прямых зависимостей между бизнес-областями.

### Начать с микросервисов

Не выбрано: независимое масштабирование и deployment модулей сейчас не требуются, а operational complexity распределённой системы не оправдана.
