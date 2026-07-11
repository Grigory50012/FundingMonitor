---
type: workflow
status: active
updated: 2026-07-11
---

# Obsidian Workflow

Эта заметка фиксирует правила ведения Obsidian vault для FundingMonitor.

## Базовые правила

- Vault находится в корне репозитория `E:\Development\Projects\FundingMonitor\FundingMonitor`.
- Основной вход для Obsidian: [[Vault Index]].
- Рабочий дашборд проекта: [[dashboard]].
- Новая проектная документация пишется на русском.
- Технические имена, API paths, классы, команды и frontmatter-поля остаются на английском.
- Документация хранится в `docs/`, чтобы её можно было читать в GitHub и версионировать в Git.

## Git visibility

Версионируем в Git:

- `README.md`
- `src/README.md`
- `frontend/FundingMonitor.Web/README.md`
- `docs/index.md`
- `docs/dashboard.md`
- `docs/Vault Index.md`
- `docs/Obsidian Workflow.md`
- `docs/project/`
- `docs/architecture/`
- `docs/adr/`
- `docs/deployment/`
- `docs/frontend/`
- `docs/templates/`
- `docs/plans/index.md`
- `docs/plans/accepted/`

Не версионируем:

- `docs/tasks/`
- `docs/dev-log/`
- `docs/plans/drafts/`
- `docs/plans/private/`
- `docs/superpowers/`

Причина: задачи, dev-log, черновики и agent-generated планы быстро меняются и будут шуметь в commit history. Accepted plans, ADR и проектная документация остаются в Git.

## Структура

- `docs/project/` — описание проекта, статус, roadmap, глоссарий.
- `docs/tasks/` — личный backlog, активные и завершённые задачи. Игнорируется Git.
- `docs/plans/accepted/` — планы, которые нужно сохранить в истории проекта.
- `docs/plans/drafts/` — локальные черновики планов. Игнорируется Git.
- `docs/plans/private/` — личные или непубличные планы. Игнорируется Git.
- `docs/dev-log/` — локальный журнал разработки. Игнорируется Git.
- `docs/adr/` — архитектурные решения.
- `docs/templates/` — шаблоны для Templater.

## README файлы

- README в модулях должны быть короткими входными точками.
- README отвечает на вопрос: как быстро запустить или разрабатывать этот модуль.
- Подробные объяснения архитектуры, решений, планов и процесса должны жить в `docs/`.
- Если информация стала длиннее быстрого старта, перенеси её в `docs/` и оставь ссылку из README.

## Frontmatter

Используй общий набор полей:

```yaml
---
type:
status:
updated: YYYY-MM-DD
area:
priority:
---
```

Минимально обязательные поля для новых заметок:

- `type`
- `status`
- `updated`

## Wikilinks

- Для внутренних ссылок используй Obsidian wikilinks: `[[path/to/note|Название]]`.
- В конце крупных заметок добавляй блок `## Связанные заметки`, если у страницы есть важные зависимости.
- Индексные страницы должны быть списками wikilinks.

## Задачи

- Задачи ведутся Markdown-чекбоксами.
- Активная работа живёт в [[tasks/active]].
- Идеи и отложенные задачи живут в [[tasks/backlog]].
- Завершённые задачи переносятся в [[tasks/done]].
- Для группировки используй теги: `#docs`, `#backend`, `#frontend`, `#infra`, `#db`.
- `docs/tasks/` игнорируется Git, поэтому задачи считаются локальным рабочим слоем.

## Когда обновлять документацию

Обновляй vault после:

- изменения направления проекта;
- добавления крупной функции;
- изменения архитектуры;
- изменения deployment или инфраструктуры;
- принятия архитектурного решения;
- завершения заметного этапа разработки.

## Связанные заметки

- [[Vault Index]]
- [[dashboard]]
- [[project/status]]
- [[plans/index]]
