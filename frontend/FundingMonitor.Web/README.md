# FundingMonitor.Web

Frontend FundingMonitor: React 19, TypeScript, Vite 7, Tailwind CSS 4.

Этот README нужен как быстрый вход в frontend-модуль. Подробная документация ведётся в `docs/` и Obsidian.

## Команды

```bash
npm install
npm run dev
npm run build
npm run lint
npm run preview
```

Dev server:

```text
http://localhost:5173
```

## Структура

- `src/app/providers/` — точка подключения application-level providers.
- `src/entities/` — доменные UI-сущности.
- `src/features/` — feature-owned API, hooks, компоненты и модели: `current-rates`, `historical-rates`, `arbitrage`, `exchange-health`, `filters`.
- `src/shared/` — переиспользуемые UI, API client, hooks и helper-функции.
- `src/widgets/dashboard/` — страница dashboard и orchestration hook.

## Документация

- [Documentation Index](../../docs/index.md)
- [Frontend Components](../../docs/frontend/components/index.md)
- [Project Overview](../../docs/project/overview.md)
- [Architecture](../../docs/architecture/index.md)
