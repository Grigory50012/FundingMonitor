---
type: process
status: active
updated: 2026-07-11
area: docs
priority: medium
---

# Чеклист обновления документации

Используй этот чеклист после заметных изменений в коде, архитектуре, deployment, планах или процессе разработки.

## Быстрая проверка

- [ ] Обновить [[status|Статус проекта]], если изменилось состояние backend, frontend, базы данных, инфраструктуры или документации.
- [ ] Обновить [[roadmap|Roadmap]], если изменился ближайший фокус, следующие задачи или отложенные направления.
- [ ] Обновить `README.md`, `src/README.md` или `frontend/FundingMonitor.Web/README.md`, если изменились команды запуска, требования или быстрые входные точки.
- [ ] Обновить [[../architecture/index|Архитектуру]], если изменились data flow, background services, API boundaries, frontend layers или интеграции.
- [ ] Добавить или обновить ADR в [[../adr/index|ADR]], если принято архитектурное решение.
- [ ] Обновить [[../deployment/docker|Docker deployment]], если изменились сервисы, порты, переменные окружения или порядок запуска.
- [ ] Обновить [[../database-schema|Схему базы данных]], если изменились сущности, индексы, миграции или правила хранения данных.
- [ ] Обновить [[../frontend/components/index|Frontend components]], если изменились публичные UI-компоненты, feature folders или compatibility exports.
- [ ] Перенести завершённые задачи из [[../tasks/active|Активных задач]] в [[../tasks/done|Завершённые задачи]].
- [ ] Добавить accepted plan в [[../plans/index|Планы]], если работа крупная и должна остаться в истории Git.

## По типу изменения

| Изменение | Проверить |
| --- | --- |
| Новая backend-функция | `src/README.md`, architecture docs, API docs, project status |
| Новая frontend-функция | frontend README, frontend component docs, dashboard/status |
| Изменение API contract | README API table, DTO docs/comments, frontend types notes |
| Изменение сбора данных | architecture docs, deployment notes, database schema, roadmap/status |
| Изменение deployment | `docker-compose.yml`, deployment docs, root README |
| Архитектурное решение | новый или обновлённый ADR, links из architecture/status |
| Крупная задача | accepted implementation plan, active/done tasks, project status |

## Перед завершением работы

- [ ] Проверить, что новые страницы имеют frontmatter с `type`, `status` и `updated`.
- [ ] Проверить, что внутренние ссылки используют Obsidian wikilinks.
- [ ] Проверить, что README остаются короткими входными точками, а длинные объяснения живут в `docs/`.
- [ ] Проверить, что локальные рабочие заметки не попали в Git, если они должны жить в `docs/tasks/`, `docs/dev-log/`, `docs/plans/drafts/` или `docs/plans/private/`.
- [ ] Проверить, что `updated` изменён у страниц, смысл которых реально поменялся.

## Связанные заметки

- [[../Obsidian Workflow]]
- [[status]]
- [[roadmap]]
- [[../plans/index]]
