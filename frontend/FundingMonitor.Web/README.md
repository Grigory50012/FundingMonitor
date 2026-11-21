# FundingMonitor.Web

Frontend FundingMonitor: React 19, TypeScript, Vite 7, Tailwind CSS 4.

Этот README нужен как быстрый вход в frontend-модуль. Подробная документация ведётся в `docs/` и Obsidian.

## Команды

```bash
npm install
npm run dev
npm run build
npm run lint
npm test
npm run preview
```

Dev server:

```text
http://127.0.0.1:5173
```

Vite привязан к IPv4 `127.0.0.1` через `server.host` в `vite.config.ts`. При `ERR_CONNECTION_REFUSED` проверьте вывод `npm run dev` и фактический порт; запросы `/api` проксируются напрямую на backend `https://localhost:5001`, без перенаправления с HTTP. Запускайте ApiHost с HTTPS launch profile. `secure: false` применяется только к локальному dev-прокси, чтобы Node принимал сертификат разработки ASP.NET Core.

## Структура

- `src/app/providers/` — точка подключения application-level providers.
- TanStack Query управляет кэшем истории и APR: обновление в `HH:01`, проверка устаревания при возврате во вкладку и восстановлении сети, три повторные попытки при ошибке. Фоновое обновление сохраняет текущие данные на экране.
- Арбитраж использует TanStack Query с фоновым обновлением каждые 5 секунд и переключателем паузы. Страницы по 25/50/100 монет формируются на клиенте после группировки и сортировки.
- `src/entities/` — доменные UI-сущности.
- `src/features/` — feature-owned API, hooks, компоненты и модели: `current-rates`, `historical-rates`, `arbitrage`, `filters`.
- `src/shared/` — переиспользуемые UI, API client, hooks и helper-функции.
- `src/widgets/dashboard/` — страница dashboard и orchestration hook.

## Документация

- [Documentation Index](../../docs/index.md)
- [Frontend Components](../../docs/frontend/components/index.md)
- [Project Overview](../../docs/project/overview.md)
- [Architecture](../../docs/architecture/index.md)
