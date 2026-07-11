---
type: project
status: active
updated: 2026-07-11
---

# Статус проекта

## Сводка

FundingMonitor имеет рабочую backend/frontend-структуру, PostgreSQL/Redis инфраструктуру, API для funding rates/history/APR/arbitrage и Obsidian-oriented документацию в `docs/`.

Документация ведётся как обычные Markdown-файлы в репозитории. Obsidian используется как рабочая среда для dashboard, index notes, задач, планов и dev-log.

## Области

| Область | Состояние | Заметки |
| --- | --- | --- |
| Backend | Активно | Есть API controllers, background services, repositories, exchange clients. |
| Frontend | Активно | React/Vite dashboard использует `widgets`, `features`, `shared`, `entities`. |
| База данных | Описана | Схема и индексы описаны в [[../database-schema]]. |
| Инфраструктура | Описана | Docker Compose поднимает PostgreSQL и Redis, см. [[../deployment/docker]]. |
| Документация | Активно | Vault index, dashboard, README входные точки и frontend component docs актуализированы. |

## Текущее состояние документации

- Главный Obsidian вход: [[../Vault Index]].
- Рабочий dashboard: [[../dashboard]].
- GitHub-friendly index: [[../index]].
- README в корне, frontend и backend оставлены короткими входными точками.
- Frontend docs обновлены под текущую `features/widgets/shared/entities` структуру.
- `CoinSelector` и `ExchangeSelector` удалены из активной документации, потому что их функции заменяет `CompactFilter`.

## Текущие риски

- Документация будет устаревать, если не обновлять её вместе с feature work.
- В коде и части старых документов есть русские строки, которые в некоторых PowerShell-выводах отображаются как mojibake; это нужно чинить отдельным проходом, если проблема воспроизводится в редакторе/браузере.
- Тестовое покрытие пока не отражено как завершённая область.

## Правило обновления

Обновляй эту страницу после значимых изменений в направлении проекта, архитектуре, крупных функциях, deployment или документационном процессе.
