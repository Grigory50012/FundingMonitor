# ADR 0008: Frontend — React 19 + TypeScript + Vite + Tailwind CSS

**Статус**: Принято  
**Дата**: 2026-06-14

## Контекст

Нужен современный, быстрый, типизированный фронтенд для дашборда мониторинга:
- Таблицы текущих ставок (сортировка, фильтрация)
- Графики истории (Recharts)
- Арбитражная таблица
- Реальное время (обновление по кнопке и при смене фильтров)
- Адаптивность, тёмная тема

## Решение

**Стек**:
| Инструмент | Версия | Назначение |
|------------|--------|------------|
| React | 19.1 | UI библиотека |
| TypeScript | 5.8 | Статическая типизация |
| Vite | 7.1 | Dev server + bundler |
| Tailwind CSS | 4.2 | Utility-first стили |
| Axios | 1.13 | HTTP клиент с интерцепторами |
| Recharts | 3.8 | Графики (SVG, declarative) |
| ESLint | 9.x | Линтинг |

**Архитектура**:
```
frontend/FundingMonitor.Web/src/
├── api/              # Axios instance + endpoint functions
├── components/       # Presentational components (таблицы, графики, фильтры)
├── containers/       # DashboardContainer — smart component, данные + состояние
├── hooks/            # useFundingRates.ts (useCurrentRates, useHistoryRates, useArbitrageRates), useLocalStorage
├── types/            # Generated API DTO и ручные frontend-only типы
├── config/           # Константы (storage keys)
└── main.tsx          # Entry point
```

**State management**: React Query / TanStack Query НЕ используется — данные загружаются через `useEffect` при изменении параметров. Фильтры кэшируются в `localStorage`. Автоматический поллинг планируется.

## Последствия

✅ **Плюсы**:
- Vite: мгновенный HMR, быстрая сборка
- TypeScript: DTO генерируются из OpenAPI через `openapi-typescript`; Axios-клиент остаётся ручным
- Tailwind: быстрое UI, тёмная тема через CSS переменные
- Recharts: декларативные графики, легкая кастомизация
- Нет Redux/Zustand — простота, меньше зависимостей

❌ **Минусы**:
- Для обновления generated DTO backend должен быть запущен локально; `npm run generate:api-types` читает встроенную OpenAPI schema из `/openapi/v1.json`
- Нет серверного стейт-менеджмента (кэш, дедуп, рефетч) — при росте добавим TanStack Query
- Нет Storybook — компоненты документируются только в коде

## Альтернативы

| Вариант | Причина отказа |
|---------|----------------|
| Next.js | SSR не нужен (дашборд за авторизацией, реальное время), лишняя сложность |
| Vue 3 + Vite | Команда знает React лучше |
| SvelteKit | Меньше экосистема, риск найма |
| Remix | Overkill для SPA дашборда |
| TanStack Query (React Query) | Пока не нужен — загрузка по фильтрам достаточна, кэш не критичен |

## Планы

1. ✅ Добавлен types-only codegen через `openapi-typescript`; генерация API-клиента намеренно не используется
2. Внедрить TanStack Query при усложнении данных
3. Добавить Storybook для каталога компонентов
4. E2E тесты (Playwright)

## Связанные решения

- [ADR 0009: встроенный OpenAPI и Scalar](0009-built-in-openapi-and-scalar.md)
