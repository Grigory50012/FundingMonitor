---
type: dashboard
status: active
updated: 2026-07-11
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

- Поддерживать [[project/status]] в актуальном состоянии.
- Планировать работу в [[tasks/active]] и [[plans/index]].
- Фиксировать заметки по разработке в [[dev-log/index]].

## Активные задачи

```tasks
not done
path includes docs/tasks
sort by priority
sort by due
```

## Страницы проекта

```dataview
TABLE status, updated
FROM "docs/project"
SORT file.name ASC
```

## Планы

```dataview
TABLE status, area, priority, updated
FROM "docs/plans"
WHERE file.name != "index"
SORT priority ASC, updated DESC
```

## Архитектурные решения

```dataview
TABLE status, date
FROM "docs/adr"
WHERE file.name != "index"
SORT file.name DESC
```

## Последние записи dev-log

```dataview
LIST
FROM "docs/dev-log"
WHERE file.name != "index"
SORT file.name DESC
LIMIT 5
```

## Быстрые ссылки

- [[index|Индекс документации]]
- [[project/overview|Описание проекта]]
- [[project/roadmap|Roadmap]]
- [[project/glossary|Глоссарий]]
- [[tasks/backlog|Backlog]]
- [[tasks/active|Активные задачи]]
- [[tasks/done|Завершённые задачи]]
- [[architecture/index|Архитектура]]
- [[adr/index|ADR]]
- [[deployment/docker|Docker deployment]]
