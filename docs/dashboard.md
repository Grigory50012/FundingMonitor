---
type: dashboard
status: active
updated: 2026-07-12
---

# Дашборд FundingMonitor

## Состояние проекта

| Область | Статус | Ссылка |
| --- | --- | --- |
| Продукт | Активно | [[project/overview]] |
| Backend | Активно | [[architecture/index]] |
| Frontend | Активно | [[frontend/components/index]] |
| База данных | Описана | [[database-schema]] |
| Deployment | Описан | [[deployment/docker]] |
| Решения | Активно | [[adr/index]] |

## Текущий фокус

- Держать [[project/status]] актуальным.
- Работать из `docs/tasks/active.md`.
- Для крупных задач создавать accepted plan в [[plans/index]].

## Активные задачи

```tasks
not done
path includes docs/tasks
sort by priority
sort by due
```

## Проектные страницы

```dataview
TABLE status, updated
FROM "docs/project"
SORT file.name ASC
```

## Accepted plans

```dataview
TABLE status, area, priority, updated
FROM "docs/plans/accepted"
SORT priority ASC, updated DESC
```

## ADR

```dataview
TABLE status, date
FROM "docs/adr"
WHERE file.name != "index"
SORT file.name DESC
```

## Быстрые ссылки

- [[index|Индекс документации]]
- [[project/status|Статус проекта]]
- [[project/roadmap|Roadmap]]
- [[tasks/active|Активные задачи]]
- [[tasks/backlog|Backlog]]
- [[plans/index|Планы]]
- [[architecture/index|Архитектура]]
- [[adr/index|ADR]]
- [[deployment/docker|Docker deployment]]
