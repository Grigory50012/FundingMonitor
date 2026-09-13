# Architecture Decision Records (ADR)

Этот каталог содержит записи архитектурных решений (Architecture Decision Records) для проекта FundingMonitor.

## Что такое ADR?

ADR — это документ, фиксирующий значимое архитектурное решение, его контекст, альтернативы и последствия. Это помогает:
- Сохранить историю принятия решений
- Облегчить онбординг новых разработчиков
- Избежать повторного обсуждения одних и тех же вопросов

## Формат записи

Каждая запись следует шаблону:
- **Статус**: Предложено / Принято / Отклонено / Устарело / Заменено
- **Контекст**: Какая проблема решается
- **Решение**: Что принято
- **Последствия**: Плюсы, минусы, риски
- **Альтернативы**: Что еще рассматривалось

## Список записей

| № | Заголовок | Статус | Дата |
|---|-----------|--------|------|
| [0001](0001-clean-architecture-layers.md) | Clean Architecture: разделение на Core, Application, Infrastructure, Api | Принято | 2026-06-14 |
| [0002](0002-background-services-for-data-collection.md) | Фоновые сервисы (BackgroundService) для сбора данных с бирж | Принято | 2026-06-14 |
| [0003](0003-redis-queue-for-historical-collection.md) | Redis как персистентная очередь для задач сбора истории | Заменено ADR 0010 | 2026-06-14 |
| [0004](0004-entity-framework-core-with-postgresql.md) | Entity Framework Core + PostgreSQL для хранения данных | Принято | 2026-06-14 |
| [0005](0005-problem-details-for-error-handling.md) | RFC 7807 ProblemDetails для глобальной обработки ошибок | Принято | 2026-06-14 |
| [0006](0006-multi-exchange-support-with-abstraction.md) | Абстракция клиентов бирж для поддержки множественных источников | Принято | 2026-06-14 |
| [0007](0007-apr-calculation-methodology.md) | Методология расчёта APR и агрегации статистики | Принято | 2026-06-14 |
| [0008](0008-frontend-react-typescript-vite.md) | Frontend: React 19 + TypeScript + Vite + Tailwind CSS | Принято | 2026-06-14 |
| [0009](0009-built-in-openapi-and-scalar.md) | Встроенный OpenAPI и Scalar | Принято | 2026-07-12 |
| [0010](0010-rabbitmq-for-parallel-historical-collection.md) | RabbitMQ для параллельного исторического сбора | Принято | 2026-07-26 |
| [0011](0011-module-first-modular-monolith.md) | Module-first организация модульного монолита | Заменено ADR 0012 | 2026-08-05 |
| [0012](0012-strict-modular-monolith.md) | Строгий модульный монолит с изолированными слоями и данными | Принято | 2026-09-06 |
| [0013](0013-simple-outbox-without-inbox.md) | Простой Outbox без Inbox, один экземпляр backend | Принято | 2026-09-12 |
