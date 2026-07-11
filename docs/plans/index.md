---
type: plans
status: active
updated: 2026-07-11
---

# Планы

Папка для implementation plans, которые не являются ADR.

## Git visibility

| Путь | В Git | Назначение |
| --- | --- | --- |
| `docs/plans/index.md` | да | Индекс и правила ведения планов |
| `docs/plans/accepted/` | да | Планы, которые стоит сохранить как часть истории проекта |
| `docs/plans/drafts/` | нет | Черновики и рабочие наброски |
| `docs/plans/private/` | нет | Личные или непубличные планы |

## Формат плана

- Проблема
- Цель
- Область
- Шаги
- Проверка
- Последующие заметки

## Accepted plans

```dataview
TABLE status, area, priority, updated
FROM "docs/plans/accepted"
SORT priority ASC, updated DESC
```

## Локальные черновики

`docs/plans/drafts/` и `docs/plans/private/` игнорируются Git. Они доступны локально в Obsidian, но не должны попадать в коммиты.
