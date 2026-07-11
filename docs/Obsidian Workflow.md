---
type: workflow
status: active
updated: 2026-07-12
---

# Obsidian Workflow

Правила ведения документации FundingMonitor.

## Основное

- Vault открывается из корня репозитория `E:\Development\Projects\FundingMonitor\FundingMonitor`.
- Главный вход в документацию: [[index]].
- Рабочий дашборд: [[dashboard]].
- Новая проектная документация пишется на русском.
- Технические имена, API paths, классы, команды и frontmatter-поля остаются на английском.
- Подробная документация живёт в `docs/`, README остаются короткими входными точками.

## Что коммитим

- `README.md`
- `src/README.md`
- `frontend/FundingMonitor.Web/README.md`
- `docs/index.md`
- `docs/dashboard.md`
- `docs/Obsidian Workflow.md`
- `docs/project/`
- `docs/architecture/`
- `docs/adr/`
- `docs/deployment/`
- `docs/frontend/`
- `docs/templates/`
- `docs/plans/index.md`
- `docs/plans/accepted/`

## Что не коммитим

- `docs/tasks/`
- `docs/dev-log/`
- `docs/plans/drafts/`
- `docs/plans/private/`
- `docs/superpowers/`

Причина: задачи, dev-log, черновики и agent-generated планы быстро меняются и будут шуметь в commit history.

## Как пользоваться

- Каждый день смотри [[dashboard]] и `docs/tasks/active.md`.
- После заметных изменений обновляй [[project/status]].
- Для важных решений создавай ADR в `docs/adr/`.
- Для крупных работ создавай план в `docs/plans/accepted/`.
- Для мелких задач достаточно `docs/tasks/active.md`.
- Dev-log веди только когда есть важное событие или решение, а не после каждого шага.

## Frontmatter

Минимум для новых заметок:

```yaml
---
type:
status:
updated: YYYY-MM-DD
---
```

Дополнительные поля для планов:

```yaml
area:
priority:
```

## Wikilinks

- В Obsidian используй wikilinks: `[[path/to/note|Название]]`.
- В GitHub-facing документах можно использовать обычные Markdown-ссылки.
- Крупные заметки по возможности заканчивай блоком `## Связанные заметки`.

## Связанные заметки

- [[index]]
- [[dashboard]]
- [[project/status]]
- [[plans/index]]
