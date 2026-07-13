# Индекс документации FundingMonitor

Это главный вход в документацию проекта. Используй его и в Obsidian, и при чтении файлов в GitHub.

## Проект

| Страница | Для чего |
| --- | --- |
| [Overview](project/overview.md) | Цели, scope и границы проекта |
| [Glossary](project/glossary.md) | Термины проекта |

## Техническая документация

| Раздел | Для чего |
| --- | --- |
| [Architecture](architecture/index.md) | Архитектура backend/frontend, data flow, deployment view |
| [ADR](adr/index.md) | Architecture Decision Records |
| [Database Schema](database-schema.md) | Таблицы, индексы, миграции, performance notes |
| [Docker Deployment](deployment/docker.md) | PostgreSQL и Redis через Docker Compose |
| [Frontend Components](frontend/components/index.md) | Frontend-структура и component map |

## README входные точки

- [Root README](../README.md)
- [Backend README](../src/README.md)
- [Frontend README](../frontend/FundingMonitor.Web/README.md)
- [Contributing](../CONTRIBUTING.md)

## API endpoints

| Method | Path | Query params |
| --- | --- | --- |
| GET | `/api/v1/FundingRates` | `symbol?`, `exchanges?` |
| GET | `/api/v1/History` | `symbol`, `exchanges?`, `from?`, `to?`, `limit?` |
| GET | `/api/v1/History/apr-stats` | `symbol`, `exchanges?` |
| GET | `/api/v1/Arbitrage` | `symbol?`, `exchanges?` |
| GET | `/api/v1/exchanges/health` | none |

## Когда обновлять документацию

Обновляй документацию вместе с кодом, если меняются:

- API contract;
- структура frontend/backend;
- схема базы данных;
- deployment workflow;
- архитектурное решение;
