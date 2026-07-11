# Индекс документации FundingMonitor

Этот файл — GitHub-friendly индекс документации. Для работы в Obsidian начинай с [[Vault Index]] или [[dashboard]].

## Основные страницы

| Раздел | Назначение |
| --- | --- |
| [Vault Index](Vault%20Index.md) | Главный Obsidian index |
| [Dashboard](dashboard.md) | Общее состояние проекта |
| [Project Overview](project/overview.md) | Цели и область проекта |
| [Project Status](project/status.md) | Текущее состояние областей |
| [Roadmap](project/roadmap.md) | Направления работ |
| [Obsidian Workflow](Obsidian%20Workflow.md) | Правила ведения vault |

## Техническая документация

| Раздел | Назначение |
| --- | --- |
| [Architecture](architecture/index.md) | Backend architecture, потоки данных, deployment view |
| [ADR](adr/index.md) | Architecture Decision Records |
| [Database Schema](database-schema.md) | Таблицы, индексы, миграции, performance notes |
| [Docker Deployment](deployment/docker.md) | PostgreSQL и Redis через Docker Compose |
| [Frontend Components](frontend/components/index.md) | Актуальная frontend-структура и component map |

## README входные точки

- [Root README](../README.md)
- [Backend README](../src/README.md)
- [Frontend README](../frontend/FundingMonitor.Web/README.md)
- [Contributing](../CONTRIBUTING.md)

## API endpoints

| Method | Path | Query params |
| --- | --- | --- |
| GET | `/api/v1/FundingRates` | `symbol?`, `exchanges?`, `includeInactive?` |
| GET | `/api/v1/History` | `symbol`, `exchanges?`, `from?`, `to?`, `limit?` |
| GET | `/api/v1/History/apr-stats` | `symbol`, `exchanges?` |
| GET | `/api/v1/Arbitrage` | `symbol?`, `exchanges?` |
| GET | `/api/v1/exchanges/health` | none |

## Frontend

Актуальная структура:

- `src/widgets/dashboard` — страница dashboard и orchestration hook.
- `src/features/current-rates` — таблица текущих ставок.
- `src/features/history` — график истории и APR table.
- `src/features/arbitrage` — таблица arbitrage opportunities.
- `src/features/filters` — активный `CompactFilter`.
- `src/shared` — UI primitives, format/sort/symbol helpers, shared API errors.
- `src/entities/exchange` — exchange constants и badges.
- `src/components` — compatibility barrel для старых импортов.

Удалённые legacy-компоненты `CoinSelector` и `ExchangeSelector` больше не документируются как активные элементы UI.

## Правило обновления

Обновляй документацию вместе с кодом, если меняются:

- API contract;
- структура frontend/backend;
- схема базы данных;
- deployment workflow;
- архитектурное решение;
- активный план или статус проекта.
