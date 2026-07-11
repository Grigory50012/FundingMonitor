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

- `src/api/` — клиент API и endpoint-функции.
- `src/entities/` — доменные UI-сущности.
- `src/features/` — feature-owned компоненты и модели.
- `src/shared/` — переиспользуемые UI и helper-функции.
- `src/widgets/dashboard/` — страница dashboard и orchestration hook.

## Документация

- [Project Dashboard](../../docs/dashboard.md)
- [Vault Index](../../docs/Vault%20Index.md)
- [Frontend Components](../../docs/frontend/components/index.md)
- [Project Status](../../docs/project/status.md)
- [Architecture](../../docs/architecture/index.md)
